﻿namespace TC.CloudGames.Application.Users.GetUserList;

internal sealed class GetUserListQueryHandler : BaseQueryHandler<GetUserListQuery, IReadOnlyList<UserListResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetUserListQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public override async Task<Result<IReadOnlyList<UserListResponse>>> ExecuteAsync(GetUserListQuery query,
        CancellationToken ct = default)
    {
        var users = await _userRepository.GetUserListAsync(query, ct).ConfigureAwait(false);

        if (users is null || !users.Any())
            return Result<IReadOnlyList<UserListResponse>>.Success([]);

        return Result.Success<IReadOnlyList<UserListResponse>>([.. users]);
    }
}