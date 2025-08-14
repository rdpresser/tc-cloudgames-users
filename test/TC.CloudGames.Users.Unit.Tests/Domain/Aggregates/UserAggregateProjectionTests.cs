using TC.CloudGames.Users.Domain.Aggregates;
using TC.CloudGames.Users.Domain.ValueObjects;
using TC.CloudGames.Users.Unit.Tests.Common;
using Xunit;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateProjectionTests
{
    [Theory, AutoFakeItEasyValidUserData]
    public void FromProjection_WithCompleteData_ShouldCreateUserWithAllProperties(Guid id, string name, string username)
    {
        // Arrange
        var email = Email.Create("projection@test.com").Value;
        var password = Password.Create("ProjectionPassword123!").Value;
        var role = Role.Create("Admin").Value;
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var isActive = true;

        // Ensure username meets validation rules
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "projuser" : username[..Math.Min(10, username.Length)];

        // Act
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(id);
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.Username.ShouldBe(username);
        user.PasswordHash.ShouldBe(password);
        user.Role.ShouldBe(role);
        user.CreatedAt.ShouldBe(createdAt);
        user.UpdatedAt.ShouldBe(updatedAt);
        user.IsActive.ShouldBe(isActive);
        user.UncommittedEvents.ShouldBeEmpty(); // No events should be generated for projection
    }

    [Theory, AutoFakeItEasyValidUserData]
    public void FromProjection_WithNullUpdatedAt_ShouldCreateUserWithNullUpdatedAt(Guid id, string name, string username)
    {
        // Arrange
        var email = Email.Create("projection2@test.com").Value;
        var password = Password.Create("ProjectionPassword123!").Value;
        var role = Role.Create("User").Value;
        var createdAt = DateTime.UtcNow.AddDays(-30);
        DateTime? updatedAt = null;
        var isActive = false;

        // Ensure username meets validation rules
        username = string.IsNullOrWhiteSpace(username) || username.Length < 3 ? "projuser" : username[..Math.Min(10, username.Length)];

        // Act
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(id);
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.Username.ShouldBe(username);
        user.PasswordHash.ShouldBe(password);
        user.Role.ShouldBe(role);
        user.CreatedAt.ShouldBe(createdAt);
        user.UpdatedAt.ShouldBeNull();
        user.IsActive.ShouldBe(isActive);
        user.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void FromProjection_WithInactiveUser_ShouldCreateInactiveUser()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Inactive User";
        var email = Email.Create("inactive@test.com").Value;
        var username = "inactiveuser";
        var password = Password.Create("InactivePassword123!").Value;
        var role = Role.Create("User").Value;
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var isActive = false;

        // Act
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.IsActive.ShouldBeFalse();
        user.UncommittedEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public void FromProjection_WithDifferentRoles_ShouldCreateUserWithCorrectRole(string roleValue)
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Role Test User";
        var email = Email.Create($"role{roleValue.ToLower()}@test.com").Value;
        var username = $"{roleValue.ToLower()}user";
        var password = Password.Create("RoleTestPassword123!").Value;
        var role = Role.Create(roleValue).Value;
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var isActive = true;

        // Act
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.Role.Value.ShouldBe(roleValue);
        user.Role.ShouldBe(role);
        user.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void FromProjection_DoesNotValidateData_ShouldCreateUserEvenWithPotentiallyInvalidData()
    {
        // Arrange - Using data that would fail normal validation
        var id = Guid.NewGuid();
        var name = ""; // Would normally fail validation
        var email = Email.Create("valid@test.com").Value; // Value Objects are still required to be valid
        var username = "ab"; // Would normally fail validation (too short)
        var password = Password.Create("ValidPassword123!").Value; // Value Objects are still required to be valid
        var role = Role.Create("User").Value; // Value Objects are still required to be valid
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var isActive = true;

        // Act - Should not throw or validate since this is for trusted data reconstruction
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.ShouldNotBeNull();
        user.Name.ShouldBe(name); // Even empty name should be accepted
        user.Username.ShouldBe(username); // Even short username should be accepted
        user.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void FromProjection_WithMinimalValidData_ShouldCreateUser()
    {
        // Arrange
        var id = Guid.Empty; // Even empty GUID should be accepted for projections
        var name = "A"; // Single character name
        var email = Email.Create("min@a.co").Value;
        var username = "a"; // Single character username
        var password = Password.Create("MinimalPass123!").Value;
        var role = Role.Create("User").Value;
        var createdAt = DateTime.MinValue;
        DateTime? updatedAt = null;
        var isActive = false;

        // Act
        var user = UserAggregate.FromProjection(id, name, email, username, password, role, createdAt, updatedAt, isActive);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(id);
        user.Name.ShouldBe(name);
        user.Username.ShouldBe(username);
        user.CreatedAt.ShouldBe(createdAt);
        user.UpdatedAt.ShouldBeNull();
        user.IsActive.ShouldBeFalse();
        user.UncommittedEvents.ShouldBeEmpty();
    }
}