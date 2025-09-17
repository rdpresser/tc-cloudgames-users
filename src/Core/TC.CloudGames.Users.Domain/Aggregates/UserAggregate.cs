namespace TC.CloudGames.Users.Domain.Aggregates;

public sealed class UserAggregate : BaseAggregateRoot
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string Username { get; private set; } = default!;
    public Password PasswordHash { get; private set; } = default!;
    public Role Role { get; private set; } = default!;

    // Construtor para Marten / ORM - deve ser público para event sourcing
    public UserAggregate() : base() { }

    // Construtor privado para factories
    private UserAggregate(Guid id) : base(id) { }

    #region Factories e Criação

    public static Result<UserAggregate> Create(string name, Email email, string username, Password password, Role role)
    {
        var errors = new List<ValidationError>();
        if (!ValueObjects.Email.TryValidate(email, out var emailErrors)) errors.AddRange(emailErrors);
        if (!Password.TryValidate(password, out var passwordErrors)) errors.AddRange(passwordErrors);
        if (!ValueObjects.Role.TryValidate(role, out var roleErrors)) errors.AddRange(roleErrors);
        errors.AddRange(ValidateNameAndUsername(name, username));

        if (errors.Count > 0) return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, email, username, password, role);
    }

    public static Result<UserAggregate> CreateFromResult(string name, Result<Email> email, string username, Result<Password> password, Result<Role> role)
    {
        var errors = new List<ValidationError>();
        errors.AddErrorsIfFailure(email);
        errors.AddErrorsIfFailure(password);
        errors.AddErrorsIfFailure(role);
        errors.AddRange(ValidateNameAndUsername(name, username));

        if (errors.Count > 0) return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, email.Value, username, password.Value, role.Value);
    }

    public static Result<UserAggregate> CreateFromPrimitives(string name, string emailValue, string username, string passwordValue, string roleValue)
    {
        var emailResult = ValueObjects.Email.Create(emailValue);
        var passwordResult = Password.Create(passwordValue);
        var roleResult = ValueObjects.Role.Create(roleValue);

        var errors = new List<ValidationError>();
        errors.AddErrorsIfFailure(emailResult);
        errors.AddErrorsIfFailure(passwordResult);
        errors.AddErrorsIfFailure(roleResult);
        errors.AddRange(ValidateNameAndUsername(name, username));

        if (errors.Count > 0) return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, emailResult.Value, username, passwordResult.Value, roleResult.Value);
    }

    private static Result<UserAggregate> CreateAggregate(string name, Email email, string username, Password password, Role role)
    {
        var aggregate = new UserAggregate(Guid.NewGuid());
        var @event = new UserCreatedDomainEvent(aggregate.Id, name, email.Value, username, password.Hash, role.Value, DateTimeOffset.UtcNow);
        aggregate.ApplyEvent(@event);
        return Result.Success(aggregate);
    }

    #endregion

    #region Update / Change Methods

    public Result UpdateInfoFromPrimitives(string name, string emailValue, string username)
    {
        var emailResult = ValueObjects.Email.Create(emailValue);
        if (!emailResult.IsSuccess) return Result.Invalid(emailResult.ValidationErrors.ToArray());
        return UpdateInfo(name, emailResult.Value, username);
    }

    public Result ChangePasswordFromPlainText(string newPlainPassword)
    {
        var passwordResult = Password.Create(newPlainPassword);
        if (!passwordResult.IsSuccess) return Result.Invalid(passwordResult.ValidationErrors.ToArray());
        return ChangePassword(passwordResult.Value);
    }

    public Result ChangeRoleFromString(string newRoleValue)
    {
        var roleResult = ValueObjects.Role.Create(newRoleValue);
        if (!roleResult.IsSuccess) return Result.Invalid(roleResult.ValidationErrors.ToArray());
        return ChangeRole(roleResult.Value);
    }

    public Result UpdateInfo(string name, Email email, string username)
    {
        if (email == null) return Result.Invalid(new ValidationError($"Email.Required", "Email is required."));
        var errors = ValidateNameAndUsername(name, username);
        if (errors.Any()) return Result.Invalid(errors.ToArray());

        var @event = new UserUpdatedDomainEvent(Id, name, email.Value, username, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    public Result ChangePassword(Password newPassword)
    {
        if (newPassword == null) return Result.Invalid(new ValidationError($"Password.Required", "Password is required."));
        var @event = new UserPasswordChangedDomainEvent(Id, newPassword.Hash, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    public Result ChangeRole(Role newRole)
    {
        if (newRole == null) return Result.Invalid(new ValidationError($"{nameof(Role)}.Invalid", "Invalid role value."));
        if (Role.Value == newRole.Value) return Result.Invalid(new ValidationError($"{nameof(Role)}.SameRole", "User already has this role."));
        var @event = new UserRoleChangedDomainEvent(Id, newRole.Value, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive) return Result.Invalid(new ValidationError("User.AlreadyActive", "User is already active."));
        var @event = new UserActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Invalid(new ValidationError("User.AlreadyInactive", "User is already deactivated."));
        var @event = new UserDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    #endregion

    #region Projection / ORM Factory

    public static UserAggregate FromProjection(Guid id, string name, string email, string username, string passwordHash, string role, DateTimeOffset createdAt, DateTimeOffset? updatedAt, bool isActive)
    {
        var user = new UserAggregate(id)
        {
            Name = name,
            Email = ValueObjects.Email.FromDb(email).Value,
            Username = username,
            PasswordHash = Password.FromHash(passwordHash).Value,
            Role = ValueObjects.Role.FromDb(role).Value
        };

        user.SetActive(isActive);
        user.SetCreatedAt(createdAt);
        user.SetUpdatedAt(updatedAt);
        return user;
    }

    public static UserAggregate FromProjection(Guid id, string name, string email, string username, string passwordHash, string role)
    {
        return new UserAggregate(id)
        {
            Name = name,
            Email = ValueObjects.Email.FromDb(email).Value,
            Username = username,
            PasswordHash = Password.FromHash(passwordHash).Value,
            Role = ValueObjects.Role.FromDb(role).Value
        };
    }

    #endregion

    #region Domain Events Apply

    public void Apply(UserCreatedDomainEvent @event)
    {
        SetId(@event.AggregateId);
        Name = @event.Name;
        Email = ValueObjects.Email.FromDb(@event.Email).Value;
        Username = @event.Username;
        PasswordHash = Password.FromHash(@event.Password).Value;
        Role = ValueObjects.Role.FromDb(@event.Role).Value;
        SetCreatedAt(@event.OccurredOn);
        SetActivate();
    }

    public void Apply(UserUpdatedDomainEvent @event)
    {
        Name = @event.Name;
        Email = ValueObjects.Email.FromDb(@event.Email).Value;
        Username = @event.Username;
        SetUpdatedAt(@event.OccurredOn);
    }

    public void Apply(UserPasswordChangedDomainEvent @event)
    {
        PasswordHash = Password.FromHash(@event.NewPassword).Value;
        SetUpdatedAt(@event.OccurredOn);
    }

    public void Apply(UserRoleChangedDomainEvent @event)
    {
        Role = ValueObjects.Role.FromDb(@event.NewRole).Value;
        SetUpdatedAt(@event.OccurredOn);
    }

    public void Apply(UserActivatedDomainEvent @event)
    {
        SetActivate();
        SetUpdatedAt(@event.OccurredOn);
    }

    public void Apply(UserDeactivatedDomainEvent @event)
    {
        SetDeactivate();
        SetUpdatedAt(@event.OccurredOn);
    }

    private void ApplyEvent(BaseDomainEvent @event)
    {
        AddNewEvent(@event);
        switch (@event)
        {
            case UserCreatedDomainEvent createdEvent: Apply(createdEvent); break;
            case UserUpdatedDomainEvent updatedEvent: Apply(updatedEvent); break;
            case UserPasswordChangedDomainEvent passwordChangedEvent: Apply(passwordChangedEvent); break;
            case UserRoleChangedDomainEvent roleChangedEvent: Apply(roleChangedEvent); break;
            case UserActivatedDomainEvent activatedEvent: Apply(activatedEvent); break;
            case UserDeactivatedDomainEvent deactivatedEvent: Apply(deactivatedEvent); break;
        }
    }

    #endregion

    #region Validation Helpers

    private static IEnumerable<ValidationError> ValidateName(string name)
    {
        var maxLength = 200;
        if (string.IsNullOrWhiteSpace(name))
            yield return new ValidationError($"{nameof(Name)}.Required", "Name is required.");
        else if (name.Length > maxLength)
            yield return new ValidationError($"{nameof(Name)}.TooLong", $"Name must be at most {maxLength} characters.");
    }

    private static IEnumerable<ValidationError> ValidateUsername(string username)
    {
        var maxLength = 50;
        var minLength = 3;
        var regex = new Regex("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        if (string.IsNullOrWhiteSpace(username))
            yield return new ValidationError($"{nameof(Username)}.Required", "Username is required.");
        else if (username.Length < minLength)
            yield return new ValidationError($"{nameof(Username)}.TooShort", $"Username must be at least {minLength} characters.");
        else if (username.Length > maxLength)
            yield return new ValidationError($"{nameof(Username)}.TooLong", $"Username must be at most {maxLength} characters.");
        else if (!regex.IsMatch(username))
            yield return new ValidationError($"{nameof(Username)}.InvalidFormat", "Username contains invalid characters.");
    }

    private static IEnumerable<ValidationError> ValidateNameAndUsername(string name, string username)
    {
        foreach (var error in ValidateName(name)) yield return error;
        foreach (var error in ValidateUsername(username)) yield return error;
    }

    #endregion

    #region Domain Events

    public record UserCreatedDomainEvent(Guid AggregateId, string Name, string Email, string Username, string Password, string Role, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserUpdatedDomainEvent(Guid AggregateId, string Name, string Email, string Username, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserPasswordChangedDomainEvent(Guid AggregateId, string NewPassword, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserRoleChangedDomainEvent(Guid AggregateId, string NewRole, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserActivatedDomainEvent(Guid AggregateId, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserDeactivatedDomainEvent(Guid AggregateId, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

    #endregion
}
