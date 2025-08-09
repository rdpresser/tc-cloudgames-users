using TC.CloudGames.Users.Application.Abstractions.Ports;

namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    internal sealed class CreateUserCommandHandler : BaseCommandHandler<CreateUserCommand, CreateUserResponse>
    {
        public CreateUserCommandHandler(IUserRepository repository)
            : base(repository)
        {

        }

        public override async Task<Result<CreateUserResponse>> ExecuteAsync(CreateUserCommand command,
            CancellationToken ct = default)
        {
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
