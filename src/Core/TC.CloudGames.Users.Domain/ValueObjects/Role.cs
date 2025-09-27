using System.Text.Json;
using System.Text.Json.Serialization;

namespace TC.CloudGames.Users.Domain.ValueObjects;

/// <summary>
/// Value Object representing a user role with validation and predefined roles.
/// </summary>
public sealed record Role
{
    public static readonly ValidationError Invalid = new("Role.Invalid", "Invalid role value.");

    // Predefined roles
    public static readonly Role User = new("User");
    public static readonly Role Admin = new("Admin");
    public static readonly Role Moderator = new("Moderator");
    public static readonly string[] ValidRoles = { "User", "Admin", "Moderator" };

    public string Value { get; }

    [JsonConstructor]
    public Role(string value)
    {
        Value = value;
    }


    ////private Role(string value)
    ////{
    ////    Value = value;
    ////}

    /// <summary>
    /// Validates a role value.
    /// </summary>
    /// <param name="value">The role name to validate.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    private static Result ValidateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Invalid);

        var normalizedValue = ValidRoles.FirstOrDefault(r =>
            string.Equals(r, value, StringComparison.OrdinalIgnoreCase));

        if (normalizedValue == null)
            return Result.Invalid(Invalid);

        return Result.Success();
    }

    /// <summary>
    /// Creates a new Role with validation.
    /// </summary>
    /// <param name="value">The role name to validate.</param>
    /// <returns>Result containing the Role if valid, or validation errors if invalid.</returns>
    public static Result<Role> Create(string value)
    {
        var validation = ValidateValue(value);
        if (!validation.IsSuccess)
            return Result.Invalid(validation.ValidationErrors);

        var normalizedValue = ValidRoles.First(r =>
            string.Equals(r, value, StringComparison.OrdinalIgnoreCase));

        return Result.Success(new Role(normalizedValue));
    }

    /// <summary>
    /// Validates a Role instance.
    /// </summary>
    /// <param name="role">The Role instance to validate.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public static Result Validate(Role? role)
    {
        if (role == null)
            return Result.Invalid(Invalid);

        return ValidateValue(role.Value);
    }

    /// <summary>
    /// Checks if the role value is valid.
    /// </summary>
    public static bool IsValid(Role? value) => Validate(value).IsSuccess;

    /// <summary>
    /// Create a Role value object from a database string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Result<Role> FromDb(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Invalid);

        return Result.Success(new Role(value));
    }

    /// <summary>
    /// Tries to validate a Role instance and returns validation errors if any.
    /// </summary>
    public static bool TryValidate(Role? value, out IReadOnlyCollection<ValidationError> errors)
    {
        var result = Validate(value);
        errors = !result.IsSuccess ? [.. result.ValidationErrors] : [];
        return result.IsSuccess;
    }

    /// <summary>
    /// Validates the role text valiue
    /// </summary>
    /// <param name="value">The text role to validate</param>
    /// <param name="errors">List of errors</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public static bool TryValidateValue(string? value, out IReadOnlyCollection<ValidationError> errors)
    {
        var result = ValidateValue(value);
        errors = !result.IsSuccess ? [.. result.ValidationErrors] : [];
        return result.IsSuccess;
    }

    /// <summary>
    /// Checks if the user has administrative privileges.
    /// </summary>
    /// <returns>True if user is Admin, false otherwise.</returns>
    public bool IsAdmin() => Value.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the user has moderation privileges.
    /// </summary>
    /// <returns>True if user is Admin or Moderator, false otherwise.</returns>
    public bool CanModerate() => IsAdmin() || Value.Equals("Moderator", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Implicit conversion from Role to string.
    /// </summary>
    /// <param name="role">The Role instance.</param>
    public static implicit operator string(Role role) => role.Value;
    ////public static implicit operator Role(string role) => Create(role).Value;

    /// <summary>
    /// Implicit conversion from string to Role.
    /// </summary>
    /// <param name="role">The role name.</param>
    ///public static implicit operator Role(string role) => Create(role).Value;
}

public sealed class RoleJsonConverter : JsonConverter<Role>
{
    public override Role Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Role.FromDb(reader.GetString()!).Value;

    public override void Write(Utf8JsonWriter writer, Role value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}