using RefactorScore.Domain.Enum;
using RefactorScore.Domain.SeedWork;
using Ardalis.GuardClauses;

namespace RefactorScore.Domain.ValueObjects;

public class CleanCodeRating : ValueObject
{
    public int VariableNaming { get; private set; }
    public int FunctionSizes  { get; private set; }
    public int NoNeedsComments { get; private set; }
    public int MethodCohesion { get; private set; }
    public int DeadCode { get; private set; }

    public double Note => CalculateNote();
    
    public Dictionary<string, string> Justifications { get; private set; }
    
    private double CalculateNote()
        => (VariableNaming + FunctionSizes + NoNeedsComments + MethodCohesion + DeadCode) / 5.0;
    
    public string Quality
    {
        get
        {
            return Note switch
            {
                >= 9.0 => RatingQuality.Excellent.ToString(),
                >= 7.5 => RatingQuality.VeryGood.ToString(),
                >= 6.0 => RatingQuality.Good.ToString(),
                >= 5.0 => RatingQuality.Acceptable.ToString(),
                >= 3.5 => RatingQuality.NeedsImprovement.ToString(),
                _ => RatingQuality.Problematic.ToString()
            };
        }
    }
    
    public CleanCodeRating(int variableNaming, int functionSizes, int noNeedsComments, int methodCohesion, int deadCode, Dictionary<string, string> justifications = null)
    {
        Guard.Against.NegativeOrZero(variableNaming, nameof(variableNaming));
        Guard.Against.NegativeOrZero(functionSizes, nameof(functionSizes));
        Guard.Against.NegativeOrZero(noNeedsComments, nameof(noNeedsComments));
        Guard.Against.NegativeOrZero(methodCohesion, nameof(methodCohesion));
        Guard.Against.NegativeOrZero(deadCode, nameof(deadCode));
        
        Guard.Against.OutOfRange(variableNaming, nameof(variableNaming), 1, 10);
        Guard.Against.OutOfRange(functionSizes, nameof(functionSizes), 1, 10);
        Guard.Against.OutOfRange(noNeedsComments, nameof(noNeedsComments), 1, 10);
        Guard.Against.OutOfRange(methodCohesion, nameof(methodCohesion), 1, 10);
        Guard.Against.OutOfRange(deadCode, nameof(deadCode), 1, 10);
        
        VariableNaming = variableNaming;
        FunctionSizes = functionSizes;
        NoNeedsComments = noNeedsComments;
        MethodCohesion = methodCohesion;
        DeadCode = deadCode;
        Justifications = (justifications ?? new Dictionary<string, string>());
    }

    private CleanCodeRating()
    {
        Justifications = new Dictionary<string, string>();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VariableNaming;
        yield return FunctionSizes;
        yield return NoNeedsComments;
        yield return MethodCohesion;
        yield return DeadCode;
        
        foreach (var kvp in Justifications.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
        
        yield return Justifications.Count;
    }
}