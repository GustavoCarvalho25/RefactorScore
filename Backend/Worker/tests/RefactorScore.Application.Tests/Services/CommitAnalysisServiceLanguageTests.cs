using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RefactorScore.Application.Services;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.Enum;
using RefactorScore.Domain.Models;
using RefactorScore.Domain.Repositories;
using RefactorScore.Domain.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace RefactorScore.Application.Tests.Services;

public class CommitAnalysisServiceLanguageTests
{
    private static CommitAnalysisService CreateService(
        ICommitAnalysisRepository? repoMock = null,
        IGitServiceFacade? gitMock = null,
        ILLMService? llmMock = null,
        ILogger<CommitAnalysisService>? logger = null)
    {
        repoMock ??= Substitute.For<ICommitAnalysisRepository>();
        gitMock ??= Substitute.For<IGitServiceFacade>();
        llmMock ??= Substitute.For<ILLMService>();
        logger ??= Substitute.For<ILogger<CommitAnalysisService>>();
        return new CommitAnalysisService(repoMock, logger, gitMock, llmMock);
    }

    private static CommitData MakeCommit(string id = "c1") => new CommitData
    {
        Id = id,
        Author = "Tester",
        Email = "tester@example.com",
        Date = DateTime.UtcNow
    };

    [Fact]
    public async Task AnalyzeCommitAsync_NoFiles_ShouldSetLanguageUnknown()
    {
        // Arrange
        var repo = Substitute.For<ICommitAnalysisRepository>();
        repo.GetByCommitIdAsync(Arg.Any<string>()).Returns((CommitAnalysis)null);

        var git = Substitute.For<IGitServiceFacade>();
        git.GetCommitByIdAsync(Arg.Any<string>()).Returns(MakeCommit());
        git.GetCommitChangesAsync(Arg.Any<string>()).Returns(new List<FileChange>());

        var llm = Substitute.For<ILLMService>();

        var svc = CreateService(repo, git, llm);

        // Act
        var result = await svc.AnalyzeCommitAsync("c1");

        // Assert
        result.Language.Should().Be("Unknown");
    }

    [Fact]
    public async Task AnalyzeCommitAsync_OnlyNonSourceFiles_ShouldSetLanguageUnknown()
    {
        // Arrange
        var repo = Substitute.For<ICommitAnalysisRepository>();
        repo.GetByCommitIdAsync(Arg.Any<string>()).Returns((CommitAnalysis)null);

        var changes = new List<FileChange>
        {
            new FileChange { Path = "README.md", Language = "Markdown", IsSourceCode = false, ChangeType = FileChangeType.Modified },
            new FileChange { Path = "docs/notes.txt", Language = "TXT", IsSourceCode = false, ChangeType = FileChangeType.Added }
        };

        var git = Substitute.For<IGitServiceFacade>();
        git.GetCommitByIdAsync(Arg.Any<string>()).Returns(MakeCommit());
        git.GetCommitChangesAsync(Arg.Any<string>()).Returns(changes);

        var llm = Substitute.For<ILLMService>();

        var svc = CreateService(repo, git, llm);

        // Act
        var result = await svc.AnalyzeCommitAsync("c2");

        // Assert
        result.Language.Should().Be("Unknown");
    }

    [Fact]
    public async Task AnalyzeCommitAsync_OnlyDeletedSourceFiles_ShouldSetLanguageUnknown()
    {
        // Arrange
        var repo = Substitute.For<ICommitAnalysisRepository>();
        repo.GetByCommitIdAsync(Arg.Any<string>()).Returns((CommitAnalysis)null);

        var changes = new List<FileChange>
        {
            new FileChange { Path = "src/File.cs", Language = "C#", IsSourceCode = true, ChangeType = FileChangeType.Deleted }
        };

        var git = Substitute.For<IGitServiceFacade>();
        git.GetCommitByIdAsync(Arg.Any<string>()).Returns(MakeCommit());
        git.GetCommitChangesAsync(Arg.Any<string>()).Returns(changes);

        var llm = Substitute.For<ILLMService>();

        var svc = CreateService(repo, git, llm);

        // Act
        var result = await svc.AnalyzeCommitAsync("c3");

        // Assert
        result.Language.Should().Be("Unknown");
    }

    [Fact]
    public async Task AnalyzeCommitAsync_SourceFiles_ShouldPickMajorityLanguage()
    {
        // Arrange
        var repo = Substitute.For<ICommitAnalysisRepository>();
        repo.GetByCommitIdAsync(Arg.Any<string>()).Returns((CommitAnalysis)null);

        var changes = new List<FileChange>
        {
            new FileChange { Path = "src/A.cs", Language = "C#", IsSourceCode = true, ChangeType = FileChangeType.Modified, Content = "class A{}" },
            new FileChange { Path = "src/B.cs", Language = "C#", IsSourceCode = true, ChangeType = FileChangeType.Added, Content = "class B{}" },
            new FileChange { Path = "web/app.js", Language = "JavaScript", IsSourceCode = true, ChangeType = FileChangeType.Modified, Content = "function f(){}" }
        };

        var git = Substitute.For<IGitServiceFacade>();
        git.GetCommitByIdAsync(Arg.Any<string>()).Returns(MakeCommit());
        git.GetCommitChangesAsync(Arg.Any<string>()).Returns(changes);

        var llm = Substitute.For<ILLMService>();
        llm.AnalyzeFileAsync(Arg.Any<string>()).Returns(new LLMAnalysisResult
        {
            VariableScore = 5,
            FunctionScore = 5,
            CommentScore = 5,
            CohesionScore = 5,
            DeadCodeScore = 5,
            Justifications = new Dictionary<string, string>()
        });
        llm.GenerateSuggestionsAsync(Arg.Any<string>(), Arg.Any<RefactorScore.Domain.ValueObjects.CleanCodeRating>())
            .Returns(new List<LLMSuggestion>());

        var svc = CreateService(repo, git, llm);

        // Act
        var result = await svc.AnalyzeCommitAsync("c4");

        // Assert
        result.Language.Should().Be("C#");
    }
}
