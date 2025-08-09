namespace TC.CloudGames.Users.Application.UseCases.CreateUser
{
    public static class CreateUserMapper
    {
        public static Result<UserAggregate> ToEntity(CreateUserCommand r)
        {
            return UserAggregate.Create(
                r.Name,
                r.Email,
                r.Username,
                r.Password,
                r.Role);
        }

        public static CreateUserResponse FromEntity(UserAggregate e)
        {
            return new CreateUserResponse
            (
                Id: e.Id,
                Name: e.Name,
                Username: e.Username,
                Email: e.Email.Value,
                Role: e.Role.Value
            );
        }
    }
}
