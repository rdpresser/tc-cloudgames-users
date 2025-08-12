using TC.CloudGames.Users.Unit.Tests.Common;
using Xunit;

namespace TC.CloudGames.Users.Unit.Tests.Domain.Aggregates;

public class UserAggregateEventTests
{
    [Fact]
    public void Create_ShouldGenerateUserCreatedEvent()
    {
        // Arrange
        var name = "Test User";
        var email = Email.Create("test@example.com").Value;
        var username = "testuser";
        var password = Password.Create("TestPassword123!").Value;
        var role = Role.Create("User").Value;

        // Act
        var result = UserAggregate.Create(name, email, username, password, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.UncommittedEvents.ShouldHaveSingleItem();
        var createdEvent = user.UncommittedEvents.First().ShouldBeOfType<UserCreatedEvent>();
        createdEvent.Id.ShouldNotBe(Guid.Empty);
        createdEvent.Name.ShouldBe(name);
        createdEvent.Email.ShouldBe(email);
        createdEvent.Username.ShouldBe(username);
        createdEvent.Password.ShouldBe(password);
        createdEvent.Role.ShouldBe(role);
        createdEvent.CreatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateInfo_ShouldGenerateUserUpdatedEvent()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newName = "Updated Name";
        var newEmail = Email.Create("updated@example.com").Value;
        var newUsername = "updateduser";

        // Act
        var result = user.UpdateInfo(newName, newEmail, newUsername);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.UncommittedEvents.Count.ShouldBe(2);
        var updatedEvent = user.UncommittedEvents.Last().ShouldBeOfType<UserUpdatedEvent>();
        updatedEvent.Id.ShouldBe(user.Id);
        updatedEvent.Name.ShouldBe(newName);
        updatedEvent.Email.ShouldBe(newEmail);
        updatedEvent.Username.ShouldBe(newUsername);
        updatedEvent.UpdatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangePassword_ShouldGenerateUserPasswordChangedEvent()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var newPassword = Password.Create("NewTestPassword123!").Value;

        // Act
        var result = user.ChangePassword(newPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.UncommittedEvents.Count.ShouldBe(2);
        var passwordChangedEvent = user.UncommittedEvents.Last().ShouldBeOfType<UserPasswordChangedEvent>();
        passwordChangedEvent.Id.ShouldBe(user.Id);
        passwordChangedEvent.NewPassword.ShouldBe(newPassword);
        passwordChangedEvent.ChangedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangeRole_ShouldGenerateUserRoleChangedEvent()
    {
        // Arrange
        var user = new UserAggregateBuilder().WithRole("User").Build().Value;
        var newRole = Role.Create("Admin").Value;

        // Act
        var result = user.ChangeRole(newRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.UncommittedEvents.Count.ShouldBe(2);
        var roleChangedEvent = user.UncommittedEvents.Last().ShouldBeOfType<UserRoleChangedEvent>();
        roleChangedEvent.Id.ShouldBe(user.Id);
        roleChangedEvent.NewRole.ShouldBe(newRole);
        roleChangedEvent.ChangedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_ShouldGenerateUserActivatedEvent()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        // Act
        var result = user.Activate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.UncommittedEvents.Count.ShouldBe(3);
        var activatedEvent = user.UncommittedEvents.Last().ShouldBeOfType<UserActivatedEvent>();
        activatedEvent.Id.ShouldBe(user.Id);
        activatedEvent.ActivatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_ShouldGenerateUserDeactivatedEvent()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.IsActive.ShouldBeTrue();

        // Act
        var result = user.Deactivate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.UncommittedEvents.Count.ShouldBe(2);
        var deactivatedEvent = user.UncommittedEvents.Last().ShouldBeOfType<UserDeactivatedEvent>();
        deactivatedEvent.Id.ShouldBe(user.Id);
        deactivatedEvent.DeactivatedAt.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Apply_UserCreatedEvent_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Event Test User";
        var email = Email.Create("event@test.com").Value;
        var username = "eventuser";
        var password = Password.Create("EventPassword123!").Value;
        var role = Role.Create("Admin").Value;
        var createdAt = DateTime.UtcNow;
        var createdEvent = new UserCreatedEvent(id, name, email, username, password, role, createdAt);
        var user = UserAggregate.FromProjection(Guid.Empty, "", Email.Create("temp@temp.com").Value, "", Password.Create("TempPass123!").Value, Role.Create("User").Value, DateTime.MinValue, null, false);

        // Act
        user.Apply(createdEvent);

        // Assert
        user.Id.ShouldBe(id);
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.Username.ShouldBe(username);
        user.PasswordHash.ShouldBe(password);
        user.Role.ShouldBe(role);
        user.CreatedAt.ShouldBe(createdAt);
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Apply_UserUpdatedEvent_ShouldUpdateProperties()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var originalId = user.Id;
        var originalCreatedAt = user.CreatedAt;
        var originalIsActive = user.IsActive;
        var newName = "Updated Event Name";
        var newEmail = Email.Create("updated.event@test.com").Value;
        var newUsername = "updatedeventuser";
        var updatedAt = DateTime.UtcNow;
        var updatedEvent = new UserUpdatedEvent(user.Id, newName, newEmail, newUsername, updatedAt);

        // Act
        user.Apply(updatedEvent);

        // Assert
        user.Id.ShouldBe(originalId);
        user.Name.ShouldBe(newName);
        user.Email.ShouldBe(newEmail);
        user.Username.ShouldBe(newUsername);
        user.UpdatedAt.ShouldBe(updatedAt);
        user.CreatedAt.ShouldBe(originalCreatedAt);
        user.IsActive.ShouldBe(originalIsActive);
    }

    [Fact]
    public void Apply_UserPasswordChangedEvent_ShouldUpdatePassword()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        var originalId = user.Id;
        var originalName = user.Name;
        var originalEmail = user.Email;
        var newPassword = Password.Create("NewEventPassword123!").Value;
        var changedAt = DateTime.UtcNow;
        var passwordChangedEvent = new UserPasswordChangedEvent(user.Id, newPassword, changedAt);

        // Act
        user.Apply(passwordChangedEvent);

        // Assert
        user.Id.ShouldBe(originalId);
        user.Name.ShouldBe(originalName);
        user.Email.ShouldBe(originalEmail);
        user.PasswordHash.ShouldBe(newPassword);
        user.UpdatedAt.ShouldBe(changedAt);
    }

    [Fact]
    public void Apply_UserRoleChangedEvent_ShouldUpdateRole()
    {
        // Arrange
        var user = new UserAggregateBuilder().WithRole("User").Build().Value;
        var originalId = user.Id;
        var originalName = user.Name;
        var newRole = Role.Create("Admin").Value;
        var changedAt = DateTime.UtcNow;
        var roleChangedEvent = new UserRoleChangedEvent(user.Id, newRole, changedAt);

        // Act
        user.Apply(roleChangedEvent);

        // Assert
        user.Id.ShouldBe(originalId);
        user.Name.ShouldBe(originalName);
        user.Role.ShouldBe(newRole);
        user.UpdatedAt.ShouldBe(changedAt);
    }

    [Fact]
    public void Apply_UserActivatedEvent_ShouldActivateUser()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.Deactivate();
        user.IsActive.ShouldBeFalse();
        var activatedAt = DateTime.UtcNow;
        var activatedEvent = new UserActivatedEvent(user.Id, activatedAt);

        // Act
        user.Apply(activatedEvent);

        // Assert
        user.IsActive.ShouldBeTrue();
        user.UpdatedAt.ShouldBe(activatedAt);
    }

    [Fact]
    public void Apply_UserDeactivatedEvent_ShouldDeactivateUser()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.IsActive.ShouldBeTrue();
        var deactivatedAt = DateTime.UtcNow;
        var deactivatedEvent = new UserDeactivatedEvent(user.Id, deactivatedAt);

        // Act
        user.Apply(deactivatedEvent);

        // Assert
        user.IsActive.ShouldBeFalse();
        user.UpdatedAt.ShouldBe(deactivatedAt);
    }

    [Fact]
    public void MultipleOperations_ShouldGenerateCorrectEventSequence()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;

        // Act
        user.UpdateInfo("Updated Name", Email.Create("updated@test.com").Value, "updateduser");
        user.ChangePassword(Password.Create("NewPassword123!").Value);
        user.ChangeRole(Role.Create("Admin").Value);
        user.Deactivate();

        // Assert
        user.UncommittedEvents.Count.ShouldBe(5);
        user.UncommittedEvents[0].ShouldBeOfType<UserCreatedEvent>();
        user.UncommittedEvents[1].ShouldBeOfType<UserUpdatedEvent>();
        user.UncommittedEvents[2].ShouldBeOfType<UserPasswordChangedEvent>();
        user.UncommittedEvents[3].ShouldBeOfType<UserRoleChangedEvent>();
        user.UncommittedEvents[4].ShouldBeOfType<UserDeactivatedEvent>();
    }

    [Fact]
    public void MarkEventsAsCommitted_ShouldClearAllUncommittedEvents()
    {
        // Arrange
        var user = new UserAggregateBuilder().Build().Value;
        user.UpdateInfo("Updated", Email.Create("updated@test.com").Value, "updateduser");
        user.ChangePassword(Password.Create("NewPassword123!").Value);

        // Act
        user.MarkEventsAsCommitted();

        // Assert
        user.UncommittedEvents.ShouldBeEmpty();
        user.Deactivate();
        user.UncommittedEvents.ShouldHaveSingleItem();
        user.UncommittedEvents.First().ShouldBeOfType<UserDeactivatedEvent>();
    }
}