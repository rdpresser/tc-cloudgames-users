using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
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
        public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app)
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
            })
            .UseSwaggerGen(uiConfig: u =>
            {
                u.DocumentPath = "v1/swagger.json";
            });

            // Middleware to dynamically rewrite swagger.json servers URL based on PathBase
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value?.EndsWith("/swagger/v1/swagger.json") == true)
                {
                    var originalBody = context.Response.Body;
                    using (var memoryStream = new MemoryStream())
                    {
                        context.Response.Body = memoryStream;
                        await next();

                        if (context.Response.StatusCode == 200 && memoryStream.Length > 0)
                        {
                            memoryStream.Position = 0;
                            using (var reader = new StreamReader(memoryStream))
                            {
                                var jsonContent = await reader.ReadToEndAsync();
                                var pathBase = context.Request.PathBase.Value;

                                if (!string.IsNullOrWhiteSpace(pathBase) && pathBase != "/")
                                {
                                    // Rewrite servers URL to include PathBase
                                    // Change "url":"http:// to "url":"/pathBase + "http://
                                    jsonContent = jsonContent.Replace(
                                        "\"url\":\"http://",
                                        $"\"url\":\"{pathBase}http://"
                                    );
                                    jsonContent = jsonContent.Replace(
                                        "\"url\":\"https://",
                                        $"\"url\":\"{pathBase}https://"
                                    );
                                }

                                var responseBytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                                context.Response.ContentLength = responseBytes.Length;
                                await originalBody.WriteAsync(responseBytes);
                            }
                        }
                        else
                        {
                            memoryStream.Position = 0;
                            await memoryStream.CopyToAsync(originalBody);
                        }
                    }
                    context.Response.Body = originalBody;
                }
                else
                {
                    await next();
                }
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
