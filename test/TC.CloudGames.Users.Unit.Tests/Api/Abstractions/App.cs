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

        // Register fake CorrelationIdGenerator
        s.RemoveAll<ICorrelationIdGenerator>();
        s.AddSingleton(A.Fake<ICorrelationIdGenerator>());

        // Register Keyed Services for different user roles
        s.AddKeyedTransient($"{nameof(ValidUserContextAccessor)}.{AppConstants.AdminRole}", (sp, key) => ValidUserContextAccessor(sp, AppConstants.AdminRole));
        s.AddKeyedTransient($"{nameof(ValidUserContextAccessor)}.{AppConstants.UserRole}", (sp, key) => ValidUserContextAccessor(sp, AppConstants.UserRole));
        s.AddKeyedTransient($"{nameof(ValidUserContextAccessor)}.{AppConstants.UnknownRole}", (sp, key) => ValidUserContextAccessor(sp, AppConstants.UnknownRole));

        s.AddKeyedTransient($"{nameof(ValidLoggedUser)}.{AppConstants.AdminRole}", (sp, key) => ValidLoggedUser(sp, AppConstants.AdminRole));
        s.AddKeyedTransient($"{nameof(ValidLoggedUser)}.{AppConstants.UserRole}", (sp, key) => ValidLoggedUser(sp, AppConstants.UserRole));
        s.AddKeyedTransient($"{nameof(ValidLoggedUser)}.{AppConstants.UnknownRole}", (sp, key) => ValidLoggedUser(sp, AppConstants.UnknownRole));

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

    protected static IReadOnlyList<Claim> GetClaimsForRole(string userRole)
    {
        var userId = Guid.NewGuid().ToString();
        return userRole switch
        {
            AppConstants.AdminRole =>
            [
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Name, "Admin User"),
                new(JwtRegisteredClaimNames.Email, "admin@admin.com"),
                new(JwtRegisteredClaimNames.UniqueName, "adminuser"),
                new("role", AppConstants.AdminRole)
            ],
            AppConstants.UserRole =>
            [
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Name, "Regular User"),
                new(JwtRegisteredClaimNames.Email, "user@user.com"),
                new(JwtRegisteredClaimNames.UniqueName, "regularuser"),
                new("role", AppConstants.UserRole)
            ],
            _ =>
            [
                new(JwtRegisteredClaimNames.Sub, Guid.Empty.ToString()),
                new(JwtRegisteredClaimNames.Name, "Unknown User"),
                new(JwtRegisteredClaimNames.Email, "unknown@test.com"),
                new(JwtRegisteredClaimNames.UniqueName, "unknownuser"),
                new("role", AppConstants.UnknownRole)
            ]
        };
    }

    protected static IHttpContextAccessor ValidUserContextAccessor(IServiceProvider sp, string userRole)
    {
        var identity = new ClaimsIdentity(GetClaimsForRole(userRole), "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal,
            RequestServices = sp
        };
        return new HttpContextAccessor { HttpContext = httpContext };
    }

    protected static IUserContext ValidLoggedUser(IServiceProvider sp, string userRole)
    {
        var httpContextAccessor = sp.GetRequiredKeyedService<IHttpContextAccessor>($"{nameof(ValidUserContextAccessor)}.{userRole}");
        var correlationIdGenerator = sp.GetRequiredService<ICorrelationIdGenerator>();
        return new UserContext(httpContextAccessor, correlationIdGenerator);
    }

    internal IFusionCache GetCache() => Services.GetRequiredService<IFusionCache>();

    internal IHttpContextAccessor GetValidUserContextAccessor(string userRole = AppConstants.AdminRole)
    {
        return Services.GetRequiredKeyedService<IHttpContextAccessor>($"{nameof(ValidUserContextAccessor)}.{userRole}");
    }

    internal IUserContext GetValidLoggedUser(string userRole = AppConstants.AdminRole)
    {
        return Services.GetRequiredKeyedService<IUserContext>($"{nameof(ValidLoggedUser)}.{userRole}");
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
