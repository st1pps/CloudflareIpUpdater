using Stipps.CloudflareApi.Serialization;

namespace Stipps.CloudflareApi.UnitTests;

public class SnakeCaseNamingPolicyTest
{
    private readonly SnakeCaseNamingPolicy _sut;
    
    public SnakeCaseNamingPolicyTest()
    {
        _sut = new SnakeCaseNamingPolicy();
    }

    [Fact]
    public void NullStringDoesNotCauseException()
    {
        // Arrange
        string input = null!;
        
        // Act
        var act = () => input.ToSnakeCase();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void EmptyStringDoesNotCauseException()
    {
        // Arrange
        var input = string.Empty;
        
        // Act
        var act = () => input.ToSnakeCase();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData("Id", "id")]
    [InlineData("Forename", "forename")]
    [InlineData("ZoneName", "zone_name")]
    [InlineData("AutoAdded", "auto_added")]
    [InlineData("MultiWordString", "multi_word_string")]
    public void InputStringIsReturnedInSnakeCase(string input, string expectedOutput)
    {
        // Act
        var result = _sut.ConvertName(input);
        
        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }
}