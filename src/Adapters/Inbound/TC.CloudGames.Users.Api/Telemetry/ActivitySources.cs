using System.Diagnostics;

namespace TC.CloudGames.Users.Api.Telemetry
{
    [ExcludeFromCodeCoverage]
    public static class ActivitySources
    {
        public static readonly ActivitySource UserActivities = new(TelemetryConstants.UserActivitySource, TelemetryConstants.Version);
        public static readonly ActivitySource DatabaseActivities = new(TelemetryConstants.DatabaseActivitySource, TelemetryConstants.Version);
        public static readonly ActivitySource CacheActivities = new(TelemetryConstants.CacheActivitySource, TelemetryConstants.Version);

        public static Activity? StartGameOperation(string operationName, string gameId, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = UserActivities.StartActivity(operationName);
            activity?.SetTag(TelemetryConstants.ServiceComponent, TelemetryConstants.UserComponent);
            activity?.SetTag(TelemetryConstants.UserId, userId);
            return activity;
        }

        public static Activity? StartUserOperation(string operationName, string userId)
        {
            var activity = UserActivities.StartActivity(operationName);
            activity?.SetTag(TelemetryConstants.ServiceComponent, TelemetryConstants.UserComponent);
            activity?.SetTag(TelemetryConstants.UserId, userId);
            return activity;
        }

        public static Activity? StartDatabaseOperation(string operationName, string tableName)
        {
            var activity = DatabaseActivities.StartActivity(operationName);
            activity?.SetTag(TelemetryConstants.ServiceComponent, TelemetryConstants.DatabaseComponent);
            activity?.SetTag("db.table", tableName);
            return activity;
        }

        public static Activity? StartCacheOperation(string operationName, string cacheKey)
        {
            var activity = CacheActivities.StartActivity(operationName);
            activity?.SetTag(TelemetryConstants.ServiceComponent, TelemetryConstants.CacheComponent);
            activity?.SetTag("cache.key", cacheKey);
            return activity;
        }
    }
}
