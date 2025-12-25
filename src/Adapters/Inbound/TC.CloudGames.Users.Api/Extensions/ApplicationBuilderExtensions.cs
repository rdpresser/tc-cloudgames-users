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
        // CRITICAL: When nginx rewrite-target is used with X-Forwarded-Prefix header,
        // this middleware restores the original path for the application context.
        public static IApplicationBuilder UseIngressPathBase(this IApplicationBuilder app, IConfiguration configuration)
        {
            var configuredBasePath = configuration["ASPNETCORE_APPL_PATH"] ?? configuration["PathBase"];

            if (!string.IsNullOrWhiteSpace(configuredBasePath))
            {
                app.UsePathBase(configuredBasePath);
            }

            app.Use(async (context, next) =>
            {
                // Priority 1: Check X-Forwarded-Prefix header (set by nginx with rewrite-target)
                if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixValues))
                {
                    var prefix = prefixValues.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        var normalized = prefix.StartsWith('/') ? prefix : $"/{prefix}";
                        normalized = normalized.TrimEnd('/');

                        context.Request.PathBase = new PathString(normalized);

                        // Store in HttpContext.Items for Swagger to access
                        context.Items["OriginalPathBase"] = normalized;
                    }
                }
                // Priority 2: Check nginx X-Original-URI (contains full path before rewrite)
                else if (context.Request.Headers.TryGetValue("X-Original-URI", out var originalUriValues))
                {
                    var originalUri = originalUriValues.FirstOrDefault() ?? string.Empty;
                    
                    // Extract prefix from original URI (e.g., /user/swagger/... -> /user)
                    if (!string.IsNullOrWhiteSpace(originalUri))
                    {
                        var segments = originalUri.TrimStart('/').Split('/');
                        if (segments.Length > 0 && segments[0] != "api" && segments[0] != "swagger")
                        {
                            var pathBase = $"/{segments[0]}";
                            context.Request.PathBase = new PathString(pathBase);
                            context.Items["OriginalPathBase"] = pathBase;
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

            // Normalize pathBase once for reuse in OpenAPI and SwaggerUI
            // Handles: "user", "/user", "/user/", "/", "  ", null, empty
            var normalizedPathBase = NormalizePathBase(pathBase);

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
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = NormalizePathBase(requestPathBase) });
                    }
                    else if (!string.IsNullOrEmpty(normalizedPathBase))
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = normalizedPathBase });
                    }
                    else
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = "/" });
                    }
                };
            });

            // Enable Swagger UI
            // CRITICAL: Use ABSOLUTE URL with PathBase prefix because NSwag's relative URL
            // resolution doesn't work correctly with nginx rewrite-target
            // The PathBase is already known from ASPNETCORE_APPL_PATH environment variable
            app.UseSwaggerUi(c =>
            {
                c.SwaggerRoutes.Clear();
                
                // Build swagger.json path with normalized PathBase
                var swaggerJsonPath = string.IsNullOrEmpty(normalizedPathBase)
                    ? "/swagger/v1/swagger.json"
                    : $"{normalizedPathBase}/swagger/v1/swagger.json";
                
                c.SwaggerRoutes.Add(new SwaggerUiRoute("v1", swaggerJsonPath));
                
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

        /// <summary>
        /// Normalizes a path base string to ensure it has a leading slash and no trailing slash.
        /// Handles edge cases: "user", "/user", "/user/", "/", "  ", null, empty string.
        /// </summary>
        /// <param name="pathBase">The path base to normalize.</param>
        /// <returns>Normalized path base (e.g., "/user") or empty string if invalid.</returns>
        private static string NormalizePathBase(string? pathBase)
        {
            if (string.IsNullOrWhiteSpace(pathBase))
            {
                return string.Empty;
            }

            // Trim leading and trailing slashes, then whitespace
            var trimmed = pathBase.Trim().Trim('/');
            
            // If nothing left after trimming (e.g., "/" or "  "), return empty
            if (string.IsNullOrEmpty(trimmed))
            {
                return string.Empty;
            }

            // Rebuild with single leading slash, no trailing slash
            return $"/{trimmed}";
        }
    }
}
