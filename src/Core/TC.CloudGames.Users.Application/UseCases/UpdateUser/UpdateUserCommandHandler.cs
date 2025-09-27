using TC.CloudGames.SharedKernel.Application.Handlers;
using Wolverine.Marten;

namespace TC.CloudGames.Users.Application.UseCases.UpdateUser
{
    internal sealed class UpdateUserCommandHandler
        : BaseCommandHandler<UpdateUserCommand, UpdateUserResponse, UserAggregate, IUserRepository>
    {
        private readonly IMartenOutbox _outbox;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserRepository repository,
            IUserContext userContext,
            IMartenOutbox outbox,
            ILogger<UpdateUserCommandHandler> logger)
            : base(repository, userContext)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<UserAggregate>> MapCommandToAggregateAsync(UpdateUserCommand command, CancellationToken ct = default)
        {
            var aggregate = await Repository.GetByIdAsync(command.Id, ct).ConfigureAwait(false);
            if (aggregate == null)
                return Result<UserAggregate>.Invalid(new ValidationError("User.NotFound", $"User {command.Id} not found"));

            var result = aggregate.UpdateInfoFromPrimitives(command.Name, command.Email, command.Username);

            if (!result.IsSuccess)
                return Result<UserAggregate>.Invalid(result.ValidationErrors);

            return Result<UserAggregate>.Success(aggregate);
        }

        protected override Task<Result> ValidateAggregateAsync(UserAggregate aggregate, CancellationToken ct = default)
        {
            // Example: validate if username is unique (future enhancement)
            return Task.FromResult(Result.Success());
        }

        protected override async Task PublishIntegrationEventsAsync(UserAggregate aggregate, CancellationToken ct = default)
        {
            var mappings = new Dictionary<Type, Func<BaseDomainEvent, UserUpdatedIntegrationEvent>>
            {
                { typeof(UserUpdatedDomainEvent), e => UpdateUserMapper.ToIntegrationEvent((UserUpdatedDomainEvent)e) }
            };

            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate,
                    UserContext,
                    nameof(UpdateUserCommandHandler),
                    mappings
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

        public override async Task<Result<UpdateUserResponse>> ExecuteAsync(UpdateUserCommand command, CancellationToken ct = default)
        {
            // 1. Map command to aggregate (loads + updates state)
            var mapResult = await MapCommandToAggregateAsync(command, ct).ConfigureAwait(false);
            if (!mapResult.IsSuccess)
            {
                AddErrors(mapResult.ValidationErrors);
                return BuildNotFoundResult();
            }

            var aggregate = mapResult.Value;

            // 2. Validate (business rules if needed)
            var validationResult = await ValidateAggregateAsync(aggregate, ct).ConfigureAwait(false);
            if (!validationResult.IsSuccess)
            {
                AddErrors(validationResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            // 3. Save aggregate state
            await Repository.SaveAsync(aggregate, ct).ConfigureAwait(false);

            // 4. Publish integration events
            await PublishIntegrationEventsAsync(aggregate, ct).ConfigureAwait(false);

            // 5. Commit transaction
            await Repository.CommitAsync(aggregate, ct).ConfigureAwait(false);

            _logger.LogInformation("User {UserId} updated successfully and events committed", aggregate.Id);

            // 6. Return response
            return UpdateUserMapper.FromAggregate(aggregate);
        }
    }
}
