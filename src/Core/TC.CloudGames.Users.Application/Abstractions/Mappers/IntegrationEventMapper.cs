using TC.CloudGames.Contracts.Events;
using TC.CloudGames.SharedKernel.Domain.Aggregate;
using TC.CloudGames.SharedKernel.Domain.Events;

namespace TC.CloudGames.Users.Application.Abstractions.Mappers
{
    public static class IntegrationEventMapper
    {
        /// <summary>
        /// Mapeia qualquer DomainEvent para um IntegrationEvent, usando a função de conversão fornecida,
        /// e cria EventContext pronto para publicação no bus.
        /// </summary>
        public static IEnumerable<EventContext<TIntegrationEvent, TAggregate>> MapToIntegrationEvents<TDomainEvent, TIntegrationEvent, TAggregate>(
            this IEnumerable<TDomainEvent> domainEvents,
            TAggregate aggregate,
            Func<TDomainEvent, TIntegrationEvent> mapFunc,
            IUserContext userContext,
            string source
        )
            where TDomainEvent : BaseDomainEvent
            where TIntegrationEvent : BaseIntegrationEvent
            where TAggregate : BaseAggregateRoot
        {

            ////var contexts = entity.Value.UncommittedEvents
            ////    .OfType<UserCreatedDomainEvent>()
            ////    .Select(e => EventContext<UserCreatedIntegrationEvent, UserAggregate>.Create(
            ////        data: CreateUserMapper.ToIntegrationEvent(e),
            ////        aggregateId: entity.Value.Id,
            ////        eventType: nameof(UserCreatedIntegrationEvent),
            ////        userId: UserContext.Id.ToString(),
            ////        isAuthenticated: UserContext.IsAuthenticated,
            ////        correlationId: UserContext.CorrelationId,
            ////        source: $"Users.API.{nameof(CreateUserCommandHandler)}.{nameof(UserCreatedIntegrationEvent)}.{nameof(CreateUserCommand)}"));

            return domainEvents.Select(e => EventContext<TIntegrationEvent, TAggregate>.Create(
                data: mapFunc(e),
                aggregateId: aggregate.Id,
                eventType: typeof(TIntegrationEvent).Name,
                userId: userContext.Id.ToString(),
                isAuthenticated: userContext.IsAuthenticated,
                correlationId: userContext.CorrelationId,
                source: source
            ));
        }
    }

}
