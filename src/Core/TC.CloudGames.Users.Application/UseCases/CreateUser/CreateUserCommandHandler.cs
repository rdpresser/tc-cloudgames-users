using TC.CloudGames.SharedKernel.Application.Handlers;
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
        protected override Task<Result<UserAggregate>> MapCommandToAggregateAsync(CreateUserCommand command, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Starting aggregate mapping for user creation with email: {Email}", command.Email);
                
                var aggregateResult = CreateUserMapper.ToAggregate(command);
                if (!aggregateResult.IsSuccess)
                {
                    _logger.LogWarning("Aggregate mapping failed for email {Email}. Validation errors: {Errors}", 
                        command.Email, 
                        string.Join(", ", aggregateResult.ValidationErrors.Select(e => $"{e.Identifier}: {e.ErrorMessage}")));
                    
                    return Task.FromResult(Result<UserAggregate>.Invalid(aggregateResult.ValidationErrors));
                }

                _logger.LogDebug("Aggregate mapping successful for user {UserId}", aggregateResult.Value.Id);
                return Task.FromResult(Result<UserAggregate>.Success(aggregateResult.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during aggregate mapping for email {Email}. Error: {ErrorMessage}", 
                    command.Email, ex.Message);
                throw new InvalidOperationException($"Failed to map command to aggregate for email {command.Email}", ex);
            }
        }

        /// <summary>
        /// Validates the aggregate.
        /// Example: cross-entity checks, uniqueness rules, or custom domain invariants.
        /// </summary>
        protected override Task<Result> ValidateAggregateAsync(UserAggregate aggregate, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Starting aggregate validation for user {UserId}", aggregate.Id);
                
                // For now, no extra validation beyond the aggregate factory
                // Validate Email/Username uniqueness here if needed (Future enhancement)
                
                _logger.LogDebug("Aggregate validation completed successfully for user {UserId}", aggregate.Id);
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during aggregate validation for user {UserId}. Error: {ErrorMessage}", 
                    aggregate.Id, ex.Message);
                throw new InvalidOperationException($"Failed to validate aggregate for user {aggregate.Id}", ex);
            }
        }

        /// <summary>
        /// Publishes integration events through Wolverine Outbox.
        /// Maps domain events -> integration events and wraps them in EventContext.
        /// </summary>
        protected override async Task PublishIntegrationEventsAsync(UserAggregate aggregate, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Starting integration events publication for user {UserId}. Uncommitted events count: {EventCount}", 
                    aggregate.Id, aggregate.UncommittedEvents.Count);

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

                _logger.LogDebug("Mapped {EventCount} integration events for user {UserId}", 
                    integrationEvents.Count(), aggregate.Id);

                foreach (var evt in integrationEvents)
                {
                    try
                    {
                        _logger.LogDebug(
                            "Queueing integration event {EventType} for user {UserId} with correlation {CorrelationId} in Marten outbox",
                            evt.EventData.GetType().Name,
                            evt.AggregateId,
                            evt.CorrelationId);

                        await _outbox.PublishAsync(evt).ConfigureAwait(false);
                        
                        _logger.LogDebug("Successfully queued integration event {EventType} for user {UserId}", 
                            evt.EventData.GetType().Name, evt.AggregateId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Failed to queue integration event {EventType} for user {UserId} with correlation {CorrelationId}. Error: {ErrorMessage}",
                            evt.EventData.GetType().Name,
                            evt.AggregateId,
                            evt.CorrelationId,
                            ex.Message);
                        throw new InvalidOperationException($"Failed to publish integration event {evt.EventData.GetType().Name} for user {evt.AggregateId}", ex);
                    }
                }

                _logger.LogInformation("Successfully published {EventCount} integration events for user {UserId}", 
                    integrationEvents.Count(), aggregate.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during integration events publication for user {UserId}. Error: {ErrorMessage}", 
                    aggregate.Id, ex.Message);
                throw new InvalidOperationException($"Failed to publish integration events for user {aggregate.Id}", ex);
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
            var operationId = Guid.NewGuid().ToString("N")[..8];
            
            _logger.LogInformation("Starting user creation operation {OperationId} for email {Email} by user {CurrentUserId}", 
                operationId, command.Email, UserContext.Id);

            try
            {
                // 1. Map command -> aggregate
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Mapping command to aggregate for operation {OperationId}", 
                    1, 6, operationId);
                
                var mapResult = await MapCommandToAggregateAsync(command, ct).ConfigureAwait(false);
                if (!mapResult.IsSuccess)
                {
                    _logger.LogWarning("Operation {OperationId} failed at mapping step. Validation errors: {Errors}",
                        operationId,
                        string.Join(", ", mapResult.ValidationErrors.Select(e => $"{e.Identifier}: {e.ErrorMessage}")));
                    
                    AddErrors(mapResult.ValidationErrors);
                    return BuildValidationErrorResult();
                }

                var aggregate = mapResult.Value;
                _logger.LogDebug("Step {StepNumber}/{StepTotal} completed - Aggregate {UserId} created for operation {OperationId}", 
                    1, 6, aggregate.Id, operationId);

                // 2. Validate aggregate (optional custom rules)
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Validating aggregate {UserId} for operation {OperationId}", 
                    2, 6, aggregate.Id, operationId);
                
                var validationResult = await ValidateAggregateAsync(aggregate, ct).ConfigureAwait(false);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Operation {OperationId} failed at validation step for user {UserId}. Validation errors: {Errors}",
                        operationId, aggregate.Id,
                        string.Join(", ", validationResult.ValidationErrors.Select(e => $"{e.Identifier}: {e.ErrorMessage}")));
                    
                    AddErrors(validationResult.ValidationErrors);
                    return BuildValidationErrorResult();
                }

                _logger.LogDebug("Step {StepNumber}/{StepTotal} completed - Aggregate validation passed for user {UserId} operation {OperationId}", 
                    2, 6, aggregate.Id, operationId);

                // 3. Persist aggregate events (event sourcing)
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Persisting aggregate events for user {UserId} operation {OperationId}", 
                    3, 6, aggregate.Id, operationId);
                
                try
                {
                    await Repository.SaveAsync(aggregate, ct).ConfigureAwait(false);
                    _logger.LogDebug("Step {StepNumber}/{StepTotal} completed - Events saved for user {UserId} operation {OperationId}", 
                        3, 6, aggregate.Id, operationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Step {StepNumber}/{StepTotal} failed - Error saving aggregate events for user {UserId} operation {OperationId}. Repository type: {RepositoryType}. Error: {ErrorMessage}",
                        3, 6, aggregate.Id, operationId, Repository.GetType().Name, ex.Message);
                    throw new InvalidOperationException($"Failed to save events for user {aggregate.Id} in operation {operationId}", ex);
                }

                // 4. Publish integration events via outbox
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Publishing integration events for user {UserId} operation {OperationId}", 
                    4, 6, aggregate.Id, operationId);
                
                try
                {
                    await PublishIntegrationEventsAsync(aggregate, ct).ConfigureAwait(false);
                    _logger.LogDebug("Step {StepNumber}/{StepTotal} completed - Integration events published for user {UserId} operation {OperationId}", 
                        4, 6, aggregate.Id, operationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Step {StepNumber}/{StepTotal} failed - Error publishing integration events for user {UserId} operation {OperationId}. Outbox type: {OutboxType}. Error: {ErrorMessage}",
                        4, 6, aggregate.Id, operationId, _outbox.GetType().Name, ex.Message);
                    throw new InvalidOperationException($"Failed to publish events for user {aggregate.Id} in operation {operationId}", ex);
                }

                // 5. Commit session (persist + flush outbox atomically)
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Committing session for user {UserId} operation {OperationId}", 
                    5, 6, aggregate.Id, operationId);
                
                try
                {
                    await Repository.CommitAsync(aggregate, ct).ConfigureAwait(false);
                    _logger.LogDebug("Step {StepNumber}/{StepTotal} completed - Session committed for user {UserId} operation {OperationId}", 
                        5, 6, aggregate.Id, operationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Step {StepNumber}/{StepTotal} failed - Error committing session for user {UserId} operation {OperationId}. Repository type: {RepositoryType}. Error: {ErrorMessage}",
                        5, 6, aggregate.Id, operationId, Repository.GetType().Name, ex.Message);
                    throw new InvalidOperationException($"Failed to commit session for user {aggregate.Id} in operation {operationId}", ex);
                }

                _logger.LogInformation("User {UserId} created successfully and events committed for operation {OperationId}", 
                    aggregate.Id, operationId);

                // 6. Map response
                _logger.LogDebug("Step {StepNumber}/{StepTotal} - Mapping response for user {UserId} operation {OperationId}", 
                    6, 6, aggregate.Id, operationId);
                
                try
                {
                    var response = CreateUserMapper.FromAggregate(aggregate);
                    _logger.LogInformation("Operation {OperationId} completed successfully. User {UserId} created with email {Email}", 
                        operationId, aggregate.Id, command.Email);
                    
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Step {StepNumber}/{StepTotal} failed - Error mapping response for user {UserId} operation {OperationId}. Error: {ErrorMessage}",
                        6, 6, aggregate.Id, operationId, ex.Message);
                    throw new InvalidOperationException($"Failed to map response for user {aggregate.Id} in operation {operationId}", ex);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Operation {OperationId} was cancelled for email {Email}. Cancellation reason: {CancellationReason}", 
                    operationId, command.Email, ex.Message);
                throw new OperationCanceledException($"User creation operation {operationId} was cancelled", ex);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Operation {OperationId} timed out for email {Email}. Timeout details: {TimeoutMessage}", 
                    operationId, command.Email, ex.Message);
                throw new TimeoutException($"User creation operation {operationId} timed out", ex);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during user creation {OperationId} for email {Email}. Details: {ErrorMessage}", 
                    operationId, command.Email, ex.Message);
                throw new InvalidOperationException($"Invalid operation during user creation {operationId}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access during user creation {OperationId} for email {Email}. User: {CurrentUserId}", 
                    operationId, command.Email, UserContext.Id);
                throw new UnauthorizedAccessException($"Unauthorized access during user creation {operationId}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Critical error during user creation operation {OperationId} for email {Email}. Current user: {CurrentUserId}. Error type: {ErrorType}",
                    operationId, command.Email, UserContext.Id, ex.GetType().Name);
                throw new InvalidOperationException($"Critical error during user creation operation {operationId}", ex);
            }
        }
    }
}
