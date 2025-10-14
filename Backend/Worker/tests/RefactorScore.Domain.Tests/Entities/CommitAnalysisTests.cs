using FluentAssertions;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.Exceptions;
using RefactorScore.Domain.Tests.Builders;
using RefactorScore.Domain.ValueObjects;
using Xunit;

namespace RefactorScore.Domain.Tests.Entities;

public class CommitAnalysisTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateCommitAnalysis()
    {
        // Arrange
        var commitId = "abc123";
        var author = "John Doe";
        var email = "john@example.com";
        var commitDate = DateTime.UtcNow.AddDays(-1);
        var analysisDate = DateTime.UtcNow;
        var language = "C#";
        var addedLines = 100;
        var removedLines = 50;

        // Act
        var analysis = new CommitAnalysis(
            commitId, author, email, commitDate, 
            analysisDate, language, addedLines, removedLines);

        // Assert
        analysis.CommitId.Should().Be(commitId);
        analysis.Author.Should().Be(author);
        analysis.Email.Should().Be(email);
        analysis.CommitDate.Should().Be(commitDate);
        analysis.AnalysisDate.Should().Be(analysisDate);
        analysis.Language.Should().Be(language);
        analysis.AddedLines.Should().Be(addedLines);
        analysis.RemovedLines.Should().Be(removedLines);
        analysis.Files.Should().BeEmpty();
        analysis.Suggestions.Should().BeEmpty();
        analysis.Rating.Should().BeNull();
        analysis.OverallNote.Should().Be(0.0);
    }

    [Fact]
    public void AddFile_WithValidFile_ShouldAddFileToCollection()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .Build();

        // Act
        analysis.AddFile(file);

        // Assert
        analysis.Files.Should().HaveCount(1);
        analysis.Files.First().Should().Be(file);
    }

    [Fact]
    public void AddFile_WithDuplicatePath_ShouldThrowDomainException()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var file1 = CommitFileBuilder.Create()
            .WithRandomData()
            .WithPath("src/Calculator.cs")
            .Build();

        var file2 = CommitFileBuilder.Create()
            .WithRandomData()
            .WithPath("src/Calculator.cs")
            .Build();

        analysis.AddFile(file1);

        // Act & Assert
        var action = () => analysis.AddFile(file2);
        action.Should().Throw<DomainException>()
            .WithMessage("File src/Calculator.cs already exists in this analysis");
    }

    [Fact]
    public void CompleteFileAnalysis_WithValidData_ShouldSetFileAnalysis()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .WithPath("src/Calculator.cs")
            .Build();

        var rating = CleanCodeRatingBuilder.Create()
            .WithExcellentScores()
            .Build();

        var suggestions = new List<Suggestion>
        {
            SuggestionBuilder.Create()
                .WithRandomData()
                .WithFileReference("src/Calculator.cs")
                .Build()
        };

        analysis.AddFile(file);

        // Act
        analysis.CompleteFileAnalysis("src/Calculator.cs", rating, suggestions);

        // Assert
        var analyzedFile = analysis.Files.First();
        analyzedFile.HasAnalysis.Should().BeTrue();
        analyzedFile.Rating.Should().Be(rating);
        analyzedFile.Suggestions.Should().HaveCount(1);
        analysis.Suggestions.Should().HaveCount(1);
    }

    [Fact]
    public void Rating_WithAnalyzedFiles_ShouldCalculateOverallRating()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var file1 = CommitFileBuilder.Create()
            .WithRandomData()
            .WithRandomRating()
            .Build();

        var file2 = CommitFileBuilder.Create()
            .WithRandomData()
            .WithRandomRating()
            .Build();

        analysis.AddFile(file1);
        analysis.AddFile(file2);

        // Act
        var rating = analysis.Rating;

        // Assert
        rating.Should().NotBeNull();
        rating!.VariableNaming.Should().BeGreaterThan(0);
        rating.FunctionSizes.Should().BeGreaterThan(0);
        rating.NoNeedsComments.Should().BeGreaterThan(0);
        rating.MethodCohesion.Should().BeGreaterThan(0);
        rating.DeadCode.Should().BeGreaterThan(0);
        rating.Note.Should().BeGreaterThan(0);
        rating.Quality.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OverallNote_WithAnalyzedFiles_ShouldReturnRatingNote()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var file = CommitFileBuilder.Create()
            .WithRandomData()
            .WithRating(CleanCodeRatingBuilder.Create().WithExcellentScores().Build())
            .Build();

        analysis.AddFile(file);

        // Act
        var overallNote = analysis.OverallNote;

        // Assert
        overallNote.Should().Be(10.0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidCommitId_ShouldThrowArgumentException(string invalidCommitId)
    {
        // Arrange & Act & Assert
        var action = () => new CommitAnalysis(
            invalidCommitId, "author", "email@test.com", 
            DateTime.UtcNow, DateTime.UtcNow, "C#", 0, 0);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddSuggestion_WithValidSuggestion_ShouldAddToCollection()
    {
        // Arrange
        var analysis = CommitAnalysisBuilder.Create()
            .WithRandomData()
            .Build();

        var suggestion = SuggestionBuilder.Create()
            .WithRandomData()
            .Build();

        // Act
        analysis.AddSuggestion(suggestion);

        // Assert
        analysis.Suggestions.Should().HaveCount(1);
        analysis.Suggestions.First().Should().Be(suggestion);
    }
}