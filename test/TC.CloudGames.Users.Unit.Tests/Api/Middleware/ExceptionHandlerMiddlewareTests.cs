using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TC.CloudGames.Users.Api.Middleware;

namespace TC.CloudGames.Users.Unit.Tests.Api.Middleware
{
    public class ExceptionHandlerMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithException_ReturnsProblemDetails()
        {
            var context = new DefaultHttpContext();
            var logger = A.Fake<ILogger<ExceptionHandlerMiddleware>>();
            var middleware = new ExceptionHandlerMiddleware(_ => throw new Exception("Test exception"), logger);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        }

        [Fact]
        public async Task InvokeAsync_WithValidationException_ReturnsBadRequest()
        {
            var context = new DefaultHttpContext();
            var logger = A.Fake<ILogger<ExceptionHandlerMiddleware>>();
            var validationException = new Users.Api.Exceptions.ValidationException([new() { ErrorMessage = "Validation failed" }]);
            var middleware = new ExceptionHandlerMiddleware(_ => throw validationException, logger);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        }
    }
}
