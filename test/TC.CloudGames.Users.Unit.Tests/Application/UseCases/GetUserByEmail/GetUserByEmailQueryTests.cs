using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserByEmail;

public class GetUserByEmailQueryTests
{
    [Fact]
    public void Properties_ShouldBeSetAndGetCorrectly()
    {
        // Arrange
        var email = "test@example.com";
        var cacheKey = "user-cache-key";

        // Act
        var query = new GetUserByEmailQuery(email);
        query.SetCacheKey(cacheKey);

        // Assert
        query.Email.ShouldBe(email);
        query.CacheKey.ShouldBe(cacheKey);
    }

    // Duration and DistributedCacheDuration are read-only and always null, so test only the getter
    [Fact]
    public void Duration_And_DistributedCacheDuration_ShouldBeNull()
    {
        var query = new GetUserByEmailQuery("test@example.com");
        query.Duration.ShouldBeNull();
        query.DistributedCacheDuration.ShouldBeNull();
    }
}
