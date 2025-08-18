namespace TC.CloudGames.Users.Unit.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for Role value object
/// </summary>
public class RoleTests
{
    #region Valid Role Tests

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public void Create_WithValidRole_ShouldSucceed(string validRole)
    {
        // Act
        var result = Role.Create(validRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe(validRole);
    }

    [Theory]
    [InlineData("user", "User")]
    [InlineData("admin", "Admin")]
    [InlineData("moderator", "Moderator")]
    [InlineData("USER", "User")]
    [InlineData("ADMIN", "Admin")]
    [InlineData("MODERATOR", "Moderator")]
    [InlineData("UsEr", "User")]
    [InlineData("AdMiN", "Admin")]
    [InlineData("MoDeRaToR", "Moderator")]
    public void Create_WithValidRoleCaseInsensitive_ShouldNormalizeCase(string input, string expected)
    {
        // Act
        var result = Role.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    #endregion

    #region Invalid Role Tests

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperUser")]
    [InlineData("Guest")]
    [InlineData("Owner")]
    [InlineData("SuperAdmin")]
    [InlineData("123")]
    [InlineData("User123")]
    [InlineData("Admin!")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidRole_ShouldFail(string invalidRole)
    {
        // Act
        var result = Role.Create(invalidRole);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
        result.ValidationErrors.ShouldContain(e => e.ErrorMessage == "Invalid role value.");
    }

    #endregion

    #region Predefined Roles Tests

    [Fact]
    public void PredefinedRoles_ShouldHaveCorrectValues()
    {
        // Assert
        Role.User.Value.ShouldBe("User");
        Role.Admin.Value.ShouldBe("Admin");
        Role.Moderator.Value.ShouldBe("Moderator");
    }

    [Fact]
    public void PredefinedRoles_ShouldBeUsableDirectly()
    {
        // Act & Assert
        Role.User.ShouldNotBeNull();
        Role.Admin.ShouldNotBeNull();
        Role.Moderator.ShouldNotBeNull();

        Role.User.Value.ShouldBe("User");
        Role.Admin.Value.ShouldBe("Admin");
        Role.Moderator.Value.ShouldBe("Moderator");
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void IsAdmin_WithAdminRole_ShouldReturnTrue()
    {
        // Arrange
        var adminRole = Role.Create("Admin").Value;

        // Act
        var result = adminRole.IsAdmin();

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Moderator")]
    public void IsAdmin_WithNonAdminRole_ShouldReturnFalse(string nonAdminRole)
    {
        // Arrange
        var role = Role.Create(nonAdminRole).Value;

        // Act
        var result = role.IsAdmin();

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    public void CanModerate_WithModeratorOrAdminRole_ShouldReturnTrue(string moderatingRole)
    {
        // Arrange
        var role = Role.Create(moderatingRole).Value;

        // Act
        var result = role.CanModerate();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanModerate_WithUserRole_ShouldReturnFalse()
    {
        // Arrange
        var userRole = Role.Create("User").Value;

        // Act
        var result = userRole.CanModerate();

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("admin", true)]  // Case insensitive
    [InlineData("ADMIN", true)]
    [InlineData("Admin", true)]
    [InlineData("moderator", false)]
    [InlineData("user", false)]
    public void IsAdmin_IsCaseInsensitive_ShouldWorkCorrectly(string roleInput, bool expectedIsAdmin)
    {
        // Arrange
        var role = Role.Create(roleInput).Value;

        // Act
        var result = role.IsAdmin();

        // Assert
        result.ShouldBe(expectedIsAdmin);
    }

    [Theory]
    [InlineData("admin", true)]
    [InlineData("moderator", true)]
    [InlineData("ADMIN", true)]
    [InlineData("MODERATOR", true)]
    [InlineData("user", false)]
    [InlineData("USER", false)]
    public void CanModerate_IsCaseInsensitive_ShouldWorkCorrectly(string roleInput, bool expectedCanModerate)
    {
        // Arrange
        var role = Role.Create(roleInput).Value;

        // Act
        var result = role.CanModerate();

        // Assert
        result.ShouldBe(expectedCanModerate);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var role = Role.Create("Admin").Value;

        // Act
        string convertedValue = role;

        // Assert
        convertedValue.ShouldBe("Admin");
    }

    [Fact]
    public void ImplicitConversion_WithPredefinedRoles_ShouldWork()
    {
        // Act
        string userRoleValue = Role.User;
        string adminRoleValue = Role.Admin;
        string moderatorRoleValue = Role.Moderator;

        // Assert
        userRoleValue.ShouldBe("User");
        adminRoleValue.ShouldBe("Admin");
        moderatorRoleValue.ShouldBe("Moderator");
    }

    [Fact]
    public void ImplicitConversion_StringToRoleAndRoleToString_ShouldWork()
    {
        // Arrange
        string roleStr = "Admin";

        // Act
        var roleObj = Role.Create(roleStr).Value;
        string resultStr = roleObj;

        // Assert
        roleObj.Value.ShouldBe("Admin");
        resultStr.ShouldBe("Admin");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameRoleValue_ShouldBeEqual()
    {
        // Arrange
        var role1 = Role.Create("Admin").Value;
        var role2 = Role.Create("Admin").Value;

        // Act & Assert
        role1.ShouldBe(role2);
        role1.Equals(role2).ShouldBeTrue();
        (role1 == role2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentRoleValues_ShouldNotBeEqual()
    {
        // Arrange
        var userRole = Role.Create("User").Value;
        var adminRole = Role.Create("Admin").Value;

        // Act & Assert
        userRole.ShouldNotBe(adminRole);
        userRole.Equals(adminRole).ShouldBeFalse();
        (userRole == adminRole).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithCaseDifference_ShouldBeEqual()
    {
        // Arrange
        var role1 = Role.Create("Admin").Value;
        var role2 = Role.Create("admin").Value;

        // Act & Assert
        role1.ShouldBe(role2); // Should be equal because roles are normalized
        role1.Value.ShouldBe(role2.Value); // Both should be "Admin"
    }

    [Fact]
    public void Equals_WithPredefinedRoles_ShouldBeEqual()
    {
        // Arrange
        var createdUserRole = Role.Create("User").Value;
        var predefinedUserRole = Role.User;

        // Act & Assert
        createdUserRole.ShouldBe(predefinedUserRole);
    }

    #endregion

    #region Static Error Values Tests

    [Fact]
    public void StaticErrorValue_ShouldHaveCorrectProperties()
    {
        // Assert
        Role.Invalid.Identifier.ShouldBe("Role.Invalid");
        Role.Invalid.ErrorMessage.ShouldBe("Invalid role value.");
    }

    #endregion

    #region Edge Cases Tests

    [Theory, AutoFakeItEasyValidUserData]
    public void Create_WithRandomValidRoles_ShouldSucceed(int selector)
    {
        // Arrange
        var validRoles = new[] { "User", "Admin", "Moderator" };
        var selectedRole = validRoles[Math.Abs(selector) % validRoles.Length];

        // Act
        var result = Role.Create(selectedRole);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(selectedRole);
    }

    [Fact]
    public void Create_WithWhitespaceAroundValidRole_ShouldFail()
    {
        // Arrange
        var roleWithWhitespace = " Admin ";

        // Act
        var result = Role.Create(roleWithWhitespace);

        // Assert
        // This should fail because the exact match logic doesn't trim whitespace
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    [Fact]
    public void BusinessLogic_AdminRole_ShouldHaveBothAdminAndModeratorPrivileges()
    {
        // Arrange
        var adminRole = Role.Create("Admin").Value;

        // Act & Assert
        adminRole.IsAdmin().ShouldBeTrue();
        adminRole.CanModerate().ShouldBeTrue(); // Admin can also moderate
    }

    [Fact]
    public void BusinessLogic_ModeratorRole_ShouldHaveModeratorButNotAdminPrivileges()
    {
        // Arrange
        var moderatorRole = Role.Create("Moderator").Value;

        // Act & Assert
        moderatorRole.IsAdmin().ShouldBeFalse();
        moderatorRole.CanModerate().ShouldBeTrue();
    }

    [Fact]
    public void BusinessLogic_UserRole_ShouldHaveNoSpecialPrivileges()
    {
        // Arrange
        var userRole = Role.Create("User").Value;

        // Act & Assert
        userRole.IsAdmin().ShouldBeFalse();
        userRole.CanModerate().ShouldBeFalse();
    }

    #endregion

    #region Additional Validation Tests

    [Theory]
    [InlineData("User", true)]
    [InlineData("Admin", true)]
    [InlineData("Moderator", true)]
    [InlineData("InvalidRole", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryValidateValue_ShouldReturnExpectedResult(string? value, bool expected)
    {
        // Act
        var result = Role.TryValidateValue(value, out var errors);

        // Assert
        result.ShouldBe(expected);
        if (!expected)
            errors.ShouldNotBeEmpty();
        else
            errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("User", true)]
    [InlineData("Admin", true)]
    [InlineData("Moderator", true)]
    [InlineData("InvalidRole", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldReturnExpectedResult(string? value, bool expected)
    {
        // Arrange
        var role = value != null ? Role.Create(value).Value : null;

        // Act
        var result = Role.IsValid(role);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void Create_WithNull_ShouldReturnInvalidResult()
    {
        // Act
        var result = Role.Create(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    [Fact]
    public void FromDb_WithNull_ShouldReturnInvalidResult()
    {
        // Act
        var result = Role.FromDb(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ValidationErrors.ShouldContain(e => e.Identifier == "Role.Invalid");
    }

    #endregion
}