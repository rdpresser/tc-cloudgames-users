using TC.CloudGames.Users.Application.Abstractions.Queries;

namespace TC.CloudGames.Users.Application.UseCases.GetUserByEmail
{
    internal sealed class GetUserByEmailQueryHandler : BaseQueryHandler<GetUserByEmailQuery, UserByEmailResponse>
    {
        private readonly IUserRepository _userRepository;
        //private readonly IUserContext _userContext

        public GetUserByEmailQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            //_userContext = userContext ?? throw new ArgumentNullException(nameof(userContext))
        }

        public override async Task<Result<UserByEmailResponse>> ExecuteAsync(GetUserByEmailQuery command, CancellationToken ct = default)
        {
            UserByEmailResponse? userResponse = null;

            // if _userContext.UserRole == AppConstants.UserRole
            //     !_userContext.UserEmail.Equals(command.Email, StringComparison.InvariantCultureIgnoreCase))
            //
            //    AddError(x => x.Email, "You are not authorized to access this user.", $"{nameof(GetUserByEmailQuery.Email)}.NotAuthorized")
            //    return ValidationErrorNotAuthorized()
            //

            userResponse = await _userRepository
                    .GetByEmailAsync(command.Email, ct)
                    .ConfigureAwait(false);

            if (userResponse is not null)
                return userResponse;

            AddError(x => x.Email, $"User with email '{command.Email}' not found.", UserDomainErrors.NotFound.ErrorCode);
            return ValidationErrorNotFound();
        }
    }
}
