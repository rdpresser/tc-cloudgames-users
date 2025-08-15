using TC.CloudGames.Users.Unit.Tests.Common;

namespace TC.CloudGames.Users.Unit.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for Password value object
/// </summary>
public class PasswordTests
{
    #region Valid Password Tests

    [Theory]
    [InlineData("ValidPass123!")]
    [InlineData("MyPassword1@")]
    [InlineData("StrongP@ssw0rd")]
    [InlineData("Complex1ty#")]
    [InlineData("Test1234$")]
    [InlineData("Minimum8!")]
    public void Create_WithValidPassword_ShouldSucceed(string validPassword)
    {
        // Act
        var result = Password.Create(validPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Hash.ShouldNotBeNullOrEmpty();
        result.Value.Hash.ShouldNotBe(validPassword); // Should be hashed, not plain text
        result.Value.Verify(validPassword).ShouldBeTrue(); // Should verify against original password
    }

    [Fact]
    public void Create_WithValidPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "SamePassword123!";

        // Act
        var result1 = Password.Create(password);
        var result2 = Password.Create(password);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        result1.Value.Hash.ShouldNotBe(result2.Value.Hash); // BCrypt generates different salts
        result1.Value.Verify(password).ShouldBeTrue();
        result2.Value.Verify(password).ShouldBeTrue();
    }

    #endregion

    #region Invalid Password Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ShouldFail(string invalidPassword)
    {
        // Act
        var result = Password.Create(invalidPassword);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.Required");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Password is required.");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    [InlineData("Test123")]
    public void Create_WithPasswordTooShort_ShouldFail(string shortPassword)
    {
        // Act
        var result = Password.Create(shortPassword);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.TooShort");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Password must be at least 8 characters.");
    }

    [Fact]
    public void Create_WithPasswordTooLong_ShouldFail()
    {
        // Arrange - Create password longer than 128 characters
        var longPassword = new string('a', 129) + "A1!"; // 132 characters total

        // Act
        var result = Password.Create(longPassword);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.TooLong");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Password cannot exceed 128 characters.");
    }

    [Theory]
    [InlineData("alllowercase123!")] // No uppercase
    [InlineData("ALLUPPERCASE123!")] // No lowercase
    [InlineData("NoNumbersHere!")] // No numbers
    [InlineData("NoSpecialChars123")] // No special characters
    [InlineData("OnlyLetters")] // Only letters
    [InlineData("12345678")] // Only numbers
    [InlineData("!@#$%^&*")] // Only special characters
    public void Create_WithWeakPassword_ShouldFail(string weakPassword)
    {
        // Act
        var result = Password.Create(weakPassword);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.Weak");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Password must contain at least one uppercase, lowercase, number and special character.");
    }

    #endregion

    #region FromHash Tests

    [Fact]
    public void FromHash_WithValidHash_ShouldSucceed()
    {
        // Arrange
        var originalPassword = "TestPassword123!";
        var password = Password.Create(originalPassword).Value;
        var hash = password.Hash;

        // Act
        var result = Password.FromHash(hash);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Hash.ShouldBe(hash);
        result.Value.Verify(originalPassword).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromHash_WithNullOrWhitespaceHash_ShouldFail(string invalidHash)
    {
        // Act
        var result = Password.FromHash(invalidHash);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Password.Required");
    }

    #endregion

    #region Verify Tests

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var plainPassword = "CorrectPassword123!";
        var password = Password.Create(plainPassword).Value;

        // Act
        var result = password.Verify(plainPassword);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var plainPassword = "CorrectPassword123!";
        var incorrectPassword = "WrongPassword123!";
        var password = Password.Create(plainPassword).Value;

        // Act
        var result = password.Verify(incorrectPassword);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    public void Verify_WithNullOrEmptyPassword_ShouldReturnFalse(string invalidPassword)
    {
        // Arrange
        var password = Password.Create("ValidPassword123!").Value;

        // Act
        var result = password.Verify(invalidPassword);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Verify_IsCaseSensitive_ShouldReturnFalseForWrongCase()
    {
        // Arrange
        var plainPassword = "CaseSensitive123!";
        var wrongCasePassword = "casesensitive123!";
        var password = Password.Create(plainPassword).Value;

        // Act
        var result = password.Verify(wrongCasePassword);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnHash()
    {
        // Arrange
        var password = Password.Create("TestPassword123!").Value;

        // Act
        string convertedValue = password;

        // Assert
        convertedValue.ShouldBe(password.Hash);
        convertedValue.ShouldNotBe("TestPassword123!"); // Should not return plain text
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameHash_ShouldBeEqual()
    {
        // Arrange
        var plainPassword = "EqualityTest123!";
        var password1 = Password.Create(plainPassword).Value;
        var password2 = Password.FromHash(password1.Hash).Value;

        // Act & Assert
        password1.ShouldBe(password2);
        password1.Equals(password2).ShouldBeTrue();
        (password1 == password2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentHashes_ShouldNotBeEqual()
    {
        // Arrange
        var password1 = Password.Create("Password1123!").Value;
        var password2 = Password.Create("Password2123!").Value;

        // Act & Assert
        password1.ShouldNotBe(password2);
        password1.Equals(password2).ShouldBeFalse();
        (password1 == password2).ShouldBeFalse();
    }

    #endregion

    #region Static Error Values Tests

    [Fact]
    public void StaticErrorValues_ShouldHaveCorrectProperties()
    {
        // Assert
        Password.Required.Identifier.ShouldBe("Password.Required");
        Password.Required.ErrorMessage.ShouldBe("Password is required.");

        Password.TooShort.Identifier.ShouldBe("Password.TooShort");
        Password.TooShort.ErrorMessage.ShouldBe("Password must be at least 8 characters.");

        Password.TooLong.Identifier.ShouldBe("Password.TooLong");
        Password.TooLong.ErrorMessage.ShouldBe("Password cannot exceed 128 characters.");

        Password.WeakPassword.Identifier.ShouldBe("Password.Weak");
        Password.WeakPassword.ErrorMessage.ShouldBe("Password must contain at least one uppercase, lowercase, number and special character.");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Create_WithExactly8Characters_ShouldSucceed()
    {
        // Arrange
        var minimumPassword = "Test123!"; // Exactly 8 characters

        // Act
        var result = Password.Create(minimumPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Verify(minimumPassword).ShouldBeTrue();
    }

    [Fact]
    public void Create_WithExactly128Characters_ShouldSucceed()
    {
        // Arrange
        var minimumPassword = "Test123!"; // 8 chars
        var padding = new string('a', 117); // 117 chars
        var maximumPassword = minimumPassword + padding + "A1!"; // 128 chars total

        // Act
        var result = Password.Create(maximumPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Verify(maximumPassword).ShouldBeTrue();
        maximumPassword.Length.ShouldBe(128);
    }

    [Theory]
    [InlineData("Test123!@#$%^&*()")]
    [InlineData("Complex1ty_With-Special.Chars")]
    [InlineData("Unicode123!ηρόραινσϊ")]
    public void Create_WithVariousSpecialCharacters_ShouldSucceed(string passwordWithSpecialChars)
    {
        // Act
        var result = Password.Create(passwordWithSpecialChars);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Verify(passwordWithSpecialChars).ShouldBeTrue();
    }

    [Theory, AutoFakeItEasyValidUserData]
    public void Create_WithAutoGeneratedValidPasswords_ShouldSucceed(int number)
    {
        // Arrange
        var password = $"AutoPass{Math.Abs(number)}!";

        // Act
        var result = Password.Create(password);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Verify(password).ShouldBeTrue();
    }

    #endregion

    #region Additional Tests

    [Theory]
    [InlineData("ValidPass123!", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void TryValidateValue_ShouldReturnExpectedResult(string value, bool expected)
    {
        // Act
        var result = Password.TryValidateValue(value, out var errors);

        // Assert
        result.ShouldBe(expected);
        if (!expected)
            errors.ShouldNotBeEmpty();
        else
            errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ValidPass123!", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldReturnExpectedResult(string? value, bool expected)
    {
        // Arrange
        var password = value != null ? Password.Create(value).Value : null;

        // Act
        var result = Password.IsValid(password);

        // Assert
        result.ShouldBe(expected);
    }

    //[Fact]
    //public void ImplicitConversion_StringToPasswordAndPasswordToString_ShouldWork()
    //{
    //    // Arrange
    //    string passwordStr = "ValidPass123!";

    //    // Act
    //    Password passwordObj = passwordStr;
    //    string resultStr = passwordObj;

    //    // Assert
    //    passwordObj.Verify(passwordStr).ShouldBeTrue();
    //    resultStr.ShouldBe(passwordObj.Hash);
    //}

    #endregion
}