using FluentAssertions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using RefactorScore.Domain.Enum;
using RefactorScore.Domain.Services;
using RefactorScore.Infrastructure.Mappers;
using RefactorScore.Infrastructure.Services;
using Xunit;

namespace RefactorScore.Integration.Tests.Services;

public class GitServiceFacadeIntegrationTests : IDisposable
{
    private readonly string _tempRepoPath;
    private readonly IGitServiceFacade _gitService;
    private readonly ILogger<GitServiceFacade> _logger;
    private readonly GitMapper _mapper;
    private readonly Repository _repository;

    public GitServiceFacadeIntegrationTests()
    {
        // Create temporary repository for testing
        _tempRepoPath = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRepoPath);
        
        // Initialize Git repository
        Repository.Init(_tempRepoPath);
        _repository = new Repository(_tempRepoPath);
        
        // Configure Git user for commits
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        _repository.Config.Set("user.name", signature.Name);
        _repository.Config.Set("user.email", signature.Email);
        
        // Setup dependencies
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning));
        _logger = loggerFactory.CreateLogger<GitServiceFacade>();
        var mapperLogger = loggerFactory.CreateLogger<GitMapper>();
        _mapper = new GitMapper(mapperLogger);
        
        _gitService = new GitServiceFacade(_tempRepoPath, _mapper, _logger);
    }

    [Fact]
    public async Task ValidateRepositoryAsync_WithValidRepository_ShouldReturnTrue()
    {
        // Act
        var result = await _gitService.ValidateRepositoryAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRepositoryAsync_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        var invalidGitService = new GitServiceFacade("/invalid/path", _mapper, _logger);

        // Act
        var result = await invalidGitService.ValidateRepositoryAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCommitByIdAsync_WithValidCommitId_ShouldReturnCommitData()
    {
        // Arrange
        var commitId = CreateSampleCommit("Initial commit", "Program.cs", "using System;\n\nclass Program\n{\n    static void Main() { }\n}");

        // Act
        var result = await _gitService.GetCommitByIdAsync(commitId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(commitId);
        result.Author.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
        result.Message.Should().Be("Initial commit");
        result.MessageShort.Should().Be("Initial commit");
    }

    [Fact]
    public async Task GetCommitByIdAsync_WithInvalidCommitId_ShouldReturnNull()
    {
        // Act
        var result = await _gitService.GetCommitByIdAsync("invalid-commit-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCommitByIdAsync_WithNullOrEmptyCommitId_ShouldReturnNull()
    {
        // Act & Assert
        var resultNull = await _gitService.GetCommitByIdAsync(null!);
        var resultEmpty = await _gitService.GetCommitByIdAsync(string.Empty);
        var resultWhitespace = await _gitService.GetCommitByIdAsync("   ");

        resultNull.Should().BeNull();
        resultEmpty.Should().BeNull();
        resultWhitespace.Should().BeNull();
    }

    [Fact]
    public async Task GetCommitsByPeriodAsync_WithValidPeriod_ShouldReturnCommitsInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);
        
        var commitId1 = CreateSampleCommit("First commit", "File1.cs", "class File1 { }");
        await Task.Delay(100); // Ensure different timestamps
        var commitId2 = CreateSampleCommit("Second commit", "File2.cs", "class File2 { }");

        // Act
        var result = await _gitService.GetCommitsByPeriodAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Id == commitId1);
        result.Should().Contain(c => c.Id == commitId2);
        result.All(c => c.Date >= startDate && c.Date <= endDate).Should().BeTrue();
    }

    [Fact]
    public async Task GetCommitsByPeriodAsync_WithNoDates_ShouldReturnAllCommits()
    {
        // Arrange
        CreateSampleCommit("Commit 1", "File1.cs", "class File1 { }");
        CreateSampleCommit("Commit 2", "File2.cs", "class File2 { }");

        // Act
        var result = await _gitService.GetCommitsByPeriodAsync(null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCommitsByPeriodAsync_WithRestrictivePeriod_ShouldReturnEmptyList()
    {
        // Arrange
        CreateSampleCommit("Test commit", "Test.cs", "class Test { }");
        var futureStart = DateTime.UtcNow.AddDays(1);
        var futureEnd = DateTime.UtcNow.AddDays(2);

        // Act
        var result = await _gitService.GetCommitsByPeriodAsync(futureStart, futureEnd);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithInitialCommit_ShouldReturnAddedFiles()
    {
        // Arrange
        var commitId = CreateSampleCommit("Initial commit", "Program.cs", "using System;\n\nclass Program\n{\n    static void Main() { }\n}");

        // Act
        var result = await _gitService.GetCommitChangesAsync(commitId);

        // Assert
        result.Should().HaveCount(1);
        var fileChange = result.First();
        fileChange.Path.Should().Be("Program.cs");
        fileChange.Language.Should().Be("C#");
        fileChange.ChangeType.Should().Be(FileChangeType.Added);
        fileChange.IsSourceCode.Should().BeTrue();
        fileChange.AddedLines.Should().BeGreaterThan(0);
        fileChange.RemovedLines.Should().Be(0);
        fileChange.Content.Should().Contain("class Program");
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithModifiedFile_ShouldReturnModifiedFileChange()
    {
        // Arrange
        var initialCommitId = CreateSampleCommit("Initial", "Calculator.cs", "class Calculator\n{\n    public int Add(int a, int b) => a + b;\n}");
        var modifiedCommitId = ModifyFile("Calculator.cs", "class Calculator\n{\n    public int Add(int a, int b) => a + b;\n    public int Subtract(int a, int b) => a - b;\n}", "Add subtract method");

        // Act
        var result = await _gitService.GetCommitChangesAsync(modifiedCommitId);

        // Assert
        result.Should().HaveCount(1);
        var fileChange = result.First();
        fileChange.Path.Should().Be("Calculator.cs");
        fileChange.Language.Should().Be("C#");
        fileChange.ChangeType.Should().Be(FileChangeType.Modified);
        fileChange.IsSourceCode.Should().BeTrue();
        fileChange.AddedLines.Should().BeGreaterThan(0);
        fileChange.Content.Should().Contain("Subtract");
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithMultipleFiles_ShouldReturnAllChanges()
    {
        // Arrange
        var commitId = CreateMultipleFilesCommit();

        // Act
        var result = await _gitService.GetCommitChangesAsync(commitId);

        // Assert
        result.Should().HaveCount(4);
        
        // Verify C# file
        var csFile = result.FirstOrDefault(f => f.Path == "UserService.cs");
        csFile.Should().NotBeNull();
        csFile!.Language.Should().Be("C#");
        csFile.IsSourceCode.Should().BeTrue();


        // Verify JavaScript file  
        var jsFile = result.FirstOrDefault(f => f.Path == "app.js");
        jsFile.Should().NotBeNull();
        jsFile!.Language.Should().Be("JavaScript");
        jsFile.IsSourceCode.Should().BeTrue();
        
        // Verify JSON file
        var jsonFile = result.FirstOrDefault(f => f.Path == "config.json");
        jsonFile.Should().NotBeNull();
        jsonFile!.Language.Should().Be("JSON");
        jsonFile.IsSourceCode.Should().BeTrue();
        
        // Verify text file
        var txtFile = result.FirstOrDefault(f => f.Path == "README.txt");
        txtFile.Should().NotBeNull();
        txtFile!.Language.Should().Be("TXT");
        txtFile.IsSourceCode.Should().BeFalse();
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithDeletedFile_ShouldReturnDeletedFileChange()
    {
        // Arrange
        CreateSampleCommit("Add file", "ToDelete.cs", "class ToDelete { }");
        var deleteCommitId = DeleteFile("ToDelete.cs", "Delete file");

        // Act
        var result = await _gitService.GetCommitChangesAsync(deleteCommitId);

        // Assert
        result.Should().HaveCount(1);
        var fileChange = result.First();
        fileChange.Path.Should().Be("ToDelete.cs");
        fileChange.ChangeType.Should().Be(FileChangeType.Deleted);
        fileChange.RemovedLines.Should().BeGreaterThan(0);
        fileChange.AddedLines.Should().Be(0);
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithInvalidCommitId_ShouldReturnEmptyList()
    {
        // Act
        var result = await _gitService.GetCommitChangesAsync("invalid-commit-id");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithNullOrEmptyCommitId_ShouldReturnEmptyList()
    {
        // Act & Assert
        var resultNull = await _gitService.GetCommitChangesAsync(null!);
        var resultEmpty = await _gitService.GetCommitChangesAsync(string.Empty);
        var resultWhitespace = await _gitService.GetCommitChangesAsync("   ");

        resultNull.Should().BeEmpty();
        resultEmpty.Should().BeEmpty();
        resultWhitespace.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommitChangesAsync_WithLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"// Line {i}: This is a large file for testing purposes"));
        var commitId = CreateSampleCommit("Large file commit", "LargeFile.cs", largeContent);

        // Act
        var result = await _gitService.GetCommitChangesAsync(commitId);

        // Assert
        result.Should().HaveCount(1);
        var fileChange = result.First();
        fileChange.Path.Should().Be("LargeFile.cs");
        fileChange.AddedLines.Should().Be(1000);
        fileChange.Content.Should().Contain("Line 1:");
        fileChange.Content.Should().Contain("Line 1000:");
    }

    [Theory]
    [InlineData("test.cs", "C#")]
    [InlineData("test.java", "Java")]
    [InlineData("test.js", "JavaScript")]
    [InlineData("test.ts", "TypeScript")]
    [InlineData("test.py", "Python")]
    [InlineData("test.rb", "Ruby")]
    [InlineData("test.php", "PHP")]
    [InlineData("test.go", "Go")]
    [InlineData("test.html", "HTML")]
    [InlineData("test.css", "CSS")]
    [InlineData("test.sql", "SQL")]
    [InlineData("test.xml", "XML")]
    [InlineData("test.json", "JSON")]
    [InlineData("test.yaml", "YAML")]
    [InlineData("test.unknown", "UNKNOWN")]
    public async Task GetCommitChangesAsync_WithDifferentFileExtensions_ShouldMapLanguageCorrectly(string fileName, string expectedLanguage)
    {
        // Arrange
        var commitId = CreateSampleCommit($"Add {fileName}", fileName, $"// Content of {fileName}");

        // Act
        var result = await _gitService.GetCommitChangesAsync(commitId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Language.Should().Be(expectedLanguage);
    }

    // Helper Methods

    private string CreateSampleCommit(string message, string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(filePath, content);
        
        Commands.Stage(_repository, fileName);
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        var commit = _repository.Commit(message, signature, signature);
        
        return commit.Sha;
    }

    private string ModifyFile(string fileName, string newContent, string commitMessage)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, newContent);
        
        Commands.Stage(_repository, fileName);
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        var commit = _repository.Commit(commitMessage, signature, signature);
        
        return commit.Sha;
    }

    private string DeleteFile(string fileName, string commitMessage)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.Delete(filePath);
        
        Commands.Remove(_repository, fileName);
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        var commit = _repository.Commit(commitMessage, signature, signature);
        
        return commit.Sha;
    }

    private string CreateMultipleFilesCommit()
    {
        // Teste sem subdiretórios primeiro
        var files = new Dictionary<string, string>
        {
            ["UserService.cs"] = "using System;\n\npublic class UserService\n{\n    public void CreateUser() { }\n}",
            ["app.js"] = "function initApp() {\n    console.log('App initialized');\n}",
            ["config.json"] = "{\n  \"database\": \"localhost\",\n  \"port\": 5432\n}",
            ["README.txt"] = "This is a readme file\nWith multiple lines\nFor testing purposes"
        };

        foreach (var (fileName, content) in files)
        {
            var filePath = Path.Combine(_tempRepoPath, fileName);
            File.WriteAllText(filePath, content);
            Commands.Stage(_repository, fileName);
        }

        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        var commit = _repository.Commit("Add multiple files", signature, signature);
    
        return commit.Sha;
    }

    public void Dispose()
    {
        _repository?.Dispose();
        
        if (Directory.Exists(_tempRepoPath))
        {
            try
            {
                // Remove read-only attributes that Git might set
                var directoryInfo = new DirectoryInfo(_tempRepoPath);
                SetAttributesNormal(directoryInfo);
                Directory.Delete(_tempRepoPath, true);
            }
            catch (Exception)
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    private static void SetAttributesNormal(DirectoryInfo directory)
    {
        foreach (var subDir in directory.GetDirectories())
        {
            SetAttributesNormal(subDir);
        }
        
        foreach (var file in directory.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
        
        directory.Attributes = FileAttributes.Normal;
    }
}
