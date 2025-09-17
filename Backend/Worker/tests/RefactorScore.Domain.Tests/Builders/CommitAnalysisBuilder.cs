using Bogus;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.ValueObjects;

namespace RefactorScore.Domain.Tests.Builders;

public class CommitAnalysisBuilder
{
    private readonly Faker _faker = new();
    private string _commitId = string.Empty;
    private string _author = string.Empty;
    private string _email = string.Empty;
    private DateTime _commitDate = DateTime.UtcNow;
    private DateTime _analysisDate = DateTime.UtcNow;
    private string _language = "C#";
    private int _addedLines = 0;
    private int _removedLines = 0;
    private readonly List<CommitFile> _files = new();
    private readonly List<Suggestion> _suggestions = new();

    public static CommitAnalysisBuilder Create() => new();

    public CommitAnalysisBuilder WithCommitId(string commitId)
    {
        _commitId = commitId;
        return this;
    }

    public CommitAnalysisBuilder WithRandomData()
    {
        _commitId = _faker.Random.Hash(40);
        _author = _faker.Person.FullName;
        _email = _faker.Person.Email;
        _commitDate = _faker.Date.Recent(30);
        _analysisDate = _faker.Date.Recent(1);
        _language = _faker.PickRandom("C#", "Java", "JavaScript", "Python", "TypeScript");
        _addedLines = _faker.Random.Int(1, 500);
        _removedLines = _faker.Random.Int(0, 100);
        return this;
    }

    public CommitAnalysisBuilder WithAuthor(string author)
    {
        _author = author;
        return this;
    }

    public CommitAnalysisBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CommitAnalysisBuilder WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    public CommitAnalysisBuilder WithFiles(params CommitFile[] files)
    {
        _files.AddRange(files);
        return this;
    }

    public CommitAnalysisBuilder WithRandomFiles(int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            var file = CommitFileBuilder.Create().WithRandomData().Build();
            _files.Add(file);
        }
        return this;
    }

    public CommitAnalysisBuilder WithSuggestions(params Suggestion[] suggestions)
    {
        _suggestions.AddRange(suggestions);
        return this;
    }

    public CommitAnalysisBuilder WithRandomSuggestions(int count = 2)
    {
        for (int i = 0; i < count; i++)
        {
            var suggestion = SuggestionBuilder.Create().WithRandomData().Build();
            _suggestions.Add(suggestion);
        }
        return this;
    }

    public CommitAnalysis Build()
    {
        if (string.IsNullOrEmpty(_commitId))
            _commitId = _faker.Random.Hash(40);
        
        if (string.IsNullOrEmpty(_author))
            _author = _faker.Person.FullName;
        
        if (string.IsNullOrEmpty(_email))
            _email = _faker.Person.Email;

        var analysis = new CommitAnalysis(
            _commitId,
            _author,
            _email,
            _commitDate,
            _analysisDate,
            _language,
            _addedLines,
            _removedLines
        );

        foreach (var file in _files)
        {
            analysis.AddFile(file);
        }

        foreach (var suggestion in _suggestions)
        {
            analysis.AddSuggestion(suggestion);
        }

        return analysis;
    }
}
