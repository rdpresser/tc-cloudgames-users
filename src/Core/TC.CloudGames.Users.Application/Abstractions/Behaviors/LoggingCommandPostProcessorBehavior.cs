﻿namespace TC.CloudGames.Users.Application.Abstractions.Behaviors
{
    [ExcludeFromCodeCoverage]
    public class LoggingCommandPostProcessorBehavior<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
        where TRequest : IBaseCommand<TResponse>
        where TResponse : class
    {
        public Task PostProcessAsync(IPostProcessorContext<TRequest, TResponse> context, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(context);

            var logger = context.HttpContext.Resolve<ILogger<TRequest>>();

            var genericType = logger.GetType().GenericTypeArguments.FirstOrDefault()?.Name ?? "Unknown";
            var name = context.Request?.GetType().Name
                       ?? genericType;

            if (!context.HasValidationFailures)
            {
                var responseValues = new
                {
                    context.Request,
                    context.Response,
                };

                using (LogContext.PushProperty("Content", responseValues, true))
                {
                    logger.LogInformation("Post-processing Request {Request} executed successfully", name);
                }
            }
            else
            {
                var responseValues = new
                {
                    context.Request,
                    context.Response,
                    Error = context.ValidationFailures
                };

                using (LogContext.PushProperty("Content", responseValues, true))
                {
                    logger.LogError("Post-processing Request {Request} validation failed with error", name);
                }
            }

            return Task.CompletedTask;
        }
    }
}
