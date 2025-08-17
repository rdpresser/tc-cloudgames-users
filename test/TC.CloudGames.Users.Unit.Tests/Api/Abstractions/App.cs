using Marten;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TC.CloudGames.Users.Unit.Tests.Api.Abstractions;

public class App : AppFixture<Users.Api.Program>
{
    public App() { }

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

        // Register fake repository
        s.RemoveAll<IUserRepository>();
        s.AddSingleton(sp => A.Fake<IUserRepository>());

        s.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromSeconds(20);
                options.DistributedCacheDuration = TimeSpan.FromSeconds(30);
            });
    }

    public IFusionCache GetCache() => Services.GetRequiredService<IFusionCache>();

    public IHttpContextAccessor GetValidUserContextAccessor(string userRole = "Admin")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userRole == "Admin" ? "Admin User" : "Regular User"),
            new Claim(ClaimTypes.Email, userRole == "Admin" ? "admin@admin.com" : "user@user.com"),
            new Claim("role", userRole)
        }, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContextAccessor = new HttpContextAccessor();
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal,
            RequestServices = Services
        };
        httpContextAccessor.HttpContext = httpContext;
        return httpContextAccessor;
    }

    public IUserContext GetValidLoggedUser(string userRole = "Admin")
    {
        var httpContextAccessor = GetValidUserContextAccessor(userRole);
        return new UserContext(httpContextAccessor);
    }

    // Runs once after all tests in this fixture
    protected override ValueTask TearDownAsync()
    {
        // Example: Clean up test data, files, or resources
        return ValueTask.CompletedTask;
    }
}
