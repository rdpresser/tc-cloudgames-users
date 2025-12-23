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

            // Enable OpenAPI with dynamic server URL based on PathBase from request
            app.UseOpenApi(o =>
            {
                o.PostProcess = (doc, req) =>
                {
                    doc.Servers.Clear();
                    
                    // Get the request PathBase (set by UseIngressPathBase middleware from X-Forwarded-Prefix or configured pathBase)
                    var requestPathBase = req.HttpContext.Request.PathBase.ToString();
                    
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
            // The swagger.json URL path is relative to root, and UseIngressPathBase middleware
            // ensures that Request.PathBase is set correctly from ASPNETCORE_APPL_PATH or X-Forwarded-Prefix header
            app.UseSwaggerUi(c =>
            {
                c.SwaggerRoutes.Clear();
                // Use the absolute path to swagger.json
                // The middleware UseIngressPathBase will handle the PathBase correctly
                c.SwaggerRoutes.Add(new SwaggerUiRoute("v1", "/swagger/v1/swagger.json"));
                c.ConfigureDefaults();
            });

            // Add middleware to rewrite Swagger UI spec URL with correct PathBase
            app.Use(async (context, next) =>
            {
                // Only apply to Swagger UI requests
                if (context.Request.Path.StartsWithSegments("/swagger/index.html") || 
                    context.Request.Path.StartsWithSegments("/swagger/ui"))
                {
                    var pathBase = context.Request.PathBase.ToString();
                    
                    // If pathBase is set, we need to rewrite the swagger.json URL in the response
                    if (!string.IsNullOrWhiteSpace(pathBase))
                    {
                        // Capture the original response body
                        var originalBody = context.Response.Body;
                        using (var memoryStream = new MemoryStream())
                        {
                            context.Response.Body = memoryStream;
                            
                            await next().ConfigureAwait(false);
                            
                            // Read the response content
                            memoryStream.Position = 0;
                            using (var streamReader = new StreamReader(memoryStream))
                            {
                                var html = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                                
                                // Rewrite the swagger.json URL to include PathBase
                                html = html.Replace(
                                    "\"/swagger/v1/swagger.json\"",
                                    $"\"{pathBase}/swagger/v1/swagger.json\"" 
                                );
                                html = html.Replace(
                                    "urls: [{\"url\":\"/./swagger/v1/swagger.json\"",
                                    $"urls: [{{\"url\":\"{pathBase}/swagger/v1/swagger.json\""
                                );
                                
                                // Write the modified content to the original response
                                using (var streamWriter = new StreamWriter(originalBody))
                                {
                                    await streamWriter.WriteAsync(html).ConfigureAwait(false);
                                }
                            }
                        }
                        return;
                    }
                }
                
                await next().ConfigureAwait(false);
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
