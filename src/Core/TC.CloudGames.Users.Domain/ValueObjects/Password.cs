namespace TC.CloudGames.Users.Domain.ValueObjects;

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
    /// Creates a new Password from a plain text password with validation and hashing
    /// </summary>
    /// <param name="plainPassword">The plain text password to validate and hash</param>
    /// <returns>Result containing the Password if valid, or validation errors if invalid</returns>
    public static Result<Password> Create(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            return Result.Invalid(Required);

        if (plainPassword.Length < 8)
            return Result.Invalid(TooShort);

        if (plainPassword.Length > 128)
            return Result.Invalid(TooLong);

        if (!IsStrongPassword(plainPassword))
            return Result.Invalid(WeakPassword);

        var hash = HashPassword(plainPassword);
        return Result.Success(new Password(hash));
    }

    /// <summary>
    /// Creates a Password from an existing hash (for reconstruction from database)
    /// </summary>
    /// <param name="hash">The pre-computed password hash</param>
    /// <returns>Result containing the Password if hash is valid</returns>
    public static Result<Password> FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result.Invalid(Required);

        return Result.Success(new Password(hash));
    }

    /// <summary>
    /// Verifies if the provided plain password matches this password hash
    /// </summary>
    /// <param name="plainPassword">The plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool Verify(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return false;
        return BCrypt.Net.BCrypt.Verify(plainPassword, Hash);
    }

    /// <summary>
    /// Validates if password meets strength requirements
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password is strong enough</returns>
    private static bool IsStrongPassword(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>BCrypt hash of the password</returns>
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static implicit operator string(Password password) => password.Hash;
    public static implicit operator Password(string hash) => Create(hash).Value;
}