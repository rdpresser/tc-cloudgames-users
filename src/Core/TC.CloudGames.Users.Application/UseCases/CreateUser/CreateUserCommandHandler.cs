using TC.CloudGames.SharedKernel.Application.Commands;

namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    internal sealed class CreateUserCommandHandler : BaseCommandHandler<CreateUserCommand, CreateUserResponse, UserAggregate, IUserRepository>
    {
        public CreateUserCommandHandler(IUserRepository repository)
            : base(repository)
        {

        }

        public override async Task<Result<CreateUserResponse>> ExecuteAsync(CreateUserCommand command,
            CancellationToken ct = default)
        {

            //var httpContext = _httpContextAccessor.HttpContext

            //var entity = CreateUserMapper.ToEntity(
            //    command,
            //    userId: httpContext?.User?.FindFirst("sub")?.Value,
            //    correlationId: httpContext?.TraceIdentifier,
            //    source: "UserRegistrationAPI"
            //)

            var entity = CreateUserMapper.ToEntity(command);

            if (!entity.IsSuccess)
            {
                AddErrors(entity.ValidationErrors);
                return Result.Invalid(entity.ValidationErrors);
            }

            await Repository.SaveAsync(entity.Value, ct).ConfigureAwait(false);

            return CreateUserMapper.FromEntity(entity);
        }
    }
}
