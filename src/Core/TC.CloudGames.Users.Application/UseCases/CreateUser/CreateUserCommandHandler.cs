using TC.CloudGames.SharedKernel.Domain.Events;

namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    internal sealed class CreateUserCommandHandler
        : BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>
    {
        private readonly IMessageBus _bus;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository repository,
            IUserContext userContext,
            IMessageBus bus,
            ILogger<CreateUserCommandHandler> logger)
            : base(repository, userContext)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus), "Message bus cannot be null");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        }

        public override async Task<Result<CreateUserResponse>> ExecuteAsync(
            CreateUserCommand command,
            CancellationToken ct = default)
        {
            // ----------------------------
            // Step 1: Map command to aggregate
            // ----------------------------
            var aggregateResult = CreateUserMapper.ToAggregate(command);
            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return Result.Invalid(aggregateResult.ValidationErrors);
            }
            var aggregate = aggregateResult.Value;

            // Step 2: Take a snapshot of uncommitted events
            var uncommittedEvents = aggregate.UncommittedEvents?.ToArray() ?? Array.Empty<BaseDomainEvent>();

            // ----------------------------
            // Step 3: Map domain events to integration events BEFORE persisting
            // Preserves all fields of the concrete integration events
            // ----------------------------
            var mappings = new Dictionary<Type, Func<BaseDomainEvent, UserCreatedIntegrationEvent>>
            {
                {
                    typeof(UserCreatedDomainEvent),
                    e => CreateUserMapper.ToIntegrationEvent((UserCreatedDomainEvent)e)
                }
                // Add other domain → integration mappings here if needed
            };

            // Map uncommitted domain events to EventContext<UserCreatedIntegrationEvent, UserAggregate>
            var integrationEvents = uncommittedEvents
                .MapToIntegrationEvents(
                    aggregate,
                    UserContext,
                    handlerName: nameof(CreateUserCommandHandler),
                    mappings: mappings
                );


            ////// ----------------------------
            ////// Step 3: Map domain events to integration events BEFORE persisting
            ////// This ensures UncommittedEvents are still available for mapping
            ////// ----------------------------
            ////var mappings = new Dictionary<Type, Func<BaseDomainEvent, BaseIntegrationEvent>>
            ////{
            ////    {
            ////        typeof(UserCreatedDomainEvent),
            ////        e => CreateUserMapper.ToIntegrationEvent((UserCreatedDomainEvent)e)
            ////    }
            ////    // Add other domain → integration mappings here if needed
            ////};

            ////var integrationEvents = uncommittedEvents
            ////    .MapToIntegrationEvents(
            ////        aggregate,
            ////        UserContext,
            ////        handlerName: nameof(CreateUserCommandHandler),
            ////        mappings: mappings
            ////    );

            // ----------------------------
            // Step 4: Persist aggregate + uncommitted domain events via Marten
            // Wolverine durable outbox participates in the same transaction
            // ----------------------------
            await Repository.PersistAsync(aggregate, ct).ConfigureAwait(false);

            // ----------------------------
            // Step 5: Publish integration events via Wolverine durable outbox
            // Messages are only dispatched after transaction commits
            // ----------------------------
            foreach (var evt in integrationEvents)
            {
                _logger.LogDebug(
                    "Queueing integration event for user {UserId} in Wolverine outbox",
                    evt.AggregateId
                );
                await _bus.PublishAsync(evt); // automatically queued in durable outbox
            }

            await Repository.Commmit(aggregate, ct).ConfigureAwait(false);

            // ----------------------------
            // Step 6: Return response to caller
            // ----------------------------
            _logger.LogInformation(
                "User {UserId} created successfully and integration events committed",
                aggregate.Id
            );

            return CreateUserMapper.FromAggregate(aggregate);
        }
    }
}
