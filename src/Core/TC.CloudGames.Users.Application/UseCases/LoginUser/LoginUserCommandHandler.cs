using TC.CloudGames.SharedKernel.Application.Handlers;
using TC.CloudGames.SharedKernel.Infrastructure.Authentication;

namespace TC.CloudGames.Users.Application.UseCases.LoginUser;

/// <summary>
/// Handler responsible for authenticating a user and generating a JWT token.
/// Inherits directly from BaseHandler because no aggregate or domain events are involved.
/// </summary>
internal sealed class LoginUserCommandHandler : BaseHandler<LoginUserCommand, LoginUserResponse>
{
    private readonly IUserRepository _repository;
    private readonly ITokenProvider _tokenProvider;

    public LoginUserCommandHandler(IUserRepository repository, ITokenProvider tokenProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public override async Task<Result<LoginUserResponse>> ExecuteAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        var userTokenInfo = await _repository
            .GetUserTokenInfoAsync(command.Email, command.Password, ct)
            .ConfigureAwait(false);

        if (userTokenInfo is null)
        {
            AddError(UserDomainErrors.InvalidCredentials.Property,
                     UserDomainErrors.InvalidCredentials.ErrorMessage,
                     UserDomainErrors.InvalidCredentials.ErrorCode);

            // Returns a NotFound result using the shared validation helper
            return BuildNotFoundResult();
        }

        var response = new LoginUserResponse(
            JwtToken: _tokenProvider.Create(userTokenInfo),
            Email: userTokenInfo.Email
        );

        return Result<LoginUserResponse>.Success(response);
    }
}