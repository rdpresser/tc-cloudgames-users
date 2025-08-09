namespace TC.CloudGames.Users.Application.UseCases.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : ICachedQuery<UserByEmailResponse>
    {
        private string? _cacheKey;
        public string CacheKey
        {
            get => _cacheKey ?? $"GetUserByEmailQuery-{Email}";
            set => _cacheKey = value;
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;
    }
}
