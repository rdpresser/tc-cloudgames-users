namespace TC.CloudGames.Users.Unit.Tests.Domain
{
    public class UserAggregateEventSourcingTests
    {
        [Fact]
        public void Should_Create_User_And_Apply_Events_Correctly()
        {
            // Arrange
            var name = "John Doe";
            var email = Email.Create("john@example.com").Value;
            var username = "johndoe";
            var password = Password.Create("Password123!").Value;
            var role = Role.Create("User").Value;

            // Act
            var result = UserAggregate.Create(name, email, username, password, role);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            var user = result.Value;

            // Verify aggregate state
            user.Id.ShouldNotBe(Guid.Empty);
            user.Name.ShouldBe(name);
            user.Email.Value.ShouldBe(email.Value);
            user.Username.ShouldBe(username);
            user.PasswordHash.Hash.ShouldBe(password.Hash);
            user.Role.Value.ShouldBe(role.Value);
            user.IsActive.ShouldBeTrue();
            user.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);

            // Verify events were generated
            user.UncommittedEvents.ShouldNotBeEmpty();
            user.UncommittedEvents.Count.ShouldBe(1);

            var createdEvent = user.UncommittedEvents[0];
            createdEvent.ShouldBeOfType<UserAggregate.UserCreatedDomainEvent>();

            var typedEvent = (UserAggregate.UserCreatedDomainEvent)createdEvent;
            typedEvent.AggregateId.ShouldBe(user.Id);
            typedEvent.Name.ShouldBe(name);
            typedEvent.Email.ShouldBe(email.Value);
            typedEvent.Username.ShouldBe(username);
            typedEvent.Password.ShouldBe(password.Hash);
            typedEvent.Role.ShouldBe(role.Value);
        }

        [Fact]
        public void Should_Update_User_And_Generate_Update_Event()
        {
            // Arrange
            var user = CreateTestUser();
            var newName = "Jane Doe";
            var newEmail = Email.Create("jane@example.com").Value;
            var newUsername = "janedoe";

            // Clear initial events
            user.MarkEventsAsCommitted();

            // Act
            var result = user.UpdateInfo(newName, newEmail, newUsername);

            // Assert
            result.IsSuccess.ShouldBeTrue();

            // Verify state changes
            user.Name.ShouldBe(newName);
            user.Email.Value.ShouldBe(newEmail.Value);
            user.Username.ShouldBe(newUsername);
            user.UpdatedAt.ShouldNotBeNull();

            // Verify events
            user.UncommittedEvents.Count.ShouldBe(1);
            var updateEvent = user.UncommittedEvents[0];
            updateEvent.ShouldBeOfType<UserAggregate.UserUpdatedDomainEvent>();

            var typedEvent = (UserAggregate.UserUpdatedDomainEvent)updateEvent;
            typedEvent.AggregateId.ShouldBe(user.Id);
            typedEvent.Name.ShouldBe(newName);
            typedEvent.Email.ShouldBe(newEmail.Value);
            typedEvent.Username.ShouldBe(newUsername);
        }

        [Fact]
        public void Should_Change_Password_And_Generate_Password_Changed_Event()
        {
            // Arrange
            var user = CreateTestUser();
            var newPassword = Password.Create("NewPassword123!").Value;
            user.MarkEventsAsCommitted();

            // Act
            var result = user.ChangePassword(newPassword);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            user.PasswordHash.Hash.ShouldBe(newPassword.Hash);
            user.UpdatedAt.ShouldNotBeNull();

            user.UncommittedEvents.Count.ShouldBe(1);
            var passwordEvent = user.UncommittedEvents[0];
            passwordEvent.ShouldBeOfType<UserAggregate.UserPasswordChangedDomainEvent>();

            var typedEvent = (UserAggregate.UserPasswordChangedDomainEvent)passwordEvent;
            typedEvent.AggregateId.ShouldBe(user.Id);
            typedEvent.NewPassword.ShouldBe(newPassword.Hash);
        }

        [Fact]
        public void Should_Change_Role_And_Generate_Role_Changed_Event()
        {
            // Arrange
            var user = CreateTestUser();
            var newRole = Role.Create("Admin").Value;
            user.MarkEventsAsCommitted();

            // Act
            var result = user.ChangeRole(newRole);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            user.Role.Value.ShouldBe(newRole.Value);
            user.UpdatedAt.ShouldNotBeNull();

            user.UncommittedEvents.Count.ShouldBe(1);
            var roleEvent = user.UncommittedEvents[0];
            roleEvent.ShouldBeOfType<UserAggregate.UserRoleChangedDomainEvent>();

            var typedEvent = (UserAggregate.UserRoleChangedDomainEvent)roleEvent;
            typedEvent.AggregateId.ShouldBe(user.Id);
            typedEvent.NewRole.ShouldBe(newRole.Value);
        }

        [Fact]
        public void Should_Activate_Deactivated_User()
        {
            // Arrange
            var user = CreateTestUser();
            user.Deactivate(); // Deactivate first
            user.MarkEventsAsCommitted();

            // Act
            var result = user.Activate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            user.IsActive.ShouldBeTrue();
            user.UpdatedAt.ShouldNotBeNull();

            user.UncommittedEvents.Count.ShouldBe(1);
            var activateEvent = user.UncommittedEvents[0];
            activateEvent.ShouldBeOfType<UserAggregate.UserActivatedDomainEvent>();
        }

        [Fact]
        public void Should_Deactivate_Active_User()
        {
            // Arrange
            var user = CreateTestUser();
            user.MarkEventsAsCommitted();

            // Act
            var result = user.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            user.IsActive.ShouldBeFalse();
            user.UpdatedAt.ShouldNotBeNull();

            user.UncommittedEvents.Count.ShouldBe(1);
            var deactivateEvent = user.UncommittedEvents[0];
            deactivateEvent.ShouldBeOfType<UserAggregate.UserDeactivatedDomainEvent>();
        }

        private static UserAggregate CreateTestUser()
        {
            var name = "Test User";
            var email = Email.Create("test@example.com").Value;
            var username = "testuser";
            var password = Password.Create("TestPassword123!").Value;
            var role = Role.Create("User").Value;

            return UserAggregate.Create(name, email, username, password, role).Value;
        }
    }
}