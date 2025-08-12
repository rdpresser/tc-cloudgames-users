using TC.CloudGames.Users.Unit.Tests.Common;

namespace TC.CloudGames.Users.Unit.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for Email value object
/// </summary>
public class EmailTests
{
    #region Valid Email Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test123@test-domain.com")]
    [InlineData("first+last@example.org")]
    [InlineData("user_name@example.net")]
    [InlineData("1234567890@example.com")]
    [InlineData("test@subdomain.example.com")]
    public void Create_WithValidEmail_ShouldSucceed(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe(validEmail.ToLowerInvariant());
    }

    [Fact]
    public void Create_WithValidEmail_ShouldNormalizeToLowerCase()
    {
        // Arrange
        var emailInput = "TEST@EXAMPLE.COM";
        var expectedOutput = "test@example.com";

        // Act
        var result = Email.Create(emailInput);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expectedOutput);
    }

    #endregion

    #region Invalid Email Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldFail(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Email.Required");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    [InlineData("test@@example.com")]
    [InlineData("test@example")]
    [InlineData("test@.com")]
    [InlineData("test@example.")]
    [InlineData("test user@example.com")]
    [InlineData("test@exam ple.com")]
    public void Create_WithInvalidFormat_ShouldFail(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Email.InvalidFormat");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Invalid email format.");
    }

    [Fact]
    public void Create_WithEmailTooLong_ShouldFail()
    {
        // Arrange - Create email longer than 200 characters
        var longLocalPart = new string('a', 190);
        var longEmail = $"{longLocalPart}@example.com"; // Total > 200 chars

        // Act
        var result = Email.Create(longEmail);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Email.MaximumLength");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Email cannot exceed 200 characters.");
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var emailValue = "implicit@test.com";
        var email = Email.Create(emailValue).Value;

        // Act
        string convertedValue = email;

        // Assert
        convertedValue.ShouldBe(emailValue);
    }

    [Fact]
    public void ImplicitConversion_StringToEmailAndEmailToString_ShouldWork()
    {
        // Arrange
        string emailStr = "test@email.com";

        // Act
        Email emailObj = emailStr;
        string resultStr = emailObj;

        // Assert
        emailObj.Value.ShouldBe(emailStr.ToLowerInvariant());
        resultStr.ShouldBe(emailStr.ToLowerInvariant());
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameEmailValue_ShouldBeEqual()
    {
        // Arrange
        var emailValue = "equality@test.com";
        var email1 = Email.Create(emailValue).Value;
        var email2 = Email.Create(emailValue).Value;

        // Act & Assert
        email1.ShouldBe(email2);
        email1.Equals(email2).ShouldBeTrue();
        (email1 == email2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentEmailValues_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;

        // Act & Assert
        email1.ShouldNotBe(email2);
        email1.Equals(email2).ShouldBeFalse();
        (email1 == email2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithCaseDifference_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("Test@Example.Com").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Act & Assert
        email1.ShouldBe(email2); // Should be equal because emails are normalized to lowercase
    }

    #endregion

    #region Static Error Values Tests

    [Fact]
    public void StaticErrorValues_ShouldHaveCorrectProperties()
    {
        // Assert
        Email.Required.Identifier.ShouldBe("Email.Required");
        Email.Required.ErrorMessage.ShouldBe("Email is required.");

        Email.Invalid.Identifier.ShouldBe("Email.InvalidFormat");
        Email.Invalid.ErrorMessage.ShouldBe("Invalid email format.");

        Email.MaximumLength.Identifier.ShouldBe("Email.MaximumLength");
        Email.MaximumLength.ErrorMessage.ShouldBe("Email cannot exceed 200 characters.");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Create_WithMaximumValidLength_ShouldSucceed()
    {
        // Arrange - Create email with exactly 200 characters
        var localPart = new string('a', 188); // 188 + '@' + 'example.com' (12) = 200
        var maxLengthEmail = $"{localPart}@example.com";

        // Act
        var result = Email.Create(maxLengthEmail);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.Length.ShouldBe(200);
    }

    [Theory, AutoFakeItEasyData]
    public void Create_WithAutoGeneratedValidEmails_ShouldSucceed(int number)
    {
        // Arrange
        var email = $"auto{Math.Abs(number)}@test.com";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(email);
    }

    [Theory]
    [InlineData("valid@email.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryValidateValue_ShouldReturnExpectedResult(string? value, bool expected)
    {
        // Act
        var result = Email.TryValidateValue(value, out var errors);

        // Assert
        result.ShouldBe(expected);
        if (!expected)
            errors.ShouldNotBeEmpty();
        else
            errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("valid@email.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldReturnExpectedResult(string? value, bool expected)
    {
        // Arrange
        var email = value != null ? Email.Create(value).Value : null;

        // Act
        var result = Email.IsValid(email);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}