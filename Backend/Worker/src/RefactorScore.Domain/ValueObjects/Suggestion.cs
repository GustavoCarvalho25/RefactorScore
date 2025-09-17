using RefactorScore.Domain.SeedWork;
using Ardalis.GuardClauses;

namespace RefactorScore.Domain.ValueObjects;

public class Suggestion : ValueObject
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Priority { get; private set; }
    public string Type { get; private set; }
    public string Difficult { get; private set; }
    public string FileReference { get; private set; } 
    public DateTime LastUpdate { get; private set; }
    public IReadOnlyList<string> StudyResources { get; private set; }
    
        
    public Suggestion(
        string title,
        string description,
        string priority,
        string type,
        string difficult,
        string fileReference,
        DateTime lastUpdate,
        List<string> studyResources = null
        )
    {
        // Validações usando Ardalis.GuardClauses
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(priority, nameof(priority));
        Guard.Against.NullOrWhiteSpace(type, nameof(type));
        Guard.Against.NullOrWhiteSpace(difficult, nameof(difficult));
        Guard.Against.NullOrWhiteSpace(fileReference, nameof(fileReference));
        
        // Validação de data
        if (lastUpdate > DateTime.UtcNow)
            throw new ArgumentException("LastUpdate cannot be in the future", nameof(lastUpdate));
        
        Title = title;
        Description = description;
        Priority = priority;
        Type = type;
        Difficult = difficult;
        FileReference = fileReference;
        LastUpdate = lastUpdate;
        StudyResources = (studyResources ?? new List<string>()).AsReadOnly();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Title;
        yield return Description;
        yield return Priority;
        yield return Type;
        yield return Difficult;
        yield return FileReference;
        yield return LastUpdate;
        
        // Para coleções, precisamos retornar cada item individualmente
        foreach (var resource in StudyResources)
        {
            yield return resource;
        }
        
        // Também incluir o count para garantir que listas de tamanhos diferentes sejam diferentes
        yield return StudyResources.Count;
    }
}