using TC.CloudGames.SharedKernel.Infrastructure.Database;
using TC.CloudGames.SharedKernel.Infrastructure.Middleware;
using Wolverine.Marten;

namespace TC.CloudGames.Users.Unit.Tests.Api.Abstractions;

public class App : AppFixture<Users.Api.Program>
{
    public App()
    {
        ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
        ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
        ValidatorOptions.Global.ErrorCodeResolver = validator => validator.Name;
        ValidatorOptions.Global.LanguageManager = new LanguageManager
        {
            Enabled = true,
            Culture = new System.Globalization.CultureInfo("en")
        };
    }

    // Runs once before any tests in this fixture
    protected override ValueTask SetupAsync()
    {
        // Example: Seed test data, set up test files, etc.
        return ValueTask.CompletedTask;
    }

    // Configure the web host builder before the app starts
    protected override void ConfigureApp(IWebHostBuilder a)
    {
        // Example: Use a different environment for testing
        a.UseEnvironment("Testing");
        // You can also configure test-specific settings here
    }

    protected override void ConfigureServices(IServiceCollection s)
    {
        // Remove Marten registration
        var martenDescriptors = s.Where(d => d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("Marten")).ToList();
        foreach (var descriptor in martenDescriptors)
            s.Remove(descriptor);

        // Register fakes for all required Marten types
        s.AddSingleton(sp => A.Fake<IDocumentStore>());
        s.AddSingleton(sp => A.Fake<IQuerySession>());
        s.AddSingleton(sp => A.Fake<IDocumentSession>());
        s.AddSingleton(sp => A.Fake<ISessionFactory>());
        s.AddSingleton(sp => A.Fake<IConfigureMarten>());
        s.AddSingleton(sp => A.Fake<StoreOptions>());

        // Register fake CacheService
        s.RemoveAll<ICacheService>();
        s.AddSingleton(sp => A.Fake<ICacheService>());

        s.RemoveAll<IFusionCache>();
        s.AddSingleton(sp => A.Fake<IFusionCache>());

        // Register fake repository
        s.RemoveAll<IUserRepository>();
        s.AddSingleton<IUserRepository, Fakes.FakeUserRepository>();

        // Decouple UserContext and HttpContextAccessor from the service provider during creation
        var fakeCorrelationIdGenerator = A.Fake<ICorrelationIdGenerator>();
        
        s.RemoveAll<ICorrelationIdGenerator>();
        s.AddSingleton(fakeCorrelationIdGenerator);

        s.RemoveAll<IUserContext>();
        s.AddSingleton<IUserContext>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new UserContext(httpContextAccessor, fakeCorrelationIdGenerator);
        });

        s.RemoveAll<IHttpContextAccessor>();
        s.AddSingleton<IHttpContextAccessor>(sp =>
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Name, "Admin User"),
                new Claim(JwtRegisteredClaimNames.Email, "admin@admin.com"),
                new Claim(JwtRegisteredClaimNames.UniqueName, "adminuser"),
                new Claim("role", "Admin")
            }, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal,
                RequestServices = sp
            };
            return new HttpContextAccessor { HttpContext = httpContext };
        });

        // Mock IConnectionStringProvider to prevent real DB calls
        s.RemoveAll<IConnectionStringProvider>();
        s.AddSingleton(sp => A.Fake<IConnectionStringProvider>());

        // Add missing mocks for handlers/services
        s.RemoveAll<ITokenProvider>();
        s.AddSingleton(sp => A.Fake<ITokenProvider>());
        s.RemoveAll<IMartenOutbox>();
        s.AddSingleton(sp => A.Fake<IMartenOutbox>());
        s.RemoveAll<ILogger<CreateUserCommandHandler>>();
        s.AddSingleton(sp => A.Fake<ILogger<CreateUserCommandHandler>>());
    }

    protected IFusionCache GetCache() => Services.GetRequiredService<IFusionCache>();

    public IHttpContextAccessor GetValidUserContextAccessor(string userRole = "Admin")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, userRole == "Admin" ? "Admin User" : "Regular User"),
            new Claim(JwtRegisteredClaimNames.Email, userRole == "Admin" ? "admin@admin.com" : "user@user.com"),
            new Claim(JwtRegisteredClaimNames.UniqueName, userRole == "Admin" ? "adminuser" : "regularuser"),
            new Claim("role", userRole)
        }, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal,
                // RequestServices is the key part. We avoid setting it here.
                // The test server will set it on the HttpContext when a request is processed.
            }
        };
        return httpContextAccessor;
    }

    public IUserContext GetValidLoggedUser(string userRole = "Admin")
    {
        // This method is now simplified and can be called outside of DI resolution
        var httpContextAccessor = GetValidUserContextAccessor(userRole);
        var correlationId = A.Fake<ICorrelationIdGenerator>(); // Use a fake directly
        return new UserContext(httpContextAccessor, correlationId);
    }

    public static IEnumerable<(string Identifier, int Count, IEnumerable<string> ErrorCodes)> GroupValidationErrorsByIdentifier(IEnumerable<ValidationError> errors)
    {
        return errors
            .GroupBy(e => e.Identifier)
            .Select(g => (
                Identifier: g.Key,
                Count: g.Count(),
                ErrorCodes: g.Select(e => $"{e.ErrorCode} - {e.ErrorMessage}")
            ));
    }

    // Runs once after all tests in this fixture
    protected override ValueTask TearDownAsync()
    {
        // Example: Clean up test data, files, or resources
        return ValueTask.CompletedTask;
    }
}
