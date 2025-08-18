namespace TC.CloudGames.Users.Application.UseCases.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : ICachedQuery<UserByEmailResponse>
    {
        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetUserByEmailQuery-{Email}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetUserByEmailQuery-{Email}-{cacheKey}";
        }
    }
}
