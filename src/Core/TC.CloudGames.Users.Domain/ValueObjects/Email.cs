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

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid(Required);
        }

        if (value.Length > MaxLength)
        {
            return Result.Invalid(MaximumLength);
        }

        if (!EmailRegex.IsMatch(value))
        {
            return Result.Invalid(Invalid);
        }

        return Result.Success(new Email(value.ToLowerInvariant()));
    }

    public static implicit operator string(Email email) => email.Value;

    //TODO: Remove this implicit operator if not needed
    public static implicit operator Email(string email) => Create(email).Value;
}