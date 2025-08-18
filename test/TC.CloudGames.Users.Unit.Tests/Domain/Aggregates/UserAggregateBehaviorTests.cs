namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateBehaviorTests
{
    [Theory, AutoFakeItEasyValidUserData]
    public void UpdateInfo_WithValidData_ShouldSucceed(string newName, string newUsername)
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmail = Email.Create("updated@test.com").Value;

        // Ensure username meets validation rules
        newUsername = string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3 ? "newuser" : newUsername[..Math.Min(10, newUsername.Length)];

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
        user.UncommittedEvents[user.UncommittedEvents.Count - 1].ShouldBeOfType<UserUpdatedEvent>();
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

    [Theory, AutoFakeItEasyValidUserData]
    public void UpdateInfoFromPrimitives_WithValidData_ShouldSucceed(string newName, string newUsername)
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newEmailValue = "primitiveupdate@test.com";

        // Ensure username meets validation rules
        newUsername = string.IsNullOrWhiteSpace(newUsername) || newUsername.Length < 3 ? "newuser" : newUsername[..Math.Min(10, newUsername.Length)];

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
        user.UncommittedEvents[user.UncommittedEvents.Count - 1].ShouldBeOfType<UserPasswordChangedEvent>();
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
        user.UncommittedEvents[user.UncommittedEvents.Count - 1].ShouldBeOfType<UserRoleChangedEvent>();
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
        user.UncommittedEvents[user.UncommittedEvents.Count - 1].ShouldBeOfType<UserActivatedEvent>();
    }

    [Fact]
    public void Activate_WhenUserIsAlreadyActive_ShouldFail()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.IsActive.ShouldBeTrue(); // User is active by default

        // Act
        var result = user.Activate();

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
        user.UncommittedEvents[user.UncommittedEvents.Count - 1].ShouldBeOfType<UserDeactivatedEvent>();
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
    }

    [Fact]
    public void ApplyEvent_WithUnknownEventType_ShouldNotThrow()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var unknownEvent = new { Type = "UnknownEvent", Data = "SomeData" };

        // Act & Assert
        Should.NotThrow(() =>
        {
            var method = user.GetType().GetMethod("ApplyEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(user, new[] { unknownEvent });
        });
    }

    [Fact]
    public void UpdateInfo_WithNullEmail_ShouldReturnInvalidResult()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newName = "Valid Name";
        var newUsername = "validuser";

        // Act
        var result = user.UpdateInfo(newName, null!, newUsername);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Email."));
    }

    [Fact]
    public void ChangePassword_WithNullPassword_ShouldReturnInvalidResult()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;

        // Act
        var result = user.ChangePassword(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier.StartsWith("Password."));
    }

    [Fact]
    public void ChangeRole_WithNullRole_ShouldReturnInvalidResult()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;

        // Act
        var result = user.ChangeRole(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }
}