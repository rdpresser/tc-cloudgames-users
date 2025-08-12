using TC.CloudGames.Users.Application.UseCases.GetUserByEmail;
using Shouldly;
using Xunit;
using System;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.GetUserByEmail;

public class UserByEmailResponseTests
{
    [Fact]
    public void Properties_ShouldBeSetAndGetCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test User";
        var username = "testuser";
        var email = "test@example.com";
        var role = "User";

        // Act
        var response = new UserByEmailResponse
        {
            Id = id,
            Name = name,
            Username = username,
            Email = email,
            Role = role
        };

        // Assert
        response.Id.ShouldBe(id);
        response.Name.ShouldBe(name);
        response.Username.ShouldBe(username);
        response.Email.ShouldBe(email);
        response.Role.ShouldBe(role);
    }
}
