namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    internal sealed class CreateUserCommandHandler : BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>
    {
        private readonly IMessageBus _bus;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(IUserRepository repository, IUserContext userContext, IMessageBus bus, ILogger<CreateUserCommandHandler> logger)
            : base(repository, userContext)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus), "Message bus cannot be null");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        }

        public override async Task<Result<CreateUserResponse>> ExecuteAsync(CreateUserCommand command,
            CancellationToken ct = default)
        {
            // 1. Create Aggregate
            var entity = CreateUserMapper.ToAggregate(command);

            if (!entity.IsSuccess)
            {
                AddErrors(entity.ValidationErrors);
                return Result.Invalid(entity.ValidationErrors);
            }

            // 2. Append DomainEvents into Marten EventStore
            await Repository.InsertOrUpdateAsync(entity.Value.Id, ct, entity.Value.UncommittedEvents).ConfigureAwait(false);

            // 3. Map DomainEvents → IntegrationEvents
            var integrationContexts = entity.Value.UncommittedEvents
                .OfType<UserCreatedDomainEvent>()
                .MapToIntegrationEvents(
                    aggregate: entity.Value,
                    mapFunc: e => CreateUserMapper.ToIntegrationEvent(e),
                    userContext: UserContext,
                    source: $"Users.API.{nameof(CreateUserCommandHandler)}.{nameof(UserCreatedIntegrationEvent)}.{nameof(CreateUserCommand)}"
                );

            // 3.1 Publish IntegrationEvents to the MessageBus - append into Outbox (in the same transaction as EventStore)
            foreach (var ctx in integrationContexts)
            {
                var envelope = EventEnvelope<UserCreatedIntegrationEvent, UserAggregate>.CreateForDomainEvent(ctx);
                _logger.LogDebug("Publishing envelope Id {EnvelopeId} with routing key {RoutingKey}", envelope.EnvelopeId, envelope.RoutingKey);
                await _bus.PublishAsync(envelope);
            }

            // 4. Single Commit (EventStore + Outbox = Publish Integration Events)
            await Repository.SaveAsync(entity.Value, ct).ConfigureAwait(false);

            return CreateUserMapper.FromAggregate(entity);
        }
    }
}
