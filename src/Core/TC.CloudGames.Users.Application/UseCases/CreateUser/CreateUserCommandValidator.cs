namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public sealed class CreateUserCommandValidator : Validator<CreateUserCommand>
    {
        public CreateUserCommandValidator(IUserRepository userRepository)
        {
            #region Name | Validation Rules
            RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode($"{nameof(CreateUserCommand.Name)}.Required")
            .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters long.")
                .WithErrorCode($"{nameof(CreateUserCommand.Name)}.MinimumLength")
            .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters.")
                .WithErrorCode($"{nameof(CreateUserCommand.Name)}.MaximumLength")
            .Matches(@"^[a-zA-Z ]+$")
                .WithMessage("Name can only contain letters and spaces.")
                .WithErrorCode($"{nameof(CreateUserCommand.Name)}.InvalidCharacters");

            #endregion

            #region Email | Validation Rules
            RuleFor(x => x.Email)
                .NotEmpty()
                    .WithMessage("Email is required.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Email)}.Required")
                .EmailAddress()
                    .WithMessage("Invalid email format.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Email)}.InvalidFormat")
                .MustAsync(async (email, cancellation) => !await userRepository.EmailExistsAsync(email, cancellation).ConfigureAwait(false))
                    .WithMessage("Email already exists.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Email)}.AlreadyExists");

            #endregion

            #region Username | Validation Rules
            RuleFor(x => x.Username)
                .NotEmpty()
                    .WithMessage("Username is required.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Username)}.Required")
                .MinimumLength(3)
                    .WithMessage("Username must be at least 3 characters long.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Username)}.MinimumLength")
                .MaximumLength(100)
                    .WithMessage("Username must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Username)}.MaximumLength")
                .Matches(@"^[a-zA-Z][a-zA-Z0-9]*$")
                    .WithMessage("Username must start with a letter and can contain only letters and numbers.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Username)}.InvalidCharacters");
            #endregion

            #region Password | Validation Rules
            RuleFor(x => x.Password)
                .NotEmpty()
                    .WithMessage("Password is required.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.Required")
                .MinimumLength(8)
                    .WithMessage("Password must be at least 8 characters long.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.MinimumLength")
                .Matches(@"[A-Z]")
                    .WithMessage("Password must contain at least one uppercase letter.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.Uppercase")
                .Matches(@"[a-z]")
                    .WithMessage("Password must contain at least one lowercase letter.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.Lowercase")
                .Matches(@"\d")
                    .WithMessage("Password must contain at least one number.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.Digit")
                .Matches(@"[\W_]")
                    .WithMessage("Password must contain at least one special character.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Password)}.SpecialCharacter");
            #endregion

            #region Role | Validation Rules
            RuleFor(x => x.Role)
                .NotEmpty()
                    .WithMessage("Role is required.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Role)}.Required")
                .Must(role => Role.ValidRoles.Contains(role))
                    .WithMessage($"Invalid role specified. Valid roles are: {Role.ValidRoles.JoinWithQuotes()}.")
                    .WithErrorCode($"{nameof(CreateUserCommand.Role)}.InvalidRole");
            #endregion
        }
    }
}
