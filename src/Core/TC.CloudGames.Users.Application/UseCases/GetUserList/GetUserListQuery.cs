namespace TC.CloudGames.Application.Users.GetUserList
{
    public sealed record GetUserListQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string SortBy = "id",
        string SortDirection = "asc",
        string Filter = ""
    ) : ICachedQuery<IReadOnlyList<UserListResponse>>
    {
        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetUserListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetUserListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{cacheKey}";
        }
    }
}
