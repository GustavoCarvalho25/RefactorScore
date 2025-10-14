using FluentAssertions;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.Tests.Builders;
using RefactorScore.Domain.ValueObjects;
using Xunit;

namespace RefactorScore.Domain.Tests.Entities;

public class CommitFileTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateCommitFile()
    {
        // Arrange
        var path = "src/Calculator.cs";
        var addedLines = 50;
        var removedLines = 10;
        var language = "C#";
        var content = "public class Calculator { }";

        // Act
        var file = new CommitFile(path, addedLines, removedLines, language, content);

        // Assert
        file.Path.Should().Be(path);
        file.AddedLines.Should().Be(addedLines);
        file.RemovedLines.Should().Be(removedLines);
        file.Language.Should().Be(language);
        file.Content.Should().Be(content);
        file.HasAnalysis.Should().BeFalse();
        file.Rating.Should().BeNull();
        file.Suggestions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidPath_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange & Act & Assert
        var action = () => new CommitFile(
            invalidPath, 50, 10, "C#", "public class Test { }");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*path*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidLanguage_ShouldThrowArgumentException(string invalidLanguage)
    {
        // Arrange & Act & Assert
        var action = () => new CommitFile(
            "src/Test.cs", 50, 10, invalidLanguage, "public class Test { }");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*language*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidContent_ShouldThrowArgumentException(string invalidContent)
    {
        // Arrange & Act & Assert
        var action = () => new CommitFile(
            "src/Test.cs", 50, 10, "C#", invalidContent);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*content*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeAddedLines_ShouldThrowArgumentException(int negativeLines)
    {
        // Arrange & Act & Assert
        var action = () => new CommitFile(
            "src/Test.cs", negativeLines, 10, "C#", "public class Test { }");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*addedLines*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Constructor_WithNegativeRemovedLines_ShouldThrowArgumentException(int negativeLines)
    {
        // Arrange & Act & Assert
        var action = () => new CommitFile(
            "src/Test.cs", 50, negativeLines, "C#", "public class Test { }");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*removedLines*");
    }

    [Fact]
    public void Constructor_WithZeroLines_ShouldCreateValidFile()
    {
        // Arrange & Act
        var file = new CommitFile(
            "src/Test.cs", 0, 0, "C#", "public class Test { }");

        // Assert
        file.AddedLines.Should().Be(0);
        file.RemovedLines.Should().Be(0);
        file.Should().NotBeNull();
    }

    [Fact]
    public void SetAnalysis_WithValidData_ShouldSetAnalysisAndSuggestions()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        var rating = CleanCodeRatingBuilder.Create()
            .WithExcellentScores()
            .Build();

        var suggestions = new List<Suggestion>
        {
            SuggestionBuilder.Create()
                .WithRandomData()
                .WithTitle("Improve variable naming")
                .Build(),
            SuggestionBuilder.Create()
                .WithRandomData()
                .WithTitle("Extract method")
                .Build()
        };

        // Act
        file.SetAnalysis(rating, suggestions);

        // Assert
        file.HasAnalysis.Should().BeTrue();
        file.Rating.Should().Be(rating);
        file.Suggestions.Should().HaveCount(2);
        file.Suggestions.Should().BeEquivalentTo(suggestions);
    }

    [Fact]
    public void SetAnalysis_WithNullRating_ShouldThrowArgumentException()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        var suggestions = new List<Suggestion>
        {
            SuggestionBuilder.Create().WithRandomData().Build()
        };

        // Act & Assert
        var action = () => file.SetAnalysis(null!, suggestions);
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("*rating*");
    }

    [Fact]
    public void SetAnalysis_WithNullSuggestions_ShouldThrowArgumentException()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        var rating = CleanCodeRatingBuilder.Create()
            .WithRandomData()
            .Build();

        // Act & Assert
        var action = () => file.SetAnalysis(rating, null!);
        
        action.Should().Throw<ArgumentException>()
            .WithMessage("*suggestions*");
    }

    [Fact]
    public void SetAnalysis_WithEmptySuggestions_ShouldSetAnalysisWithNoSuggestions()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        var rating = CleanCodeRatingBuilder.Create()
            .WithRandomData()
            .Build();

        var emptySuggestions = new List<Suggestion>();

        // Act
        file.SetAnalysis(rating, emptySuggestions);

        // Assert
        file.HasAnalysis.Should().BeTrue();
        file.Rating.Should().Be(rating);
        file.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public void SetAnalysis_CalledMultipleTimes_ShouldReplaceExistingAnalysis()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        var firstRating = CleanCodeRatingBuilder.Create()
            .WithPoorScores()
            .Build();

        var firstSuggestions = new List<Suggestion>
        {
            SuggestionBuilder.Create()
                .WithTitle("First suggestion")
                .WithRandomData()
                .Build()
        };

        var secondRating = CleanCodeRatingBuilder.Create()
            .WithExcellentScores()
            .Build();

        var secondSuggestions = new List<Suggestion>
        {
            SuggestionBuilder.Create()
                .WithTitle("Second suggestion")
                .WithRandomData()
                .Build(),
            SuggestionBuilder.Create()
                .WithTitle("Third suggestion")
                .WithRandomData()
                .Build()
        };

        // Act
        file.SetAnalysis(firstRating, firstSuggestions);
        file.SetAnalysis(secondRating, secondSuggestions);

        // Assert
        file.Rating.Should().Be(secondRating);
        file.Suggestions.Should().HaveCount(2);
        file.Suggestions.Should().BeEquivalentTo(secondSuggestions);
        file.Suggestions.Should().NotContain(s => s.Title == "First suggestion");
    }

    [Fact]
    public void HasAnalysis_WithoutAnalysis_ShouldReturnFalse()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        // Act & Assert
        file.HasAnalysis.Should().BeFalse();
    }

    [Fact]
    public void HasAnalysis_WithAnalysis_ShouldReturnTrue()
    {
        // Arrange
        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .WithRandomRating()
            .Build();

        // Act & Assert
        file.HasAnalysis.Should().BeTrue();
    }

    [Theory]
    [InlineData("Calculator.cs", "C#")]
    [InlineData("UserService.java", "Java")]
    [InlineData("utils.js", "JavaScript")]
    [InlineData("main.py", "Python")]
    [InlineData("Component.tsx", "TypeScript")]
    public void Constructor_WithDifferentLanguages_ShouldCreateValidFile(string fileName, string language)
    {
        // Arrange & Act
        var file = new CommitFile(
            $"src/{fileName}", 25, 5, language, "// Sample content");

        // Assert
        file.Path.Should().Be($"src/{fileName}");
        file.Language.Should().Be(language);
        file.Content.Should().Be("// Sample content");
    }

    [Fact]
    public void Constructor_WithLargeLineNumbers_ShouldCreateValidFile()
    {
        // Arrange
        var largeAddedLines = 10000;
        var largeRemovedLines = 5000;

        // Act
        var file = new CommitFile(
            "src/LargeFile.cs", largeAddedLines, largeRemovedLines, "C#", "// Large file content");

        // Assert
        file.AddedLines.Should().Be(largeAddedLines);
        file.RemovedLines.Should().Be(largeRemovedLines);
    }
}