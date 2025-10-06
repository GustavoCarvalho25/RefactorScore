using FluentAssertions;
using RefactorScore.Domain.ValueObjects;
using RefactorScore.Domain.Tests.Builders;
using Xunit;

namespace RefactorScore.Domain.Tests.ValueObjects;

public class CleanCodeRatingTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange & Act
        var rating = new CleanCodeRating(8, 7, 9, 6, 5);

        // Assert
        rating.VariableNaming.Should().Be(8);
        rating.FunctionSizes.Should().Be(7);
        rating.NoNeedsComments.Should().Be(9);
        rating.MethodCohesion.Should().Be(6);
        rating.DeadCode.Should().Be(5);
        rating.Justifications.Should().NotBeNull();
        rating.Justifications.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_Withjustifications_ShouldCreateInstanceWithjustifications()
    {
        // Arrange
        var justifications = new Dictionary<string, string>
        {
            { "VariableNaming", "Good variable names used" },
            { "FunctionSizes", "Functions are appropriately sized" }
        };

        // Act
        var rating = new CleanCodeRating(8, 7, 9, 6, 5, justifications);

        // Assert
        rating.Justifications.Should().HaveCount(2);
        rating.Justifications["VariableNaming"].Should().Be("Good variable names used");
        rating.Justifications["FunctionSizes"].Should().Be("Functions are appropriately sized");
    }

    [Theory]
    [InlineData(0, 5, 5, 5, 5, "variableNaming")]
    [InlineData(5, 0, 5, 5, 5, "functionSizes")]
    [InlineData(5, 5, 0, 5, 5, "noNeedsComments")]
    [InlineData(5, 5, 5, 0, 5, "methodCohesion")]
    [InlineData(5, 5, 5, 5, 0, "deadCode")]
    public void Constructor_WithZeroValues_ShouldThrowArgumentException(
        int variableNaming, int functionSizes, int noNeedsComments, 
        int methodCohesion, int deadCode, string expectedParamName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new CleanCodeRating(variableNaming, functionSizes, noNeedsComments, methodCohesion, deadCode));
        
        exception.ParamName.Should().Be(expectedParamName);
    }

    [Theory]
    [InlineData(-1, 5, 5, 5, 5, "variableNaming")]
    [InlineData(5, -1, 5, 5, 5, "functionSizes")]
    [InlineData(5, 5, -1, 5, 5, "noNeedsComments")]
    [InlineData(5, 5, 5, -1, 5, "methodCohesion")]
    [InlineData(5, 5, 5, 5, -1, "deadCode")]
    public void Constructor_WithNegativeValues_ShouldThrowArgumentException(
        int variableNaming, int functionSizes, int noNeedsComments, 
        int methodCohesion, int deadCode, string expectedParamName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new CleanCodeRating(variableNaming, functionSizes, noNeedsComments, methodCohesion, deadCode));
        
        exception.ParamName.Should().Be(expectedParamName);
    }

    [Theory]
    [InlineData(11, 5, 5, 5, 5, "variableNaming")]
    [InlineData(5, 11, 5, 5, 5, "functionSizes")]
    [InlineData(5, 5, 11, 5, 5, "noNeedsComments")]
    [InlineData(5, 5, 5, 11, 5, "methodCohesion")]
    [InlineData(5, 5, 5, 5, 11, "deadCode")]
    public void Constructor_WithValuesAboveRange_ShouldThrowArgumentOutOfRangeException(
        int variableNaming, int functionSizes, int noNeedsComments, 
        int methodCohesion, int deadCode, string expectedParamName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            new CleanCodeRating(variableNaming, functionSizes, noNeedsComments, methodCohesion, deadCode));
        
        exception.ParamName.Should().Be(expectedParamName);
    }

    [Theory]
    [InlineData(10, 10, 10, 10, 10, 10.0)] // Perfect score
    [InlineData(8, 7, 9, 6, 5, 7.0)]       // Mixed scores
    [InlineData(1, 1, 1, 1, 1, 1.0)]       // Minimum scores
    [InlineData(5, 5, 5, 5, 5, 5.0)]       // Average scores
    public void Note_ShouldCalculateCorrectAverage(
        int variableNaming, int functionSizes, int noNeedsComments, 
        int methodCohesion, int deadCode, double expectedNote)
    {
        // Arrange & Act
        var rating = new CleanCodeRating(variableNaming, functionSizes, noNeedsComments, methodCohesion, deadCode);

        // Assert
        rating.Note.Should().Be(expectedNote);
    }

    [Theory]
    [InlineData(10, 10, 10, 10, 10, "Excellent")]     // 10.0
    [InlineData(9, 9, 9, 9, 9, "Excellent")]          // 9.0
    [InlineData(8, 8, 8, 8, 7, "VeryGood")]           // 7.8
    [InlineData(7, 7, 7, 7, 7, "Good")]               // 7.0 
    [InlineData(6, 6, 6, 6, 6, "Good")]               // 6.0
    [InlineData(5, 5, 5, 5, 5, "Acceptable")]         // 5.0
    [InlineData(4, 4, 4, 4, 2, "NeedsImprovement")]   // 3.6
    [InlineData(3, 3, 3, 3, 3, "Problematic")]        // 3.0
    [InlineData(1, 1, 1, 1, 1, "Problematic")]        // 1.0
    public void Quality_ShouldReturnCorrectQualityRating(
        int variableNaming, int functionSizes, int noNeedsComments, 
        int methodCohesion, int deadCode, string expectedQuality)
    {
        // Arrange & Act
        var rating = new CleanCodeRating(variableNaming, functionSizes, noNeedsComments, methodCohesion, deadCode);

        // Assert
        rating.Quality.Should().Be(expectedQuality);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var justifications = new Dictionary<string, string>
        {
            { "VariableNaming", "Good naming" },
            { "FunctionSizes", "Appropriate sizes" }
        };

        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5, justifications);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 5, justifications);

        // Act & Assert
        rating1.Should().Be(rating2);
        rating1.Equals(rating2).Should().BeTrue();
        (rating1 == rating2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameValuesButDifferentJustificationsOrder_ShouldReturnTrue()
    {
        // Arrange
        var justifications1 = new Dictionary<string, string>
        {
            { "VariableNaming", "Good naming" },
            { "FunctionSizes", "Appropriate sizes" }
        };

        var justifications2 = new Dictionary<string, string>
        {
            { "FunctionSizes", "Appropriate sizes" },
            { "VariableNaming", "Good naming" }
        };

        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5, justifications1);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 5, justifications2);

        // Act & Assert
        rating1.Should().Be(rating2);
        rating1.Equals(rating2).Should().BeTrue();
        (rating1 == rating2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 4); // Different DeadCode

        // Act & Assert
        rating1.Should().NotBe(rating2);
        rating1.Equals(rating2).Should().BeFalse();
        (rating1 == rating2).Should().BeFalse();
        (rating1 != rating2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentJustifications_ShouldReturnFalse()
    {
        // Arrange
        var justifications1 = new Dictionary<string, string>
        {
            { "VariableNaming", "Good naming" }
        };

        var justifications2 = new Dictionary<string, string>
        {
            { "VariableNaming", "Different naming" }
        };

        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5, justifications1);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 5, justifications2);

        // Act & Assert
        rating1.Should().NotBe(rating2);
        rating1.Equals(rating2).Should().BeFalse();
        (rating1 == rating2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var justifications = new Dictionary<string, string>
        {
            { "VariableNaming", "Good naming" }
        };

        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5, justifications);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 5, justifications);

        // Act & Assert
        rating1.GetHashCode().Should().Be(rating2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var rating1 = new CleanCodeRating(8, 7, 9, 6, 5);
        var rating2 = new CleanCodeRating(8, 7, 9, 6, 4);

        // Act & Assert
        rating1.GetHashCode().Should().NotBe(rating2.GetHashCode());
    }

    [Fact]
    public void Constructor_WithNullJustifications_ShouldCreateEmptyDictionary()
    {
        // Arrange & Act
        var rating = new CleanCodeRating(8, 7, 9, 6, 5, null);

        // Assert
        rating.Justifications.Should().NotBeNull();
        rating.Justifications.Should().BeEmpty();
    }

    [Fact]
    public void UsingBuilder_ShouldCreateValidInstance()
    {
        // Arrange & Act
        var rating = new CleanCodeRatingBuilder()
            .WithVariableNaming(8)
            .WithFunctionSizes(7)
            .WithNoNeedsComments(9)
            .WithMethodCohesion(6)
            .WithDeadCode(5)
            .WithJustifications(new Dictionary<string, string> { { "Test", "Value" } })
            .Build();

        // Assert
        rating.VariableNaming.Should().Be(8);
        rating.FunctionSizes.Should().Be(7);
        rating.NoNeedsComments.Should().Be(9);
        rating.MethodCohesion.Should().Be(6);
        rating.DeadCode.Should().Be(5);
        rating.Justifications.Should().ContainKey("Test");
        rating.Note.Should().Be(7.0);
        rating.Quality.Should().Be("Good");
    }

    [Fact]
    public void QualityBoundaryTests_ShouldReturnCorrectQuality()
    {
        // Test boundary conditions for Quality property
        
        // Excellent boundary (>= 9.0)
        var excellentRating = new CleanCodeRating(9, 9, 9, 9, 9); // 9.0 
        excellentRating.Quality.Should().Be("Excellent");
        
        // VeryGood boundary (>= 7.5, < 9.0)
        var veryGoodRating = new CleanCodeRating(8, 8, 7, 7, 7); // 7.4
        veryGoodRating.Quality.Should().Be("Good");
        
        var veryGoodBoundaryRating = new CleanCodeRating(8, 8, 8, 7, 7); // 7.6
        veryGoodBoundaryRating.Quality.Should().Be("VeryGood");
        
        // Good boundary (>= 6.0, < 7.5)
        var goodRating = new CleanCodeRating(6, 6, 6, 6, 6); // 6.0 
        goodRating.Quality.Should().Be("Good");
        
        // Acceptable boundary (>= 5.0, < 6.0)
        var acceptableRating = new CleanCodeRating(5, 5, 5, 5, 5); // 5.0 
        acceptableRating.Quality.Should().Be("Acceptable");
        
        // NeedsImprovement boundary (>= 3.5, < 5.0)
        var needsImprovementRating = new CleanCodeRating(4, 4, 3, 3, 3); // 3.4
        needsImprovementRating.Quality.Should().Be("Problematic");
        
        var needsImprovementBoundaryRating = new CleanCodeRating(4, 4, 4, 3, 3); // 3.6
        needsImprovementBoundaryRating.Quality.Should().Be("NeedsImprovement");
    }
}
