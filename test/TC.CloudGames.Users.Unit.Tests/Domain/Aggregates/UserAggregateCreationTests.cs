using TC.CloudGames.Users.Domain.Aggregates;
using TC.CloudGames.Users.Domain.ValueObjects;
using TC.CloudGames.Users.Unit.Tests.Common;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

/// <summary>
/// Comprehensive unit tests for UserAggregate creation methods
/// Tests both regular Create method and CreateFromPrimitives factory method
/// </summary>
public class UserAggregateCreationTests
{
    #region Create Method Tests (With Value Objects)

    [Theory, AutoFakeItEasyData]
    public void Create_WithValidData_ShouldSucceed(
        Guid id,
        string name,
        string username)
    {
        // Arrange
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        
        // Ensure username meets validation rules
        username = username.Substring(0, Math.Min(10, username.Length));
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            username = "testuser";

        // Act
        var result = UserAggregate.Create(id, name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(id);
        result.Value.Name.ShouldBe(name);
        result.Value.Email.ShouldBe(email);
        result.Value.Username.ShouldBe(username);
        result.Value.PasswordHash.ShouldBe(password);
        result.Value.Role.ShouldBe(role);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.CreatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Value.UpdatedAt.ShouldBeNull();
        result.Value.UncommittedEvents.ShouldHaveSingleItem();
        result.Value.UncommittedEvents.First().ShouldBeOfType<UserCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldFail(string invalidName)
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, invalidName, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = new string('a', 101); // Exceeds 100 character limit
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidUsername_ShouldFail(string invalidUsername)
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, name, email, invalidUsername, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.Required");
    }

    [Fact]
    public void Create_WithUsernameTooShort_ShouldFail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var username = "ab"; // Less than 3 characters
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.TooShort");
    }

    [Fact]
    public void Create_WithUsernameTooLong_ShouldFail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var username = "a" + new string('x', 50); // username length > 50
        // Debug: Print username and length
        System.Diagnostics.Debug.WriteLine($"Username: '{username}', Length: {username.Length}");

        // Act
        var result = UserAggregate.Create(id, name, email, username, password, role);

        // Assert
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
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, name, email, invalidUsername, password, role);

        // Assert
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
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(id, name, email, validUsername, password, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe(validUsername);
    }

    #endregion

    #region CreateFromPrimitives Method Tests

    [Theory, AutoFakeItEasyData]
    public void CreateFromPrimitives_WithValidData_ShouldSucceed(
        Guid id,
        string name,
        string username)
    {
        // Arrange
        var emailValue = "primitives@test.com";
        var passwordValue = "PrimitivesPassword123!";
        var roleValue = "User";
        
        // Ensure username meets validation rules
        username = username.Substring(0, Math.Min(10, username.Length));
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            username = "testuser";

        // Act
        var result = UserAggregate.CreateFromPrimitives(id, name, emailValue, username, passwordValue, roleValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(id);
        result.Value.Name.ShouldBe(name);
        result.Value.Email.Value.ShouldBe(emailValue);
        result.Value.Username.ShouldBe(username);
        result.Value.Role.Value.ShouldBe(roleValue);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.UncommittedEvents.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@invalid.com")]
    [InlineData("invalid@")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateFromPrimitives_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";
        var roleValue = "User";

        // Act
        var result = UserAggregate.CreateFromPrimitives(id, name, invalidEmail, username, passwordValue, roleValue);

        // Assert
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
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var roleValue = "User";

        // Act
        var result = UserAggregate.CreateFromPrimitives(id, name, emailValue, username, invalidPassword, roleValue);

        // Assert
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
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";

        // Act
        var result = UserAggregate.CreateFromPrimitives(id, name, emailValue, username, passwordValue, invalidRole);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    [InlineData("user")] // Case insensitive
    [InlineData("ADMIN")]
    [InlineData("moderator")]
    public void CreateFromPrimitives_WithValidRoles_ShouldSucceed(string validRole)
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";

        // Act
        var result = UserAggregate.CreateFromPrimitives(id, name, emailValue, username, passwordValue, validRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.Value.ShouldBe(validRole.Substring(0, 1).ToUpper() + validRole.Substring(1).ToLower());
    }

    #endregion

    #region Builder Pattern Tests

    [Fact]
    public void Builder_WithDefaultValues_ShouldCreateValidUser()
    {
        // Act
        var result = new UserAggregateBuilder().Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsActive.ShouldBeTrue();
        result.Value.Role.Value.ShouldBe("User");
    }

    [Fact]
    public void Builder_WithCustomValues_ShouldCreateUserWithSpecifiedValues()
    {
        // Arrange
        var customId = Guid.NewGuid();
        var customName = "Custom User";
        var customEmail = "custom@test.com";
        var customUsername = "customuser";
        var customRole = "Admin";

        // Act
        var result = new UserAggregateBuilder()
            .WithId(customId)
            .WithName(customName)
            .WithEmail(customEmail)
            .WithUsername(customUsername)
            .WithRole(customRole)
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(customId);
        result.Value.Name.ShouldBe(customName);
        result.Value.Email.Value.ShouldBe(customEmail);
        result.Value.Username.ShouldBe(customUsername);
        result.Value.Role.Value.ShouldBe(customRole);
    }

    [Fact]
    public void Builder_BuildFromPrimitives_ShouldCreateValidUser()
    {
        // Act
        var result = new UserAggregateBuilder()
            .WithName("Primitive User")
            .WithEmail("primitive@test.com")
            .WithUsername("primitiveuser")
            .BuildFromPrimitives();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Primitive User");
        result.Value.Email.Value.ShouldBe("primitive@test.com");
        result.Value.Username.ShouldBe("primitiveuser");
    }

    #endregion
}