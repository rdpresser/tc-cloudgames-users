using TC.CloudGames.SharedKernel.Application.Handlers;
using TC.CloudGames.SharedKernel.Domain.Events;
using Wolverine.Marten;

namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    internal sealed class CreateUserCommandHandler
        : BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>
    {
        private readonly IMartenOutbox _outbox;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository repository,
            IUserContext userContext,
            IMartenOutbox outbox,
            ILogger<CreateUserCommandHandler> logger)
            : base(repository, userContext)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox), "Marten outbox cannot be null");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        }

        /// <summary>
        /// Maps the command to the aggregate.
        /// </summary>
        protected override Task<Result<UserAggregate>> MapCommandToAggregateAsync(CreateUserCommand command)
        {
            var aggregateResult = CreateUserMapper.ToAggregate(command);
            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return Task.FromResult(Result<UserAggregate>.Invalid(aggregateResult.ValidationErrors));
            }

            return Task.FromResult(Result<UserAggregate>.Success(aggregateResult.Value));
        }

        /// <summary>
        /// Validates the aggregate.
        /// Example: cross-entity checks, uniqueness rules, or custom domain invariants.
        /// </summary>
        protected override Task<Result> ValidateAggregateAsync(UserAggregate aggregate)
        {
            // For now, no extra validation beyond the aggregate factory
            // Validate Email/Username uniqueness here if needed (Future enhancement)
            return Task.FromResult(Result.Success());
        }

        /// <summary>
        /// Publishes integration events through Wolverine Outbox.
        /// Maps domain events -> integration events and wraps them in EventContext.
        /// </summary>
        protected override async Task PublishIntegrationEventsAsync(UserAggregate aggregate)
        {
            var mappings = new Dictionary<Type, Func<BaseDomainEvent, UserCreatedIntegrationEvent>>
            {
                { typeof(UserCreatedDomainEvent), e => CreateUserMapper.ToIntegrationEvent((UserCreatedDomainEvent)e) }
            };

            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate, // vamos resolver esse ponto já já
                    userContext: UserContext,
                    handlerName: nameof(CreateUserCommandHandler),
                    mappings: mappings
                );

            foreach (var evt in integrationEvents)
            {
                _logger.LogDebug(
                    "Queueing integration event {EventType} for user {UserId} in Marten outbox",
                    evt.EventData.GetType().Name,
                    evt.AggregateId);

                await _outbox.PublishAsync(evt).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Main command execution.
        /// Uses the base template (map → validate → save → publish → commit).
        /// </summary>
        public override async Task<Result<CreateUserResponse>> ExecuteAsync(
            CreateUserCommand command,
            CancellationToken ct = default)
        {
            // 1. Map command -> aggregate
            var mapResult = await MapCommandToAggregateAsync(command);
            if (!mapResult.IsSuccess)
                return Result<CreateUserResponse>.Invalid(mapResult.ValidationErrors);

            var aggregate = mapResult.Value;

            // 2. Validate aggregate (optional custom rules)
            var validationResult = await ValidateAggregateAsync(aggregate);
            if (!validationResult.IsSuccess)
                return Result<CreateUserResponse>.Invalid(validationResult.ValidationErrors);

            // 3. Persist aggregate events (event sourcing)
            await Repository.SaveAsync(aggregate, ct);

            // 4. Publish integration events via outbox
            await PublishIntegrationEventsAsync(aggregate);

            // 5. Commit session (persist + flush outbox atomically)
            await Repository.CommitAsync(aggregate, ct);

            _logger.LogInformation("User {UserId} created successfully and events committed", aggregate.Id);

            // 6. Map response
            return CreateUserMapper.FromAggregate(aggregate);
        }
    }
}
