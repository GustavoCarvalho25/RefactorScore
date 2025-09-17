using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.ValueObjects;
using RefactorScore.Integration.Tests.Infrastructure;
using MongoDB.Driver;
using Xunit;

namespace RefactorScore.Integration.Tests.Repositories;

public class CommitAnalysisRepositoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AddAsync_WithValidCommitAnalysis_ShouldPersistToDatabase()
    {
        // Arrange
        var commitAnalysis = CreateSampleCommitAnalysis();

        // Act
        await Repository.AddAsync(commitAnalysis);

        // Assert
        var collection = await GetCollectionAsync<CommitAnalysis>("CommitAnalysis");
        var persistedAnalysis = await collection.Find(x => x.Id == commitAnalysis.Id).FirstOrDefaultAsync();
        
        persistedAnalysis.Should().NotBeNull();
        persistedAnalysis!.CommitId.Should().Be(commitAnalysis.CommitId);
        persistedAnalysis.Author.Should().Be(commitAnalysis.Author);
        persistedAnalysis.Email.Should().Be(commitAnalysis.Email);
        persistedAnalysis.Language.Should().Be(commitAnalysis.Language);
        persistedAnalysis.Files.Should().HaveCount(commitAnalysis.Files.Count);
        persistedAnalysis.Suggestions.Should().HaveCount(commitAnalysis.Suggestions.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCommitAnalysis()
    {
        // Arrange
        var commitAnalysis = CreateSampleCommitAnalysis();
        await Repository.AddAsync(commitAnalysis);

        // Act
        var result = await Repository.GetByIdAsync(commitAnalysis.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(commitAnalysis.Id);
        result.CommitId.Should().Be(commitAnalysis.CommitId);
        result.Author.Should().Be(commitAnalysis.Author);
        result.Files.Should().HaveCount(commitAnalysis.Files.Count);
        result.Suggestions.Should().HaveCount(commitAnalysis.Suggestions.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid().ToString();

        // Act
        var result = await Repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCommitIdAsync_WithExistingCommitId_ShouldReturnCommitAnalysis()
    {
        // Arrange
        var commitAnalysis = CreateSampleCommitAnalysis();
        await Repository.AddAsync(commitAnalysis);

        // Act
        var result = await Repository.GetByCommitIdAsync(commitAnalysis.CommitId);

        // Assert
        result.Should().NotBeNull();
        result!.CommitId.Should().Be(commitAnalysis.CommitId);
        result.Author.Should().Be(commitAnalysis.Author);
    }

    [Fact]
    public async Task GetByCommitIdAsync_WithNonExistingCommitId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingCommitId = "non-existing-commit-id";

        // Act
        var result = await Repository.GetByCommitIdAsync(nonExistingCommitId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleAnalyses_ShouldReturnAllAnalyses()
    {
        // Arrange
        var analysis1 = CreateSampleCommitAnalysis("commit1", "author1@test.com");
        var analysis2 = CreateSampleCommitAnalysis("commit2", "author2@test.com");
        var analysis3 = CreateSampleCommitAnalysis("commit3", "author3@test.com");

        await Repository.AddAsync(analysis1);
        await Repository.AddAsync(analysis2);
        await Repository.AddAsync(analysis3);

        // Act
        var results = await Repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(x => x.CommitId == "commit1");
        results.Should().Contain(x => x.CommitId == "commit2");
        results.Should().Contain(x => x.CommitId == "commit3");
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedAnalysis_ShouldUpdateInDatabase()
    {
        // Arrange
        var commitAnalysis = CreateSampleCommitAnalysis();
        await Repository.AddAsync(commitAnalysis);

        var rating = new CleanCodeRating(8, 7, 9, 6, 5);
        var suggestions = new List<Suggestion>
        {
            new("Updated Suggestion", "Updated Description", "High", "Refactoring", "Medium", "src/UpdatedFile.cs", DateTime.UtcNow)
        };
        commitAnalysis.CompleteFileAnalysis("src/TestFile1.cs", rating, suggestions);

        // Act
        await Repository.UpdateAsync(commitAnalysis);

        // Assert
        var updatedAnalysis = await Repository.GetByIdAsync(commitAnalysis.Id);
        updatedAnalysis.Should().NotBeNull();
        updatedAnalysis!.Files.First().HasAnalysis.Should().BeTrue();
        updatedAnalysis.Files.First().Rating.Should().NotBeNull();
        updatedAnalysis.Suggestions.Should().HaveCount(suggestions.Count);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var commitAnalysis = CreateSampleCommitAnalysis();
        await Repository.AddAsync(commitAnalysis);

        var existingAnalysis = await Repository.GetByIdAsync(commitAnalysis.Id);
        existingAnalysis.Should().NotBeNull();

        // Act
        await Repository.DeleteAsync(commitAnalysis.Id);

        // Assert
        var deletedAnalysis = await Repository.GetByIdAsync(commitAnalysis.Id);
        deletedAnalysis.Should().BeNull();
    }

    [Fact]
    public async Task ComplexScenario_WithCompleteWorkflow_ShouldPersistAllData()
    {
        // Arrange
        var commitAnalysis = CreateCompleteCommitAnalysis();

        // Act - Add to repository
        await Repository.AddAsync(commitAnalysis);

        // Assert 
        var persistedAnalysis = await Repository.GetByCommitIdAsync(commitAnalysis.CommitId);
        
        persistedAnalysis.Should().NotBeNull();
        persistedAnalysis!.Files.Should().HaveCount(2);
        persistedAnalysis.Suggestions.Should().HaveCount(3);
        
        var firstFile = persistedAnalysis.Files.First();
        firstFile.HasAnalysis.Should().BeTrue();
        firstFile.Rating.Should().NotBeNull();
        firstFile.Rating!.Note.Should().Be(7.0);
        firstFile.Rating.Quality.Should().Be("Good");
        firstFile.Suggestions.Should().HaveCount(2);
        
        persistedAnalysis.OverallNote.Should().BeGreaterThan(0);
        persistedAnalysis.AddedLines.Should().Be(150);
        persistedAnalysis.RemovedLines.Should().Be(50);
    }

    [Fact]
    public async Task MongoDbMapping_WithComplexValueObjects_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var justifies = new Dictionary<string, string>
        {
            { "VariableNaming", "Good variable names throughout the code" },
            { "FunctionSizes", "Functions are appropriately sized" },
            { "Comments", "Code is self-documenting" }
        };
        
        var rating = new CleanCodeRating(8, 7, 9, 6, 5, justifies);
        var studyResources = new List<string> { "Clean Code Book", "SOLID Principles", "Refactoring Guide" };
        var suggestion = new Suggestion(
            "Improve Variable Naming",
            "Consider using more descriptive variable names",
            "Medium",
            "Refactoring",
            "Easy",
            "src/TestFile1.cs",
            DateTime.UtcNow.AddHours(-1),
            studyResources
        );

        var commitAnalysis = CreateSampleCommitAnalysis();
        commitAnalysis.CompleteFileAnalysis("src/TestFile1.cs", rating, new List<Suggestion> { suggestion });

        // Act
        await Repository.AddAsync(commitAnalysis);
        var retrieved = await Repository.GetByIdAsync(commitAnalysis.Id);

        // Assert
        var retrievedFile = retrieved!.Files.First();
        retrievedFile.Rating.Should().NotBeNull();
        retrievedFile.Rating!.VariableNaming.Should().Be(8);
        retrievedFile.Rating.FunctionSizes.Should().Be(7);
        retrievedFile.Rating.NoNeedsComments.Should().Be(9);
        retrievedFile.Rating.MethodCohesion.Should().Be(6);
        retrievedFile.Rating.DeadCode.Should().Be(5);
        retrievedFile.Rating.Note.Should().Be(7.0);
        retrievedFile.Rating.Quality.Should().Be("Good");
        retrievedFile.Rating.Justifies.Should().HaveCount(3);
        retrievedFile.Rating.Justifies["VariableNaming"].Should().Be("Good variable names throughout the code");

        // Assert
        var retrievedSuggestion = retrieved.Suggestions.First();
        retrievedSuggestion.Title.Should().Be("Improve Variable Naming");
        retrievedSuggestion.Description.Should().Be("Consider using more descriptive variable names");
        retrievedSuggestion.Priority.Should().Be("Medium");
        retrievedSuggestion.Type.Should().Be("Refactoring");
        retrievedSuggestion.Difficult.Should().Be("Easy");
        retrievedSuggestion.FileReference.Should().Be("src/TestFile1.cs");
        retrievedSuggestion.StudyResources.Should().HaveCount(3);
        retrievedSuggestion.StudyResources.Should().Contain("Clean Code Book");
    }

    private CommitAnalysis CreateSampleCommitAnalysis(string commitId = "test-commit-123", string email = "test@example.com")
    {
        var analysis = new CommitAnalysis(
            commitId,
            "Test Author",
            email,
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow,
            "C#",
            0,
            0 
        );
        
        var CommitFile1 = new CommitFile("src/TestFile1.cs", 50, 25, "C#", "public class Test { }");

        var CommitFile2 = new CommitFile("src/TestFile2.cs", 100, 25, "C#", "public class Test { }");

        
        analysis.AddFile(CommitFile1);
        analysis.AddFile(CommitFile2);
        
        return analysis;
    }

    private CommitAnalysis CreateCompleteCommitAnalysis()
    {
        var analysis = new CommitAnalysis(
            "complete-commit-456",
            "Complete Author",
            "complete@example.com",
            DateTime.UtcNow.AddHours(-3),
            DateTime.UtcNow,
            "C#",
            0,
            0 
        );

        var commitFile1 = new CommitFile("src/CompleteFile1.cs", 100, 25, "C#", "public class Complete1 { }");
        var commitFile2 = new CommitFile("src/CompleteFile2.cs", 50, 25, "C#", "public class Complete2 { }");
        
        analysis.AddFile(commitFile1);
        analysis.AddFile(commitFile2);

        var rating1 = new CleanCodeRating(8, 7, 6, 7, 7);
        var suggestions1 = new List<Suggestion>
        {
            new("Suggestion 1", "Description 1", "High", "Refactoring", "Medium", "src/CompleteFile1.cs", DateTime.UtcNow),
            new("Suggestion 2", "Description 2", "Low", "Documentation", "Easy", "src/CompleteFile1.cs", DateTime.UtcNow)
        };
        analysis.CompleteFileAnalysis("src/CompleteFile1.cs", rating1, suggestions1);

        var rating2 = new CleanCodeRating(6, 6, 8, 5, 5);
        var suggestions2 = new List<Suggestion>
        {
            new("Suggestion 3", "Description 3", "Medium", "Performance", "Hard", "src/CompleteFile2.cs", DateTime.UtcNow)
        };
        analysis.CompleteFileAnalysis("src/CompleteFile2.cs", rating2, suggestions2);

        return analysis;
    }
}
