namespace Stipps.CloudflareApi.UnitTests;

public class SnakeCaseTests
{
    [Fact]
    public void SimpleStringCanBeConvertedToSnakeCase()
    {
        // Arrange
        const string input = "Id";
        const string expectedOutput = "id";

        // Act
        var result = input.ToSnakeCase();
        
        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }
    
    [Fact]
    public void MultiWordStringCanBeConvertedToSnakeCase()
    {
        // Arrange
        const string input = "AutoAdded";
        const string expectedOutput = "auto_added";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }
    
    [Fact]
    public void TripleWordStringCanBeConvertedToSnakeCase()
    {
        // Arrange
        const string input = "ExpectedDeliveryDate";
        const string expectedOutput = "expected_delivery_date";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().BeEquivalentTo(expectedOutput);
    }

    [Fact]
    public void NullStringDoesNotCauseAnException()
    {
        // Arrange
        const string input = null!;

        // Act
        var act = () => input!.ToSnakeCase();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void EmptyStringDoesNotCauseAnException()
    {
        // Arrange
        const string input = "";
        
        // Act
        var act = () => input.ToSnakeCase();

        // Assert
        act.Should().NotThrow();
    }
    
    
}