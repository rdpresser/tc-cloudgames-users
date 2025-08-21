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

        public override async Task<Result<CreateUserResponse>> ExecuteAsync(
            CreateUserCommand command,
            CancellationToken ct = default)
        {
            // Step 1: Map command to aggregate
            var aggregateResult = CreateUserMapper.ToAggregate(command);
            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return Result.Invalid(aggregateResult.ValidationErrors);
            }
            var aggregate = aggregateResult.Value;

            // Step 2: Take snapshot of uncommitted events
            var uncommittedEvents = aggregate.UncommittedEvents?.ToArray() ?? Array.Empty<BaseDomainEvent>();

            // Step 3: Map domain events to integration events
            var mappings = new Dictionary<Type, Func<BaseDomainEvent, UserCreatedIntegrationEvent>>
            {
                { typeof(UserCreatedDomainEvent), e => CreateUserMapper.ToIntegrationEvent((UserCreatedDomainEvent)e) }
            };

            var integrationEvents = uncommittedEvents
                .MapToIntegrationEvents(aggregate, UserContext, nameof(CreateUserCommandHandler), mappings);

            // Step 4: Persist aggregate events (event sourcing)
            await Repository.SaveAsync(aggregate, ct).ConfigureAwait(false);

            // Step 5: Publish integration events in Marten outbox
            foreach (var evt in integrationEvents)
            {
                _logger.LogDebug("Queueing integration event for user {UserId} in Marten outbox", evt.AggregateId);
                await _outbox.PublishAsync(evt).ConfigureAwait(false);
            }

            // Step 6: Commit Marten session (sends messages in the same transaction)
            await Repository.CommitAsync(aggregate, ct).ConfigureAwait(false);

            _logger.LogInformation("User {UserId} created successfully and integration events committed", aggregate.Id);

            // Step 7: Return response
            return CreateUserMapper.FromAggregate(aggregate);
        }
    }
}
