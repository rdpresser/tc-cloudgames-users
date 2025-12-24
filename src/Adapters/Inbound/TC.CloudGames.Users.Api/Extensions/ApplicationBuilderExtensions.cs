using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using NSwag;
using NSwag.AspNetCore;
using TC.CloudGames.SharedKernel.Infrastructure.Database.Initializer;

namespace TC.CloudGames.Users.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            return app;
        }

        // Normalizes PathBase when the service runs behind an ingress with a path prefix (e.g. /user)
        public static IApplicationBuilder UseIngressPathBase(this IApplicationBuilder app, IConfiguration configuration)
        {
            var configuredBasePath = configuration["ASPNETCORE_APPL_PATH"] ?? configuration["PathBase"];

            if (!string.IsNullOrWhiteSpace(configuredBasePath))
            {
                app.UsePathBase(configuredBasePath);
            }

            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixValues))
                {
                    var prefix = prefixValues.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        var normalized = prefix.StartsWith('/') ? prefix : $"/{prefix}";
                        normalized = normalized.TrimEnd('/');

                        context.Request.PathBase = new PathString(normalized);

                        if (context.Request.Path.StartsWithSegments(context.Request.PathBase, out var remaining))
                        {
                            context.Request.Path = remaining;
                        }
                    }
                }

                await next().ConfigureAwait(false);
            });

            return app;
        }

        // Configures FastEndpoints with custom settings and Swagger generation
        public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseFastEndpoints(c =>
            {
                c.Security.RoleClaimType = "role";
                c.Endpoints.RoutePrefix = "api";
                c.Endpoints.ShortNames = true;
                c.Errors.ProducesMetadataType = typeof(Microsoft.AspNetCore.Mvc.ProblemDetails);
                c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
                {
                    var errors = failures.Select(f => new
                    {
                        name = f.PropertyName.ToPascalCaseFirst(),
                        reason = f.ErrorMessage,
                        code = f.ErrorCode
                    }).ToArray();

                    string title = statusCode switch
                    {
                        400 => "Validation Error",
                        404 => "Not Found",
                        403 => "Forbidden",
                        _ => "One or more errors occurred!"
                    };

                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = statusCode,
                        Instance = ctx.Request.Path.Value ?? string.Empty,
                        Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
                        Title = title,
                    };

                    problemDetails.Extensions["traceId"] = ctx.TraceIdentifier;
                    problemDetails.Extensions["errors"] = errors;

                    return problemDetails;
                };
            });

            // Get default PathBase from environment for server URL
            var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH")
                ?? configuration["ASPNETCORE_APPL_PATH"]
                ?? configuration["PathBase"]
                ?? string.Empty;

            // Enable OpenAPI/Swagger with proper PathBase handling
            // Key: UseOpenApi MUST be called AFTER UsePathBase middleware (UseIngressPathBase)
            // so that PathBase is already set in the request context
            app.UseOpenApi(o =>
            {
                // This PostProcess runs for each request, using the current request's PathBase
                o.PostProcess = (doc, req) =>
                {
                    doc.Servers.Clear();
                    
                    var requestPathBase = req.HttpContext.Request.PathBase.ToString();
                    
                    // Use the request PathBase (dynamic, from X-Forwarded-Prefix or UsePathBase)
                    if (!string.IsNullOrWhiteSpace(requestPathBase))
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = requestPathBase });
                    }
                    else if (!string.IsNullOrWhiteSpace(pathBase))
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = pathBase });
                    }
                    else
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = "/" });
                    }
                };
            });

            // Enable Swagger UI
            // CRITICAL: Use relative URL pattern that NSwag expects
            // The "/" prefix tells NSwag to resolve relative to the current request PathBase
            app.UseSwaggerUi(c =>
            {
                c.SwaggerRoutes.Clear();
                
                // Use root-relative path for swagger.json
                // NSwag will automatically prepend the current PathBase
                c.SwaggerRoutes.Add(new SwaggerUiRoute("v1", "/swagger/v1/swagger.json"));
                
                c.ConfigureDefaults();
            });

            return app;
        }

        // Configures custom middlewares including HTTPS redirection, exception handling, correlation, logging, and health checks
        public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app)
        {
            // Enables proxy headers (important for ACA)
            ////app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });

            app.UseCustomExceptionHandler()
                .UseCorrelationMiddleware()
                .UseMiddleware<TelemetryMiddleware>() // Add telemetry middleware after correlation
                .UseSerilogRequestLogging()
                .UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/live", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                // Add Prometheus metrics endpoint
                .UseOpenTelemetryPrometheusScrapingEndpoint("/metrics");

            return app;
        }

        public async static Task<IApplicationBuilder> CreateMessageDatabase(this IApplicationBuilder app)
        {
            // Ensure outbox database exists before Wolverine is used
            var connProvider = app.ApplicationServices.GetRequiredService<IConnectionStringProvider>();
            await PostgresDatabaseHelper.EnsureDatabaseExists(connProvider);

            return app;
        }
    }
}
