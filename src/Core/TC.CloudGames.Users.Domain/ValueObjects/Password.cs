namespace TC.CloudGames.Users.Domain.ValueObjects;

/// <summary>
/// Value Object representing a user password with validation and hashing.
/// </summary>
public sealed record Password
{
    public static readonly ValidationError Required = new("Password.Required", "Password is required.");
    public static readonly ValidationError TooShort = new("Password.TooShort", "Password must be at least 8 characters.");
    public static readonly ValidationError TooLong = new("Password.TooLong", "Password cannot exceed 128 characters.");
    public static readonly ValidationError WeakPassword = new("Password.Weak", "Password must contain at least one uppercase, lowercase, number and special character.");

    public string Hash { get; }

    private Password(string hash)
    {
        Hash = hash;
    }

    /// <summary>
    /// Validates a plain text password value.
    /// </summary>
    /// <param name="value">The plain text password to validate.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    private static Result ValidateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Required);

        if (value.Length < 8)
            return Result.Invalid(TooShort);

        if (value.Length > 128)
            return Result.Invalid(TooLong);

        if (!IsStrongPassword(value))
            return Result.Invalid(WeakPassword);

        return Result.Success();
    }

    /// <summary>
    /// Creates a new Password from a plain text password with validation and hashing.
    /// </summary>
    /// <param name="plainPassword">The plain text password to validate and hash.</param>
    /// <returns>Result containing the Password if valid, or validation errors if invalid.</returns>
    public static Result<Password> Create(string plainPassword)
    {
        var validation = ValidateValue(plainPassword);
        if (!validation.IsSuccess)
            return Result.Invalid(validation.ValidationErrors);

        var hash = HashPassword(plainPassword);
        return Result.Success(new Password(hash));
    }

    /// <summary>
    /// Creates a Password from an existing hash (for reconstruction from database).
    /// </summary>
    /// <param name="hash">The pre-computed password hash.</param>
    /// <returns>Result containing the Password if hash is valid.</returns>
    public static Result<Password> FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result.Invalid(Required);

        return Result.Success(new Password(hash));
    }

    /// <summary>
    /// Validates a Password instance, ensuring the hash is present.
    /// Does NOT validate password strength or format (that is done in Create).
    /// </summary>
    public static Result Validate(Password? password)
    {
        if (password == null || string.IsNullOrWhiteSpace(password.Hash))
            return Result.Invalid(Required);

        // Optionally, add hash format validation here
        return Result.Success();
    }

    /// <summary>
    /// Verifies if the provided plain password matches this password hash.
    /// </summary>
    /// <param name="plainPassword">The plain text password to verify.</param>
    /// <returns>True if password matches, false otherwise.</returns>
    public bool Verify(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return false;

        return BCrypt.Net.BCrypt.EnhancedVerify(plainPassword, Hash);
    }

    /// <summary>
    /// Validates if password meets strength requirements.
    /// </summary>
    /// <param name="password">Password to validate.</param>
    /// <returns>True if password is strong enough.</returns>
    private static bool IsStrongPassword(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    /// <param name="password">Plain text password to hash.</param>
    /// <returns>BCrypt hash of the password.</returns>
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Validates a Password value.
    /// </summary>
    public static bool TryValidate(Password? value, out IReadOnlyCollection<ValidationError> errors)
    {
        var result = Validate(value);
        errors = !result.IsSuccess ? [.. result.ValidationErrors] : [];
        return result.IsSuccess;
    }

    /// <summary>
    /// Validates if the password is valid
    /// </summary>
    public static bool IsValid(Password? value) => Validate(value).IsSuccess;

    /// <summary>
    /// Implicit conversion from Password to string.
    /// </summary>
    /// <param name="password">The Password instance.</param>
    public static implicit operator string(Password password) => password.Hash;

    /// <summary>
    /// Implicit conversion from string to Password.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    ///public static implicit operator Password(string password) => Create(password).Value;

    /// <summary>
    /// Validates a plain text password value (before hashing).
    /// </summary>
    /// <param name="plainPassword">The plain text password to validate.</param>
    /// <param name="errors">List of validation errors.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool TryValidateValue(string? plainPassword, out IReadOnlyCollection<ValidationError> errors)
    {
        var result = ValidateValue(plainPassword);
        errors = !result.IsSuccess ? [.. result.ValidationErrors] : [];
        return result.IsSuccess;
    }
}