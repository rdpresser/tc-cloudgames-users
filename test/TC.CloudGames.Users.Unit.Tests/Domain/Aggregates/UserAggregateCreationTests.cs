using TC.CloudGames.Users.Unit.Tests.Common;
using Xunit;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateCreationTests
{
    [Theory, AutoFakeItEasyData]
    public void Create_WithValidData_ShouldSucceed(string name, string username)
    {
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "testuser" : username[..Math.Min(10, username.Length)];
        var result = UserAggregate.Create(name, email, username, password, role);
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.Username.ShouldBe(username);
        user.PasswordHash.ShouldBe(password);
        user.Role.ShouldBe(role);
        user.IsActive.ShouldBeTrue();
        user.CreatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.ShouldBeNull();
        user.UncommittedEvents.ShouldHaveSingleItem();
        user.UncommittedEvents.First().ShouldBeOfType<UserCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldFail(string invalidName)
    {
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(invalidName, email, username, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldFail()
    {
        var name = new string('a', 201);
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(name, email, username, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidUsername_ShouldFail(string invalidUsername)
    {
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(name, email, invalidUsername, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.Required");
    }

    [Fact]
    public void Create_WithUsernameTooShort_ShouldFail()
    {
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var username = "ab";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(name, email, username, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.TooShort");
    }

    [Fact]
    public void Create_WithUsernameTooLong_ShouldFail()
    {
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var username = "a" + new string('x', 50);
        var result = UserAggregate.Create(name, email, username, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.TooLong");
    }

    [Theory]
    [InlineData("invalid@username")]
    [InlineData("invalid username")]
    [InlineData("invalid#username")]
    [InlineData("invalid$username")]
    public void Create_WithInvalidUsernameFormat_ShouldFail(string invalidUsername)
    {
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(name, email, invalidUsername, password, role);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.InvalidFormat");
    }

    [Theory]
    [InlineData("valid_username")]
    [InlineData("valid-username")]
    [InlineData("validusername123")]
    [InlineData("VALIDUSERNAME")]
    public void Create_WithValidUsernameFormats_ShouldSucceed(string validUsername)
    {
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var result = UserAggregate.Create(name, email, validUsername, password, role);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe(validUsername);
    }

    [Theory, AutoFakeItEasyData]
    public void CreateFromPrimitives_WithValidData_ShouldSucceed(string name, string username)
    {
        var emailValue = "primitives@test.com";
        var passwordValue = "PrimitivesPassword123!";
        var roleValue = "User";
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "testuser" : username[..Math.Min(10, username.Length)];
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, roleValue);
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Name.ShouldBe(name);
        user.Email.Value.ShouldBe(emailValue);
        user.Username.ShouldBe(username);
        user.Role.Value.ShouldBe(roleValue);
        user.IsActive.ShouldBeTrue();
        user.UncommittedEvents.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@invalid.com")]
    [InlineData("invalid@")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateFromPrimitives_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        var name = "Valid Name";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";
        var roleValue = "User";
        var result = UserAggregate.CreateFromPrimitives(name, invalidEmail, username, passwordValue, roleValue);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Email."));
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("ONLYUPPERCASE")]
    [InlineData("onlylowercase")]
    [InlineData("12345678")]
    [InlineData("NoSpecialChar123")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateFromPrimitives_WithInvalidPassword_ShouldFail(string invalidPassword)
    {
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var roleValue = "User";
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, invalidPassword, roleValue);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Password."));
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateFromPrimitives_WithInvalidRole_ShouldFail(string invalidRole)
    {
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, invalidRole);
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    [InlineData("user")]
    [InlineData("ADMIN")]
    [InlineData("moderator")]
    public void CreateFromPrimitives_WithValidRoles_ShouldSucceed(string validRole)
    {
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, validRole);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.Value.ShouldBe(validRole[..1].ToUpper() + validRole[1..].ToLower());
    }

    [Fact]
    public void Builder_WithDefaultValues_ShouldCreateValidUser()
    {
        var result = new UserAggregateBuilder().Build();
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.IsActive.ShouldBeTrue();
        user.Role.Value.ShouldBe("User");
    }

    [Fact]
    public void Builder_WithCustomValues_ShouldCreateUserWithSpecifiedValues()
    {
        var customName = "Custom User";
        var customEmail = "custom@test.com";
        var customUsername = "customuser";
        var customRole = "Admin";
        var result = new UserAggregateBuilder()
            .WithName(customName)
            .WithEmail(customEmail)
            .WithUsername(customUsername)
            .WithRole(customRole)
            .Build();
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Id.ShouldNotBe(Guid.Empty);
        user.Name.ShouldBe(customName);
        user.Email.Value.ShouldBe(customEmail);
        user.Username.ShouldBe(customUsername);
        user.Role.Value.ShouldBe(customRole);
    }

    [Fact]
    public void Builder_BuildFromPrimitives_ShouldCreateValidUser()
    {
        var result = new UserAggregateBuilder()
            .WithName("Primitive User")
            .WithEmail("primitive@test.com")
            .WithUsername("primitiveuser")
            .BuildFromPrimitives();
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Name.ShouldBe("Primitive User");
        user.Email.Value.ShouldBe("primitive@test.com");
        user.Username.ShouldBe("primitiveuser");
    }
}