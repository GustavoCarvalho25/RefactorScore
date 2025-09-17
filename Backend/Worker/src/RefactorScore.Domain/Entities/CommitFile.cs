using RefactorScore.Domain.SeedWork;
using RefactorScore.Domain.ValueObjects;
using Ardalis.GuardClauses;

namespace RefactorScore.Domain.Entities;

public class CommitFile : Entity
{
    public string Path { get; private set; }
    public string Language { get; private set; }
    public int AddedLines { get; private set; }
    public int RemovedLines { get; private set; }
    public string Content { get; private set; }
    public bool HasAnalysis => Rating != null;
    public CleanCodeRating? Rating { get; private set; }

    private List<Suggestion> _suggestions = new();
    public List<Suggestion> Suggestions => _suggestions;
    
    public CommitFile(string path, int addedLines, int removedLines, string language, string content)
    {
        Guard.Against.NullOrWhiteSpace(path, nameof(path));
        Guard.Against.NullOrWhiteSpace(language, nameof(language));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.Negative(addedLines, nameof(addedLines));
        Guard.Against.Negative(removedLines, nameof(removedLines));
        
        Path = path;
        Language = language;
        Content = content;
        AddedLines = addedLines;
        RemovedLines = removedLines;
    }

    public void SetAnalysis(CleanCodeRating rating, List<Suggestion> suggestions) 
    {
        Guard.Against.Null(rating, nameof(rating));
        Guard.Against.Null(suggestions, nameof(suggestions));
        
        Rating = rating;
        _suggestions.Clear();
        _suggestions.AddRange(suggestions);
    }
}