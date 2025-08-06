namespace TC.CloudGames.Users.Domain.Aggregates;

public sealed class UserAggregate
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public string Username { get; private set; } = string.Empty;
    public Password PasswordHash { get; private set; } = null!;
    public Role Role { get; private set; } = Role.User;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    // Private constructor for aggregate reconstruction
    private UserAggregate() { }

    /// <summary>
    /// Creates a new UserAggregate with proper validation using Value Objects.
    /// This ensures the aggregate is always created in a valid state.
    /// </summary>
    /// <param name="id">Unique identifier for the user</param>
    /// <param name="name">User's display name</param>
    /// <param name="email">Pre-validated Email value object</param>
    /// <param name="username">User's unique username</param>
    /// <param name="password">Pre-validated Password value object</param>
    /// <param name="role">Pre-validated Role value object</param>
    /// <returns>Result containing the UserAggregate if valid, or validation errors if invalid</returns>
    public static Result<UserAggregate> Create(Guid id, string name, Email email, string username, Password password, Role role)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new ValidationError("Name.Required", "Name is required"));
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            errors.Add(new ValidationError("Username.TooShort", "Username must be at least 3 characters"));
        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());

        var aggregate = new UserAggregate();
        var @event = new UserCreatedEvent(id, name, email, username, password, role, DateTime.UtcNow);
        aggregate.ApplyEvent(@event);
        return Result.Success(aggregate);
    }

    /// <summary>
    /// SAFE FACTORY METHOD: Creates UserAggregate from primitive values with automatic validation.
    /// This method prevents invalid objects from being passed to the aggregate.
    /// </summary>
    /// <param name="id">Unique identifier for the user</param>
    /// <param name="name">User's display name</param>
    /// <param name="emailValue">Email string to validate</param>
    /// <param name="username">User's unique username</param>
    /// <param name="passwordValue">Plain password to validate and hash</param>
    /// <param name="roleValue">Role string to validate</param>
    /// <returns>Result containing the UserAggregate if all validations pass</returns>
    public static Result<UserAggregate> CreateFromPrimitives(
        Guid id,
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
        if (!emailResult.IsSuccess)
            errors.AddRange(emailResult.ValidationErrors);
        if (!passwordResult.IsSuccess)
            errors.AddRange(passwordResult.ValidationErrors);
        if (!roleResult.IsSuccess)
            errors.AddRange(roleResult.ValidationErrors);
        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());
        return Create(id, name, emailResult.Value, username, passwordResult.Value, roleResult.Value);
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
    public static UserAggregate FromProjection(Guid id, string name, Email email, string username,
        Password password, Role role, DateTime createdAt, DateTime? updatedAt, bool isActive)
    {
        return new UserAggregate
        {
            Id = id,
            Name = name,
            Email = email,
            Username = username,
            PasswordHash = password,
            Role = role,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsActive = isActive
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
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new ValidationError("Name.Required", "Name is required"));
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            errors.Add(new ValidationError("Username.TooShort", "Username must be at least 3 characters"));
        if (errors.Count > 0)
            return Result.Invalid(errors.ToArray());

        // Email is already validated by the Value Object
        var @event = new UserUpdatedEvent(Id, name, email, username, DateTime.UtcNow);
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
        // Password is already validated by the Value Object
        var @event = new UserPasswordChangedEvent(Id, newPassword, DateTime.UtcNow);
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
            return Result.Invalid(new ValidationError("Role.Invalid", "Invalid role value"));
        if (Role.Value == newRole.Value)
            return Result.Invalid(new ValidationError("Role.SameRole", "User already has this role"));
        var @event = new UserRoleChangedEvent(Id, newRole, DateTime.UtcNow);
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
            return Result.Invalid(new ValidationError("User.AlreadyActive", "User is already active"));
        var @event = new UserActivatedEvent(Id, DateTime.UtcNow);
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
            return Result.Invalid(new ValidationError("User.AlreadyInactive", "User is already deactivated"));
        var @event = new UserDeactivatedEvent(Id, DateTime.UtcNow);
        ApplyEvent(@event);
        return Result.Success();
    }

    // Event application methods
    public void Apply(UserCreatedEvent @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Email = @event.Email;
        Username = @event.Username;
        PasswordHash = @event.Password;
        Role = @event.Role;
        CreatedAt = @event.CreatedAt;
        IsActive = true;
    }

    public void Apply(UserUpdatedEvent @event)
    {
        Name = @event.Name;
        Email = @event.Email;
        Username = @event.Username;
        UpdatedAt = @event.UpdatedAt;
    }

    public void Apply(UserPasswordChangedEvent @event)
    {
        PasswordHash = @event.NewPassword;
        UpdatedAt = @event.ChangedAt;
    }

    public void Apply(UserRoleChangedEvent @event)
    {
        Role = @event.NewRole;
        UpdatedAt = @event.ChangedAt;
    }

    public void Apply(UserActivatedEvent @event)
    {
        IsActive = true;
        UpdatedAt = @event.ActivatedAt;
    }

    public void Apply(UserDeactivatedEvent @event)
    {
        IsActive = false;
        UpdatedAt = @event.DeactivatedAt;
    }

    /// <summary>
    /// Applies an event to the aggregate and adds it to uncommitted events
    /// </summary>
    /// <param name="event">Domain event to apply</param>
    private void ApplyEvent(object @event)
    {
        _uncommittedEvents.Add(@event);

        // Apply the event to update the aggregate state
        switch (@event)
        {
            case UserCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case UserUpdatedEvent updatedEvent:
                Apply(updatedEvent);
                break;
            case UserPasswordChangedEvent passwordChangedEvent:
                Apply(passwordChangedEvent);
                break;
            case UserRoleChangedEvent roleChangedEvent:
                Apply(roleChangedEvent);
                break;
            case UserActivatedEvent activatedEvent:
                Apply(activatedEvent);
                break;
            case UserDeactivatedEvent deactivatedEvent:
                Apply(deactivatedEvent);
                break;
        }
    }

    /// <summary>
    /// Marks all uncommitted events as committed (called after persistence)
    /// </summary>
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}

// Domain Events - using Value Objects for type safety
public record UserCreatedEvent(Guid Id, string Name, Email Email, string Username, Password Password, Role Role, DateTime CreatedAt);
public record UserUpdatedEvent(Guid Id, string Name, Email Email, string Username, DateTime UpdatedAt);
public record UserPasswordChangedEvent(Guid Id, Password NewPassword, DateTime ChangedAt);
public record UserRoleChangedEvent(Guid Id, Role NewRole, DateTime ChangedAt);
public record UserActivatedEvent(Guid Id, DateTime ActivatedAt);
public record UserDeactivatedEvent(Guid Id, DateTime DeactivatedAt);
