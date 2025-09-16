using static TC.CloudGames.Users.Domain.Aggregates.UserAggregate;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateCreationTests
{
    [Theory, AutoFakeItEasyValidUserData]
    public void Create_WithValidData_ShouldSucceed(string name, string username)
    {
        // Arrange
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "testuser" : username[..Math.Min(10, username.Length)];

        // Act
        var result = UserAggregate.Create(name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.Username.ShouldBe(username);
        user.PasswordHash.ShouldBe(password);
        user.Role.ShouldBe(role);
        user.IsActive.ShouldBeTrue();
        user.CreatedAt.ShouldBeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.ShouldBeNull();
        user.UncommittedEvents.ShouldHaveSingleItem();
        user.UncommittedEvents[0].ShouldBeOfType<UserCreatedDomainEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldFail(string invalidName)
    {
        // Arrange
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(invalidName, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var name = new string('a', 201);
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooLong");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUsername_ShouldFail(string invalidUsername)
    {
        // Arrange
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, invalidUsername, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.Required");
    }

    [Fact]
    public void Create_WithUsernameTooShort_ShouldFail()
    {
        // Arrange
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var username = "ab";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.TooShort");
    }

    [Fact]
    public void Create_WithUsernameTooLong_ShouldFail()
    {
        // Arrange
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;
        var username = "a" + new string('x', 50);

        // Act
        var result = UserAggregate.Create(name, email, username, password, role);

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
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, invalidUsername, password, role);

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
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, validUsername, password, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe(validUsername);
    }

    [Theory, AutoFakeItEasyValidUserData]
    public void CreateFromPrimitives_WithValidData_ShouldSucceed(string name, string username)
    {
        // Arrange
        var emailValue = "primitives@test.com";
        var passwordValue = "PrimitivesPassword123!";
        var roleValue = "User";
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "testuser" : username[..Math.Min(10, username.Length)];

        // Act
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, roleValue);

        // Assert
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
    public void CreateFromPrimitives_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var name = "Valid Name";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";
        var roleValue = "User";

        // Act
        var result = UserAggregate.CreateFromPrimitives(name, invalidEmail, username, passwordValue, roleValue);

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
    public void CreateFromPrimitives_WithInvalidPassword_ShouldFail(string invalidPassword)
    {
        // Arrange
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var roleValue = "User";

        // Act
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, invalidPassword, roleValue);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Password."));
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public void CreateFromPrimitives_WithInvalidRole_ShouldFail(string invalidRole)
    {
        // Arrange
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";

        // Act
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, invalidRole);

        // Assert
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
        // Arrange
        var name = "Valid Name";
        var emailValue = "valid@test.com";
        var username = "validuser";
        var passwordValue = "ValidPassword123!";

        // Act
        var result = UserAggregate.CreateFromPrimitives(name, emailValue, username, passwordValue, validRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.Value.ShouldBe(validRole[..1].ToUpper() + validRole[1..].ToLower());
    }

    [Fact]
    public void Builder_WithDefaultValues_ShouldCreateValidUser()
    {
        // Arrange
        // Act
        var result = new UserAggregateBuilder().Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.IsActive.ShouldBeTrue();
        user.Role.Value.ShouldBe("User");
    }

    [Fact]
    public void Builder_WithCustomValues_ShouldCreateUserWithSpecifiedValues()
    {
        // Arrange
        var customName = "Custom User";
        var customEmail = "custom@test.com";
        var customUsername = "customuser";
        var customRole = "Admin";

        // Act
        var result = new UserAggregateBuilder()
            .WithName(customName)
            .WithEmail(customEmail)
            .WithUsername(customUsername)
            .WithRole(customRole)
            .Build();

        // Assert
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
        // Arrange
        // Act
        var result = new UserAggregateBuilder()
            .WithName("Primitive User")
            .WithEmail("primitive@test.com")
            .WithUsername("primitiveuser")
            .BuildFromPrimitives();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Name.ShouldBe("Primitive User");
        user.Email.Value.ShouldBe("primitive@test.com");
        user.Username.ShouldBe("primitiveuser");
    }

    [Fact]
    public void Create_WithNullEmail_ShouldReturnInvalidResult()
    {
        // Arrange
        var name = "Valid Name";
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, null!, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Email."));
    }

    [Fact]
    public void Create_WithNullPassword_ShouldReturnInvalidResult()
    {
        // Arrange
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, username, null!, role);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Password."));
    }

    [Fact]
    public void Create_WithNullRole_ShouldReturnInvalidResult()
    {
        // Arrange
        var name = "Valid Name";
        var email = Email.Create("valid@test.com").Value;
        var username = "validuser";
        var password = Password.Create("ValidPassword123!").Value;

        // Act
        var result = UserAggregate.Create(name, email, username, password, null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    [Fact]
    public void CreateFromPrimitives_WithNullValues_ShouldReturnInvalidResult()
    {
        // Act
        var result = UserAggregate.CreateFromPrimitives(null!, null!, null!, null!, null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldNotBeEmpty();
    }
}