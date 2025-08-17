namespace TC.CloudGames.Users.Domain.ValueObjects;

public sealed record Email
{
    private const int MaxLength = 200;

    public static readonly ValidationError Required = new("Email.Required", "Email is required.");
    public static readonly ValidationError Invalid = new("Email.InvalidFormat", "Invalid email format.");
    public static readonly ValidationError MaximumLength = new("Email.MaximumLength", $"Email cannot exceed {MaxLength} characters.");

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    private static Result ValidateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Required);

        if (value!.Length > MaxLength)
            return Result.Invalid(MaximumLength);

        if (!EmailRegex.IsMatch(value))
            return Result.Invalid(Invalid);

        return Result.Success();
    }

    public static Result<Email> Create(string value)
    {
        var validation = ValidateValue(value);
        if (!validation.IsSuccess)
            return Result.Invalid(validation.ValidationErrors);

        return Result.Success(new Email(value.ToLowerInvariant()));
    }

    public static Result Validate(Email? email)
    {
        if (email == null)
            return Result.Invalid(Required);

        return ValidateValue(email.Value);
    }

    /// <summary>
    /// Create an Email value object from a database string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Result<Email> FromDb(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Invalid(Required);

        return Result.Success(new Email(value));
    }

    /// <summary>
    /// Validates an email value and returns a list of validation errors if any.
    /// </summary>
    public static bool TryValidate(Email? value, out IReadOnlyCollection<ValidationError> errors)
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
    /// Validates an email value.
    /// </summary>
    public static bool IsValid(Email? value) => Validate(value).IsSuccess;

    public static implicit operator string(Email email) => email.Value;
    ///public static implicit operator Email(string email) => Create(email).Value;
}
