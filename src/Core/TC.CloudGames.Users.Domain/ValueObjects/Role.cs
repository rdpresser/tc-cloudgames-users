namespace TC.CloudGames.Users.Domain.ValueObjects;

public sealed record Role
{
    public static readonly ValidationError Invalid = new("Role.Invalid", "Invalid role value.");

    // Predefined roles
    public static readonly Role User = new("User");
    public static readonly Role Admin = new("Admin");
    public static readonly Role Moderator = new("Moderator");

    public static readonly string[] ValidRoles = { "User", "Admin", "Moderator" };

    public string Value { get; }

    private Role(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Role with validation
    /// </summary>
    /// <param name="value">The role name to validate</param>
    /// <returns>Result containing the Role if valid, or validation errors if invalid</returns>
    public static Result<Role> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Invalid);

        var normalizedValue = ValidRoles.FirstOrDefault(r =>
            string.Equals(r, value, StringComparison.OrdinalIgnoreCase));

        if (normalizedValue == null)
            return Result.Invalid(Invalid);

        return Result.Success(new Role(normalizedValue));
    }

    /// <summary>
    /// Checks if the user has administrative privileges
    /// </summary>
    /// <returns>True if user is Admin, false otherwise</returns>
    public bool IsAdmin() => Value.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the user has moderation privileges
    /// </summary>
    /// <returns>True if user is Admin or Moderator, false otherwise</returns>
    public bool CanModerate() => IsAdmin() || Value.Equals("Moderator", StringComparison.OrdinalIgnoreCase);

    public static implicit operator string(Role role) => role.Value;
    public static implicit operator Role(string role) => Create(role).Value;
}