using FluentAssertions;
using RefactorScore.Domain.Tests.Builders;
using RefactorScore.Domain.ValueObjects;
using Xunit;

namespace RefactorScore.Domain.Tests.ValueObjects;

public class SuggestionTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateSuggestion()
    {
        // Arrange
        var title = "Improve variable naming";
        var description = "Use more descriptive variable names to improve code readability";
        var priority = "High";
        var type = "Refactoring";
        var difficult = "Medium";
        var fileReference = "src/Calculator.cs";
        var lastUpdate = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Clean Code - Chapter 2", "Refactoring - Martin Fowler" };

        // Act
        var suggestion = new Suggestion(
            title, description, priority, type, difficult, 
            fileReference, lastUpdate, studyResources);

        // Assert
        suggestion.Title.Should().Be(title);
        suggestion.Description.Should().Be(description);
        suggestion.Priority.Should().Be(priority);
        suggestion.Type.Should().Be(type);
        suggestion.Difficult.Should().Be(difficult);
        suggestion.FileReference.Should().Be(fileReference);
        suggestion.LastUpdate.Should().Be(lastUpdate);
        suggestion.StudyResources.Should().BeEquivalentTo(studyResources);
        suggestion.StudyResources.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithNullStudyResources_ShouldCreateEmptyList()
    {
        // Arrange & Act
        var suggestion = new Suggestion(
            "Test Title", "Test Description", "Medium", "CodeStyle", 
            "Easy", "src/Test.cs", DateTime.UtcNow.AddHours(-1), null);

        // Assert
        suggestion.StudyResources.Should().NotBeNull();
        suggestion.StudyResources.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            invalidTitle, "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidDescription_ShouldThrowArgumentException(string invalidDescription)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            "Title", invalidDescription, "High", "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*description*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidPriority_ShouldThrowArgumentException(string invalidPriority)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            "Title", "Description", invalidPriority, "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*priority*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            "Title", "Description", "High", invalidType, 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*type*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidDifficult_ShouldThrowArgumentException(string invalidDifficult)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            invalidDifficult, "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*difficult*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidFileReference_ShouldThrowArgumentException(string invalidFileReference)
    {
        // Arrange & Act & Assert
        var action = () => new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", invalidFileReference, DateTime.UtcNow.AddHours(-1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*fileReference*");
    }

    [Fact]
    public void Constructor_WithFutureLastUpdate_ShouldThrowArgumentException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act & Assert
        var action = () => new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", futureDate);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*LastUpdate cannot be in the future*");
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    public void Constructor_WithDifferentPriorities_ShouldCreateValidSuggestion(string priority)
    {
        // Arrange & Act
        var suggestion = new Suggestion(
            "Test Title", "Test Description", priority, "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        // Assert
        suggestion.Priority.Should().Be(priority);
    }

    [Theory]
    [InlineData("Refactoring")]
    [InlineData("CodeStyle")]
    [InlineData("Performance")]
    [InlineData("Security")]
    [InlineData("Maintainability")]
    public void Constructor_WithDifferentTypes_ShouldCreateValidSuggestion(string type)
    {
        // Arrange & Act
        var suggestion = new Suggestion(
            "Test Title", "Test Description", "Medium", type, 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        // Assert
        suggestion.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("Easy")]
    [InlineData("Medium")]
    [InlineData("Hard")]
    public void Constructor_WithDifferentDifficulties_ShouldCreateValidSuggestion(string difficulty)
    {
        // Arrange & Act
        var suggestion = new Suggestion(
            "Test Title", "Test Description", "Medium", "Refactoring", 
            difficulty, "src/Test.cs", DateTime.UtcNow.AddHours(-1));

        // Assert
        suggestion.Difficult.Should().Be(difficulty);
    }

    [Fact]
    public void StudyResources_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var studyResources = new List<string> { "Resource 1", "Resource 2", "Resource 3" };
        
        var suggestion = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1), studyResources);

        // Act
        var resources = suggestion.StudyResources;

        // Assert
        resources.Should().BeAssignableTo<IReadOnlyList<string>>();
        resources.Should().HaveCount(3);
        resources.GetType().Name.Should().Contain("ReadOnly");
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Resource 1", "Resource 2" };

        var suggestion1 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        var suggestion2 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        // Act & Assert
        suggestion1.Should().Be(suggestion2);
        suggestion1.Equals(suggestion2).Should().BeTrue();
        (suggestion1 == suggestion2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Resource 1" };

        var suggestion1 = new Suggestion(
            "Title 1", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        var suggestion2 = new Suggestion(
            "Title 2", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        // Act & Assert
        suggestion1.Should().NotBe(suggestion2);
        suggestion1.Equals(suggestion2).Should().BeFalse();
        (suggestion1 == suggestion2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Resource 1", "Resource 2" };

        var suggestion1 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        var suggestion2 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        // Act & Assert
        suggestion1.GetHashCode().Should().Be(suggestion2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Resource 1" };

        var suggestion1 = new Suggestion(
            "Title 1", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        var suggestion2 = new Suggestion(
            "Title 2", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, studyResources);

        // Act & Assert
        suggestion1.GetHashCode().Should().NotBe(suggestion2.GetHashCode());
    }

    [Fact]
    public void Constructor_WithEmptyStudyResourcesList_ShouldCreateEmptyList()
    {
        // Arrange
        var emptyResources = new List<string>();

        // Act
        var suggestion = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1), emptyResources);

        // Assert
        suggestion.StudyResources.Should().BeEmpty();
        suggestion.StudyResources.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLargeStudyResourcesList_ShouldCreateValidSuggestion()
    {
        // Arrange
        var largeResourcesList = new List<string>();
        for (int i = 1; i <= 10; i++)
        {
            largeResourcesList.Add($"Resource {i}");
        }

        // Act
        var suggestion = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", DateTime.UtcNow.AddHours(-1), largeResourcesList);

        // Assert
        suggestion.StudyResources.Should().HaveCount(10);
        suggestion.StudyResources.Should().BeEquivalentTo(largeResourcesList);
    }

    [Fact]
    public void Constructor_WithBuilder_ShouldCreateValidSuggestion()
    {
        // Arrange & Act
        var suggestion = SuggestionBuilder.Create()
            .WithTitle("Extract Method")
            .WithDescription("Break down large methods into smaller, focused methods")
            .WithPriority("High")
            .WithType("Refactoring")
            .WithDifficult("Medium")
            .WithFileReference("src/LargeClass.cs")
            .WithStudyResources("Clean Code - Chapter 3", "Refactoring - Extract Method")
            .Build();

        // Assert
        suggestion.Title.Should().Be("Extract Method");
        suggestion.Description.Should().Be("Break down large methods into smaller, focused methods");
        suggestion.Priority.Should().Be("High");
        suggestion.Type.Should().Be("Refactoring");
        suggestion.Difficult.Should().Be("Medium");
        suggestion.FileReference.Should().Be("src/LargeClass.cs");
        suggestion.StudyResources.Should().HaveCount(2);
        suggestion.StudyResources.Should().Contain("Clean Code - Chapter 3");
        suggestion.StudyResources.Should().Contain("Refactoring - Extract Method");
    }

    [Fact]
    public void Equals_WithDifferentStudyResources_ShouldReturnFalse()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(-1);
        var resources1 = new List<string> { "Resource 1", "Resource 2" };
        var resources2 = new List<string> { "Resource 1", "Resource 3" };

        var suggestion1 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, resources1);

        var suggestion2 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date, resources2);

        // Act & Assert
        suggestion1.Should().NotBe(suggestion2);
    }

    [Fact]
    public void Equals_WithDifferentLastUpdate_ShouldReturnFalse()
    {
        // Arrange
        var date1 = DateTime.UtcNow.AddHours(-2);
        var date2 = DateTime.UtcNow.AddHours(-1);
        var studyResources = new List<string> { "Resource 1" };

        var suggestion1 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date1, studyResources);

        var suggestion2 = new Suggestion(
            "Title", "Description", "High", "Refactoring", 
            "Medium", "src/Test.cs", date2, studyResources);

        // Act & Assert
        suggestion1.Should().NotBe(suggestion2);
    }
}