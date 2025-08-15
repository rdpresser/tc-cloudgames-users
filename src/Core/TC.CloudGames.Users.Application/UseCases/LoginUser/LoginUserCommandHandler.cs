using TC.CloudGames.SharedKernel.Application.Commands;
using TC.CloudGames.SharedKernel.Infrastructure.Authentication;

namespace TC.CloudGames.Users.Application.UseCases.LoginUser;

internal sealed class LoginUserCommandHandler : BaseCommandHandler<LoginUserCommand, LoginUserResponse, UserAggregate, IUserRepository>
{
    private readonly ITokenProvider _tokenProvider;

    public LoginUserCommandHandler(IUserRepository repository, ITokenProvider tokenProvider)
        : base(repository)
    {
        _tokenProvider = tokenProvider;
    }

    public override async Task<Result<LoginUserResponse>> ExecuteAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        var userTokenInfo = await Repository.GetUserTokenInfoAsync
            (
                command.Email, command.Password, ct
            ).ConfigureAwait(false);

        if (userTokenInfo is null)
        {
            AddError(UserDomainErrors.InvalidCredentials.Property, UserDomainErrors.InvalidCredentials.ErrorMessage,
                UserDomainErrors.InvalidCredentials.ErrorCode);

            return ValidationErrorNotFound();
        }

        return new LoginUserResponse(
            JwtToken: _tokenProvider.Create(userTokenInfo),
            Email: userTokenInfo.Email
        );
    }
}