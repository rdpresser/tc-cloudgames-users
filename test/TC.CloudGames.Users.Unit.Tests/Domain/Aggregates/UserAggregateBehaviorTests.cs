using TC.CloudGames.Users.Unit.Tests.Common;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

/// <summary>
/// Unit tests for UserAggregate behavior methods (Update, Change, Activate/Deactivate)
/// </summary>
public class UserAggregateBehaviorTests
{
    #region UpdateInfo Tests

    [Theory, AutoFakeItEasyData]
    public void UpdateInfo_WithValidData_ShouldSucceed(string newName, string newUsername)
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmail = Email.Create("updated@test.com").Value;

        // Ensure username meets validation rules
        newUsername = newUsername.Substring(0, Math.Min(10, newUsername.Length));
        if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3)
            newUsername = "newuser";

        var originalCreatedAt = user.CreatedAt;
        var originalId = user.Id;

        // Act
        var result = user.UpdateInfo(newName, newEmail, newUsername);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.Name.ShouldBe(newName);
        user.Email.ShouldBe(newEmail);
        user.Username.ShouldBe(newUsername);
        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.CreatedAt.ShouldBe(originalCreatedAt); // Should not change
        user.Id.ShouldBe(originalId); // Should not change
        user.UncommittedEvents.Count.ShouldBe(2); // Create + Update events
        user.UncommittedEvents.Last().ShouldBeOfType<UserUpdatedEvent>();
    }

    [Fact]
    public void UpdateInfo_WithInvalidName_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmail = Email.Create("updated@test.com").Value;
        var newUsername = "validuser";
        var invalidName = ""; // Empty name

        // Act
        var result = user.UpdateInfo(invalidName, newEmail, newUsername);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no Update event
    }

    [Fact]
    public void UpdateInfo_WithInvalidUsername_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmail = Email.Create("updated@test.com").Value;
        var validName = "Valid Name";
        var invalidUsername = "ab"; // Too short

        // Act
        var result = user.UpdateInfo(validName, newEmail, invalidUsername);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Username.TooShort");
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no Update event
    }

    #endregion

    #region UpdateInfoFromPrimitives Tests

    [Theory, AutoFakeItEasyData]
    public void UpdateInfoFromPrimitives_WithValidData_ShouldSucceed(string newName, string newUsername)
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmailValue = "primitiveupdate@test.com";

        // Ensure username meets validation rules
        newUsername = newUsername.Substring(0, Math.Min(10, newUsername.Length));
        if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3)
            newUsername = "newuser";

        // Act
        var result = user.UpdateInfoFromPrimitives(newName, newEmailValue, newUsername);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.Name.ShouldBe(newName);
        user.Email.Value.ShouldBe(newEmailValue);
        user.Username.ShouldBe(newUsername);
        user.UpdatedAt.ShouldNotBeNull();
        user.UncommittedEvents.Count.ShouldBe(2); // Create + Update events
    }

    [Fact]
    public void UpdateInfoFromPrimitives_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var validName = "Valid Name";
        var invalidEmail = "invalid-email";
        var validUsername = "validuser";

        // Act
        var result = user.UpdateInfoFromPrimitives(validName, invalidEmail, validUsername);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Email."));
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no Update event
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public void ChangePassword_WithValidPassword_ShouldSucceed()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newPassword = Password.Create("NewValidPassword123!").Value;
        var originalPasswordHash = user.PasswordHash;

        // Act
        var result = user.ChangePassword(newPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.PasswordHash.ShouldBe(newPassword);
        user.PasswordHash.ShouldNotBe(originalPasswordHash);
        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UncommittedEvents.Count.ShouldBe(2); // Create + PasswordChanged events
        user.UncommittedEvents.Last().ShouldBeOfType<UserPasswordChangedEvent>();
    }

    [Fact]
    public void ChangePasswordFromPlainText_WithValidPassword_ShouldSucceed()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newPlainPassword = "NewPlainPassword123!";
        var originalPasswordHash = user.PasswordHash;

        // Act
        var result = user.ChangePasswordFromPlainText(newPlainPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.PasswordHash.ShouldNotBe(originalPasswordHash);
        user.PasswordHash.Verify(newPlainPassword).ShouldBeTrue();
        user.UpdatedAt.ShouldNotBeNull();
        user.UncommittedEvents.Count.ShouldBe(2); // Create + PasswordChanged events
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("NoSpecialChar123")]
    [InlineData("")]
    [InlineData(null)]
    public void ChangePasswordFromPlainText_WithInvalidPassword_ShouldFail(string invalidPassword)
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var originalPasswordHash = user.PasswordHash;

        // Act
        var result = user.ChangePasswordFromPlainText(invalidPassword);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Password."));
        user.PasswordHash.ShouldBe(originalPasswordHash); // Should not change
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no PasswordChanged event
    }

    #endregion

    #region ChangeRole Tests

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public void ChangeRole_WithDifferentValidRole_ShouldSucceed(string newRoleValue)
    {
        // Arrange
        var user = new UserAggregateBuilder()
            .WithRole("User") // Start with User role
            .Build().Value;
        var newRole = Role.Create(newRoleValue).Value;
        var originalRole = user.Role;

        // Act
        var result = user.ChangeRole(newRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.Role.ShouldBe(newRole);
        user.Role.ShouldNotBe(originalRole);
        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UncommittedEvents.Count.ShouldBe(2); // Create + RoleChanged events
        user.UncommittedEvents.Last().ShouldBeOfType<UserRoleChangedEvent>();
    }

    [Fact]
    public void ChangeRole_WithSameRole_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder()
            .WithRole("User")
            .Build().Value;
        var sameRole = Role.Create("User").Value;

        // Act
        var result = user.ChangeRole(sameRole);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.SameRole");
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no RoleChanged event
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public void ChangeRoleFromString_WithValidRole_ShouldSucceed(string newRoleValue)
    {
        // Arrange
        var user = new UserAggregateBuilder()
            .WithRole("User")
            .Build().Value;

        // Act
        var result = user.ChangeRoleFromString(newRoleValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.Role.Value.ShouldBe(newRoleValue);
        user.UpdatedAt.ShouldNotBeNull();
        user.UncommittedEvents.Count.ShouldBe(2); // Create + RoleChanged events
    }

    [Fact]
    public void ChangeRoleFromString_WithInvalidRole_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var invalidRole = "InvalidRole";

        // Act
        var result = user.ChangeRoleFromString(invalidRole);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no RoleChanged event
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Activate_WhenUserIsInactive_ShouldSucceed()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        // First deactivate the user
        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        // Act
        var result = user.Activate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.IsActive.ShouldBeTrue();
        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UncommittedEvents.Count.ShouldBe(3); // Create + Deactivated + Activated events
        user.UncommittedEvents.Last().ShouldBeOfType<UserActivatedEvent>();
    }

    [Fact]
    public void Activate_WhenUserIsAlreadyActive_ShouldFail()
    {
        // Arrange
        var userResult = new UserAggregateBuilder().Build();
        Assert.NotNull(userResult); // xUnit assertion for null
        Console.WriteLine($"userResult.IsSuccess: {userResult.IsSuccess}");
        if (userResult.ValidationErrors != null)
        {
            foreach (var error in userResult.ValidationErrors)
            {
                Console.WriteLine($"ValidationError: {error.Identifier} - {error.ErrorMessage}");
            }
        }
        Assert.True(userResult.IsSuccess, "UserAggregateBuilder.Build() failed");
        Assert.NotNull(userResult.Value);
        var user = userResult.Value;
        Console.WriteLine($"user.IsActive: {user.IsActive}");
        user.IsActive.ShouldBeTrue(); // User is active by default

        // Act
        var result = user.Activate();
        Assert.NotNull(result);
        Console.WriteLine($"result.IsSuccess: {result.IsSuccess}");
        if (result.ValidationErrors != null)
        {
            foreach (var error in result.ValidationErrors)
            {
                Console.WriteLine($"Activate ValidationError: {error.Identifier} - {error.ErrorMessage}");
            }
        }

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "User.AlreadyActive");
        user.UncommittedEvents.Count.ShouldBe(1); // Only Create event, no Activated event
    }

    [Fact]
    public void Deactivate_WhenUserIsActive_ShouldSucceed()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.IsActive.ShouldBeTrue(); // User is active by default

        // Act
        var result = user.Deactivate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.IsActive.ShouldBeFalse();
        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UncommittedEvents.Count.ShouldBe(2); // Create + Deactivated events
        user.UncommittedEvents.Last().ShouldBeOfType<UserDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenUserIsAlreadyInactive_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        // First deactivate the user
        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        // Act
        var result = user.Deactivate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "User.AlreadyInactive");
        user.UncommittedEvents.Count.ShouldBe(2); // Create + Deactivated events, no second Deactivated event
    }

    #endregion

    #region Event Management Tests

    [Fact]
    public void MarkEventsAsCommitted_ShouldClearUncommittedEvents()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.UpdateInfo("Updated Name", Email.Create("updated@test.com").Value, "updateduser");
        user.UncommittedEvents.Count.ShouldBe(2); // Create + Update events

        // Act
        user.MarkEventsAsCommitted();

        // Assert
        user.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UncommittedEvents_ShouldBeReadOnly()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;

        // Act & Assert
        (user.UncommittedEvents is List<object>).ShouldBeFalse();
        user.UncommittedEvents.ShouldBeAssignableTo<IReadOnlyList<object>>();
        user.UncommittedEvents.ShouldBeAssignableTo<IReadOnlyList<object>>();
    }

    #endregion
}