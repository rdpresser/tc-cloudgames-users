namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserList
{
    public class GetUserListQueryTests
    {
        [Fact]
        public void GetUserListQuery_DefaultValues_ShouldBeSetCorrectly()
        {
            var query = new GetUserListQuery();
            query.PageNumber.ShouldBe(1);
            query.PageSize.ShouldBe(10);
            query.SortBy.ShouldBe("id");
            query.SortDirection.ShouldBe("asc");
            query.Filter.ShouldBe("");
            query.GetCacheKey.ShouldBe("GetUserListQuery-1-10-id-asc-");
        }

        [Fact]
        public void GetUserListQuery_CustomValues_ShouldBeSetCorrectly()
        {
            var query = new GetUserListQuery(2, 20, "name", "desc", "search");
            query.PageNumber.ShouldBe(2);
            query.PageSize.ShouldBe(20);
            query.SortBy.ShouldBe("name");
            query.SortDirection.ShouldBe("desc");
            query.Filter.ShouldBe("search");
            query.GetCacheKey.ShouldBe("GetUserListQuery-2-20-name-desc-search");
        }

        [Fact]
        public void GetUserListQuery_SetCacheKey_ShouldUpdateCacheKey()
        {
            var query = new GetUserListQuery(1, 10, "id", "asc", "filter");
            query.SetCacheKey("custom");
            query.GetCacheKey.ShouldBe("GetUserListQuery-1-10-id-asc-filter-custom");
        }
    }
}
