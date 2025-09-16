namespace TC.CloudGames.Users.Domain.Aggregates;

public sealed class UserAggregate : BaseAggregateRoot
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string Username { get; private set; } = default!;
    public Password PasswordHash { get; private set; } = default!;
    public Role Role { get; private set; } = default!;

    // Private constructor for aggregate reconstruction
    private UserAggregate(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Creates a new UserAggregate with proper validation using Value Objects and primitive values.
    /// This ensures the aggregate is always created in a valid state and avoids double validation.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="email">Pre-validated Email value object</param>
    /// <param name="username">User's unique username</param>
    /// <param name="password">Pre-validated Password value object</param>
    /// <param name="role">Pre-validated Role value object</param>
    /// <returns>Result containing the UserAggregate if valid, or validation errors if invalid</returns>
    public static Result<UserAggregate> Create(string name, Email email, string username, Password password, Role role)
    {
        var errors = new List<ValidationError>();

        if (!Email.TryValidate(email, out var emailErrors))
            errors.AddRange(emailErrors);
        if (!Password.TryValidate(password, out var passwordErrors))
            errors.AddRange(passwordErrors);
        if (!Role.TryValidate(role, out var roleErrors))
            errors.AddRange(roleErrors);

        errors.AddRange(ValidateNameAndUsername(name, username));
        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, email, username, password, role);
    }

    /// <summary>
    /// Creates a new UserAggregate from validated value object results.
    /// This method prevents invalid objects from being passed to the aggregate and avoids double validation.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="email">Result of Email value object creation</param>
    /// <param name="username">User's unique username</param>
    /// <param name="password">Result of Password value object creation</param>
    /// <param name="role">Result of Role value object creation</param>
    /// <returns>Result containing the UserAggregate if all validations pass</returns>
    public static Result<UserAggregate> CreateFromResult(string name, Result<Email> email, string username, Result<Password> password, Result<Role> role)
    {
        var errors = new List<ValidationError>();
        errors.AddErrorsIfFailure(email);
        errors.AddErrorsIfFailure(password);
        errors.AddErrorsIfFailure(role);
        errors.AddRange(ValidateNameAndUsername(name, username));

        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, email.Value, username, password.Value, role.Value);
    }

    /// <summary>
    /// Creates a new UserAggregate from primitive values with automatic validation.
    /// This method prevents invalid objects from being passed to the aggregate and avoids double validation.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="emailValue">Email string to validate</param>
    /// <param name="username">User's unique username</param>
    /// <param name="passwordValue">Plain password to validate and hash</param>
    /// <param name="roleValue">Role string to validate</param>
    /// <returns>Result containing the UserAggregate if all validations pass</returns>
    public static Result<UserAggregate> CreateFromPrimitives(
        string name,
        string emailValue,
        string username,
        string passwordValue,
        string roleValue)
    {
        var emailResult = Email.Create(emailValue);
        var passwordResult = Password.Create(passwordValue);
        var roleResult = Role.Create(roleValue);

        var errors = new List<ValidationError>();
        errors.AddErrorsIfFailure(emailResult);
        errors.AddErrorsIfFailure(passwordResult);
        errors.AddErrorsIfFailure(roleResult);
        errors.AddRange(ValidateNameAndUsername(name, username));

        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());

        return CreateAggregate(name, emailResult.Value, username, passwordResult.Value, roleResult.Value);
    }

    /// <summary>
    /// Creates and returns a new UserAggregate instance and applies the UserCreatedEvent.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="email">Validated Email value object</param>
    /// <param name="username">User's unique username</param>
    /// <param name="password">Validated Password value object</param>
    /// <param name="role">Validated Role value object</param>
    /// <returns>Result containing the UserAggregate</returns>
    private static Result<UserAggregate> CreateAggregate(string name, Email email, string username, Password password, Role role)
    {
        var aggregate = new UserAggregate(Guid.NewGuid());
        var @event = new UserCreatedDomainEvent(aggregate.Id, name, email, username, password, role, aggregate.CreatedAt);
        aggregate.ApplyEvent(@event);
        return Result.Success(aggregate);
    }

    /// <summary>
    /// Validates name and username and returns a list of validation errors.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="username">User's unique username</param>
    /// <returns>List of validation errors</returns>
    private static IEnumerable<ValidationError> ValidateNameAndUsername(string name, string username)
    {
        foreach (var error in ValidateName(name))
            yield return error;
        foreach (var error in ValidateUsername(username))
            yield return error;
    }

    /// <summary>
    /// SAFE UPDATE METHOD: Updates user info from primitive values with automatic validation
    /// </summary>
    /// <param name="name">New display name</param>
    /// <param name="emailValue">Email string to validate</param>
    /// <param name="username">New username</param>
    /// <returns>Result indicating success or validation errors</returns>
    public Result UpdateInfoFromPrimitives(string name, string emailValue, string username)
    {
        var emailResult = Email.Create(emailValue);
        if (!emailResult.IsSuccess)
            return Result.Invalid(emailResult.ValidationErrors.ToArray());
        return UpdateInfo(name, emailResult.Value, username);
    }

    /// <summary>
    /// SAFE PASSWORD CHANGE: Changes password from plain text with automatic validation
    /// </summary>
    /// <param name="newPlainPassword">New plain text password</param>
    /// <returns>Result indicating success or validation errors</returns>
    public Result ChangePasswordFromPlainText(string newPlainPassword)
    {
        var passwordResult = Password.Create(newPlainPassword);
        if (!passwordResult.IsSuccess)
            return Result.Invalid(passwordResult.ValidationErrors.ToArray());
        return ChangePassword(passwordResult.Value);
    }

    /// <summary>
    /// SAFE ROLE CHANGE: Changes role from string with automatic validation
    /// </summary>
    /// <param name="newRoleValue">New role string</param>
    /// <returns>Result indicating success or validation errors</returns>
    public Result ChangeRoleFromString(string newRoleValue)
    {
        var roleResult = Role.Create(newRoleValue);
        if (!roleResult.IsSuccess)
            return Result.Invalid(roleResult.ValidationErrors.ToArray());
        return ChangeRole(roleResult.Value);
    }

    /// <summary>
    /// Factory method to reconstruct aggregate from projection data (for read operations)
    /// No validation needed as data comes from a trusted source (database)
    /// </summary>
    public static UserAggregate FromProjection(Guid id, string name, string email, string username,
        string passwordHash, string role, DateTimeOffset createdAt, DateTimeOffset? updatedAt, bool isActive)
    {
        var user = new UserAggregate(id)
        {
            Name = name,
            Email = Email.FromDb(email),
            Username = username,
            PasswordHash = Password.FromHash(passwordHash),
            Role = Role.FromDb(role)
        };

        user.SetActive(isActive);
        user.SetCreatedAt(createdAt);
        user.SetUpdatedAt(updatedAt);
        return user;
    }

    /// <summary>
    /// Factory method to reconstruct aggregate from projection data (for read operations)
    /// No validation needed as data comes from a trusted source (database)
    /// </summary>
    public static UserAggregate FromProjection(Guid id, string name, string email, string username,
        string passwordHash, string role)
    {
        return new UserAggregate(id)
        {
            Name = name,
            Email = Email.FromDb(email),
            Username = username,
            PasswordHash = Password.FromHash(passwordHash),
            Role = Role.FromDb(role)
        };
    }

    /// <summary>
    /// Updates user information with validated Value Objects
    /// </summary>
    /// <param name="name">New display name</param>
    /// <param name="email">Pre-validated Email value object</param>
    /// <param name="username">New username</param>
    /// <returns>Result indicating success or validation errors</returns>
    public Result UpdateInfo(string name, Email email, string username)
    {
        if (email == null)
            return Result.Invalid(new ValidationError($"Email.Required", "Email is required."));
        var errors = ValidateNameAndUsername(name, username);
        if (errors.Any())
            return Result.Invalid(errors.ToArray());

        var @event = new UserUpdatedDomainEvent(Id, name, email, username, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    /// <summary>
    /// Changes user password with pre-validated Password value object
    /// </summary>
    /// <param name="newPassword">Pre-validated Password value object</param>
    /// <returns>Result indicating success or business rule violations</returns>
    public Result ChangePassword(Password newPassword)
    {
        if (newPassword == null)
            return Result.Invalid(new ValidationError($"Password.Required", "Password is required."));
        var @event = new UserPasswordChangedDomainEvent(Id, newPassword, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    /// <summary>
    /// Changes user role with pre-validated Role value object
    /// </summary>
    /// <param name="newRole">Pre-validated Role value object</param>
    /// <returns>Result indicating success or business rule violations</returns>
    public Result ChangeRole(Role newRole)
    {
        if (newRole == null)
            return Result.Invalid(new ValidationError($"{nameof(Role)}.Invalid", "Invalid role value."));
        if (Role.Value == newRole.Value)
            return Result.Invalid(new ValidationError($"{nameof(Role)}.SameRole", "User already has this role."));
        var @event = new UserRoleChangedDomainEvent(Id, newRole, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    /// <summary>
    /// Activates a deactivated user
    /// </summary>
    /// <returns>Result indicating success or business rule violations</returns>
    public Result Activate()
    {
        if (IsActive)
            return Result.Invalid(new ValidationError("User.AlreadyActive", "User is already active."));
        var @event = new UserActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    /// <summary>
    /// Deactivates an active user
    /// </summary>
    /// <returns>Result indicating success or business rule violations</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Invalid(new ValidationError("User.AlreadyInactive", "User is already deactivated."));
        var @event = new UserDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    // Event application methods
    internal void Apply(UserCreatedDomainEvent @event)
    {
        SetId(@event.AggregateId);
        Name = @event.Name;
        Email = @event.Email;
        Username = @event.Username;
        PasswordHash = @event.Password;
        Role = @event.Role;
        SetCreatedAt(@event.OccurredOn);
        SetActivate();
    }

    internal void Apply(UserUpdatedDomainEvent @event)
    {
        Name = @event.Name;
        Email = @event.Email;
        Username = @event.Username;
        SetUpdatedAt(@event.OccurredOn);
    }

    internal void Apply(UserPasswordChangedDomainEvent @event)
    {
        PasswordHash = @event.NewPassword;
        SetUpdatedAt(@event.OccurredOn);
    }

    internal void Apply(UserRoleChangedDomainEvent @event)
    {
        Role = @event.NewRole;
        SetUpdatedAt(@event.OccurredOn);
    }

    internal void Apply(UserActivatedDomainEvent @event)
    {
        SetActivate();
        SetUpdatedAt(@event.OccurredOn);
    }

    internal void Apply(UserDeactivatedDomainEvent @event)
    {
        SetDeactivate();
        SetUpdatedAt(@event.OccurredOn);
    }

    /// <summary>
    /// Applies an event to the aggregate and adds it to uncommitted events
    /// </summary>
    /// <param name="event">Domain event to apply</param>
    private void ApplyEvent(BaseDomainEvent @event)
    {
        AddNewEvent(@event);
        switch (@event)
        {
            case UserCreatedDomainEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserUpdatedDomainEvent updatedEvent:
                Apply(updatedEvent);
                break;
            case UserPasswordChangedDomainEvent passwordChangedEvent:
                Apply(passwordChangedEvent);
                break;
            case UserRoleChangedDomainEvent roleChangedEvent:
                Apply(roleChangedEvent);
                break;
            case UserActivatedDomainEvent activatedEvent:
                Apply(activatedEvent);
                break;
            case UserDeactivatedDomainEvent deactivatedEvent:
                Apply(deactivatedEvent);
                break;
        }
    }

    /// <summary>
    /// Validates the user's display name and adds any errors to the provided list.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="errors">List to add validation errors to</param>
    private static IEnumerable<ValidationError> ValidateName(string name)
    {
        var maxLength = 200;
        if (string.IsNullOrWhiteSpace(name))
            yield return new ValidationError($"{nameof(Name)}.Required", "Name is required.");
        else if (name.Length > maxLength)
            yield return new ValidationError($"{nameof(Name)}.TooLong", $"Name must be at most {maxLength} characters.");
    }

    /// <summary>
    /// Validates the user's username and adds any errors to the provided list.
    /// </summary>
    /// <param name="username">User's unique username</param>
    /// <param name="errors">List to add validation errors to</param>
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

    // Domain Events (internal, rich in domain details, using Value Objects for type safety)
    public record UserCreatedDomainEvent(Guid AggregateId, string Name, Email Email, string Username, Password Password, Role Role, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserUpdatedDomainEvent(Guid AggregateId, string Name, Email Email, string Username, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserPasswordChangedDomainEvent(Guid AggregateId, Password NewPassword, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserRoleChangedDomainEvent(Guid AggregateId, Role NewRole, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserActivatedDomainEvent(Guid AggregateId, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    public record UserDeactivatedDomainEvent(Guid AggregateId, DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
}
