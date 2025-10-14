using Bogus;
using RefactorScore.Domain.ValueObjects;

namespace RefactorScore.Domain.Tests.Builders;

public class SuggestionBuilder
{
    private readonly Faker _faker = new();
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _priority = "Medium";
    private string _type = "Refactoring";
    private string _difficult = "Medium";
    private string _fileReference = string.Empty;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private List<string> _studyResources = new();

    public static SuggestionBuilder Create() => new();

    public SuggestionBuilder WithRandomData()
    {
        _title = _faker.Hacker.Phrase();
        _description = _faker.Lorem.Sentences(2);
        _priority = _faker.PickRandom("Low", "Medium", "High", "Critical");
        _type = _faker.PickRandom("Refactoring", "CodeStyle", "Performance", "Security", "Maintainability");
        _difficult = _faker.PickRandom("Easy", "Medium", "Hard");
        _fileReference = $"{_faker.System.DirectoryPath()}/{_faker.System.FileName(".cs")}";
        _lastUpdate = _faker.Date.Recent(7);
        
        var resourceCount = _faker.Random.Int(0, 3);
        _studyResources = _faker.Make(resourceCount, () => 
            $"{ToTitleCase(_faker.Hacker.Noun())} - Chapter {_faker.Random.Int(1, 20)}") as List<string>;
        
        return this;
    }

    public SuggestionBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public SuggestionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public SuggestionBuilder WithPriority(string priority)
    {
        _priority = priority;
        return this;
    }

    public SuggestionBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public SuggestionBuilder WithDifficult(string difficult)
    {
        _difficult = difficult;
        return this;
    }

    public SuggestionBuilder WithFileReference(string fileReference)
    {
        _fileReference = fileReference;
        return this;
    }

    public SuggestionBuilder WithLastUpdate(DateTime lastUpdate)
    {
        _lastUpdate = lastUpdate;
        return this;
    }

    public SuggestionBuilder WithStudyResources(params string[] studyResources)
    {
        _studyResources = studyResources.ToList();
        return this;
    }

    public Suggestion Build()
    {
        if (string.IsNullOrEmpty(_title))
            _title = _faker.Hacker.Phrase();
        
        if (string.IsNullOrEmpty(_description))
            _description = _faker.Lorem.Sentences(2);
        
        if (string.IsNullOrEmpty(_fileReference))
            _fileReference = $"{_faker.System.DirectoryPath()}/{_faker.System.FileName(".cs")}";

        return new Suggestion(
            _title,
            _description,
            _priority,
            _type,
            _difficult,
            _fileReference,
            _lastUpdate,
            _studyResources
        );
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}
