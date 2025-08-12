namespace TC.CloudGames.Users.Unit.Tests.Common;

/// <summary>
/// Custom AutoFakeItEasy data attribute for xUnit theories with domain-specific customizations
/// </summary>
public class AutoFakeItEasyDataAttribute : AutoDataAttribute
{
    public AutoFakeItEasyDataAttribute() : base(() =>
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoFakeItEasyCustomization());

        // Customize Value Objects creation
        fixture.Register(() => Email.Create($"test{fixture.Create<int>()}@example.com").Value);
        fixture.Register(() => Password.Create("TestPassword123!").Value);
        fixture.Register(() => Role.Create("User").Value);

        return fixture;
    })
    {

    }
}

/// <summary>
/// Domain-specific test data builder for UserAggregate tests
/// </summary>
public class UserAggregateBuilder
{
    private readonly Fixture _fixture;
    private string _name;
    private Email _email;
    private string _username;
    private Password _password;
    private Role _role;

    public UserAggregateBuilder()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoFakeItEasyCustomization());

        // Set sensible defaults
        _name = GenerateValidName();
        _email = Email.Create("builder@test.com").Value;
        _username = GenerateValidUsername();
        _password = Password.Create("BuilderPassword123!").Value;
        _role = Role.Create("User").Value;
    }

    public UserAggregateBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserAggregateBuilder WithEmail(string email)
    {
        _email = Email.Create(email).Value;
        return this;
    }

    public UserAggregateBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserAggregateBuilder WithPassword(string password)
    {
        _password = Password.Create(password).Value;
        return this;
    }

    public UserAggregateBuilder WithRole(string role)
    {
        _role = Role.Create(role).Value;
        return this;
    }

    public Result<UserAggregate> Build()
    {
        // Defensive: ensure all required fields are non-null and valid
        if (string.IsNullOrWhiteSpace(_name))
            _name = "Test User";
        if (_email == null)
            _email = Email.Create("builder@test.com").Value;
        if (string.IsNullOrWhiteSpace(_username))
            _username = "testuser";
        if (_password == null)
            _password = Password.Create("BuilderPassword123!").Value;
        if (_role == null)
            _role = Role.Create("User").Value;
        var result = UserAggregate.Create(_name, _email, _username, _password, _role);
        if (!result.IsSuccess && result.ValidationErrors != null)
        {
            foreach (var error in result.ValidationErrors)
            {
                Console.WriteLine($"[DEBUG] UserAggregateBuilder.Build() error: {error.Identifier} - {error.ErrorMessage}");
            }
        }
        if (!result.IsSuccess)
        {
            throw new Exception($"UserAggregateBuilder.Build() failed: {string.Join(", ", result.ValidationErrors.Select(e => e.Identifier + ": " + e.ErrorMessage))}");
        }
        return result;
    }

    public Result<UserAggregate> BuildFromPrimitives()
    {
        return UserAggregate.CreateFromPrimitives(_name, _email.Value, _username, "BuilderPassword123!", _role.Value);
    }

    private string GenerateValidName()
    {
        // Always return a valid, non-null, non-empty name within 1-100 chars
        return "Test User";
    }

    private string GenerateValidUsername()
    {
        // Always return a valid username within 3-50 chars, only allowed chars
        return "testuser";
    }
}

/// <summary>
/// Extension methods for tests to handle common test scenarios
/// </summary>
[ExcludeFromCodeCoverage]
public static class TestExtensions
{
    /// <summary>
    /// Extension to handle nullable DateTime comparison
    /// </summary>
    public static void ShouldBeCloseTo(this DateTime? actual, DateTime expected, TimeSpan tolerance)
    {
        actual.ShouldNotBeNull();
        actual.Value.ShouldBeCloseTo(expected, tolerance);
    }

    /// <summary>
    /// Extension to handle non-nullable DateTime comparison (using Shouldly's built-in method)
    /// </summary>
    public static void ShouldBeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance)
    {
        Math.Abs((actual - expected).TotalMilliseconds).ShouldBeLessThan(tolerance.TotalMilliseconds);
    }
}