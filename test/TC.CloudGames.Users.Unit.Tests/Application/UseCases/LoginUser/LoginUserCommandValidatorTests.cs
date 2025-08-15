using TC.CloudGames.Users.Application.UseCases.LoginUser;

namespace TC.CloudGames.Users.Unit.Tests.Application.UseCases.LoginUser;

public class LoginUserCommandValidatorTests
{
    [Theory]
    [InlineData("", "ValidPass123!", false)]
    [InlineData("invalid-email", "ValidPass123!", false)]
    [InlineData("valid@email.com", "", false)]
    [InlineData("valid@email.com", "short", false)]
    [InlineData("valid@email.com", "alllowercase123!", false)]
    [InlineData("valid@email.com", "ALLUPPERCASE123!", false)]
    [InlineData("valid@email.com", "NoNumbersHere!", false)]
    [InlineData("valid@email.com", "NoSpecialChars123", false)]
    [InlineData("valid@email.com", "ValidPass123!", true)]
    public async Task Validate_ShouldReturnExpectedResult(string email, string password, bool expectedIsValid)
    {
        var validator = new LoginUserCommandValidator();
        var command = new LoginUserCommand(email, password);
        var result = await validator.ValidateAsync(command, CancellationToken.None);
        result.IsValid.ShouldBe(expectedIsValid);
    }
}
