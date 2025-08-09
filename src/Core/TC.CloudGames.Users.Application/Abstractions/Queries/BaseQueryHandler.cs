namespace TC.CloudGames.Users.Application.Abstractions.Queries
{
    internal abstract class BaseQueryHandler<TQuery, TResponse> : CommandHandler<TQuery, Result<TResponse>>
            where TQuery : IBaseQuery<TResponse>
            where TResponse : class
    {
        private FastEndpoints.ValidationContext<TQuery> ValidationContext { get; } = Instance;

        protected Result<TResponse> ValidationErrorNotFound()
        {
            if (ValidationContext.ValidationFailures.Count == 0)
            {
                return Result<TResponse>.Success(default!);
            }

            return Result<TResponse>.NotFound([.. ValidationContext.ValidationFailures.Select(x => x.ErrorMessage)]);
        }

        protected Result<TResponse> ValidationErrorNotAuthorized()
        {
            if (ValidationContext.ValidationFailures.Count == 0)
            {
                return Result<TResponse>.Success(default!);
            }

            return Result<TResponse>.Unauthorized([.. ValidationContext.ValidationFailures.Select(x => x.ErrorMessage)]);
        }
    }
}
