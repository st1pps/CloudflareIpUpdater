using System.ComponentModel.DataAnnotations;
using Stipps.CloudflareApi.Configuration;

namespace Stipps.CloudflareApi.UnitTests;

public class SettingsValidationTests
{
    [Theory]
    [InlineData("12345", "test@mymail.de")]
    public void ValidCombinations(string apiKey, string email)
    {
        // Arrange
        var sut = new CloudflareConnectionSettings
        {
            ApiToken = apiKey,
            Email = email
        };

        // Act and Assert
        sut.Invoking(CloudflareConnectionSettings.Validate).Should().NotThrow();
    }

    [Fact]
    public void SettingIsNull()
    {
        CloudflareConnectionSettings sut = new();
        sut.Invoking(_ => CloudflareConnectionSettings.Validate(null))
            .Should().Throw<ArgumentNullException>();
    }
    
    [Theory]
    [InlineData("", "test@mymail.de")]
    [InlineData(null, "test@mymail.de")]
    [InlineData("1234", null)]
    [InlineData("1234", "")]
    [InlineData(null, null)]
    public void InvalidCombinations(string apiKey, string email)
    {
        // Arrange
        var sut = new CloudflareConnectionSettings
        {
            ApiToken = apiKey,
            Email = email
        };
        
        // Act and Assert
        sut.Invoking(CloudflareConnectionSettings.Validate).Should().Throw<ValidationException>();
    }
}