﻿using System.Diagnostics.CodeAnalysis;

namespace TC.CloudGames.Users.Domain.Abstractions
{
    [ExcludeFromCodeCoverage]
    public record DomainError(string Property, string ErrorMessage, string ErrorCode = default!)
    {
        public static readonly DomainError None = new(string.Empty, string.Empty, string.Empty);

        public static readonly DomainError NullValue = new("Error.NullValue", "Null value was provided", string.Empty);
    }
}
