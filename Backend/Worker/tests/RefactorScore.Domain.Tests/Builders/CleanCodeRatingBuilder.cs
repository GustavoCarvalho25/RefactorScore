using Bogus;
using RefactorScore.Domain.ValueObjects;

namespace RefactorScore.Domain.Tests.Builders;

public class CleanCodeRatingBuilder
{
    private readonly Faker _faker = new();
    private int _variableNaming = 8;
    private int _functionSizes = 7;
    private int _noNeedsComments = 9;
    private int _methodCohesion = 8;
    private int _deadCode = 10;
    private Dictionary<string, string> _justifies = new();

    public static CleanCodeRatingBuilder Create() => new();

    public CleanCodeRatingBuilder WithRandomData()
    {
        _variableNaming = _faker.Random.Int(1, 10);
        _functionSizes = _faker.Random.Int(1, 10);
        _noNeedsComments = _faker.Random.Int(1, 10);
        _methodCohesion = _faker.Random.Int(1, 10);
        _deadCode = _faker.Random.Int(1, 10);
        
        _justifies = new Dictionary<string, string>
        {
            ["VariableNaming"] = _faker.Lorem.Sentence(),
            ["FunctionSizes"] = _faker.Lorem.Sentence(),
            ["NoNeedsComments"] = _faker.Lorem.Sentence(),
            ["MethodCohesion"] = _faker.Lorem.Sentence(),
            ["DeadCode"] = _faker.Lorem.Sentence()
        };
        
        return this;
    }

    public CleanCodeRatingBuilder WithVariableNaming(int score)
    {
        _variableNaming = score;
        return this;
    }

    public CleanCodeRatingBuilder WithFunctionSizes(int score)
    {
        _functionSizes = score;
        return this;
    }

    public CleanCodeRatingBuilder WithNoNeedsComments(int score)
    {
        _noNeedsComments = score;
        return this;
    }

    public CleanCodeRatingBuilder WithMethodCohesion(int score)
    {
        _methodCohesion = score;
        return this;
    }

    public CleanCodeRatingBuilder WithDeadCode(int score)
    {
        _deadCode = score;
        return this;
    }

    public CleanCodeRatingBuilder WithJustifies(Dictionary<string, string> justifies)
    {
        _justifies = justifies;
        return this;
    }

    public CleanCodeRatingBuilder WithExcellentScores()
    {
        _variableNaming = 10;
        _functionSizes = 10;
        _noNeedsComments = 10;
        _methodCohesion = 10;
        _deadCode = 10;
        
        _justifies = new Dictionary<string, string>
        {
            ["VariableNaming"] = "Excellent variable naming throughout the code",
            ["FunctionSizes"] = "All functions are appropriately sized",
            ["NoNeedsComments"] = "Code is self-documenting",
            ["MethodCohesion"] = "Methods have high cohesion",
            ["DeadCode"] = "No dead code found"
        };
        
        return this;
    }

    public CleanCodeRatingBuilder WithPoorScores()
    {
        _variableNaming = 3;
        _functionSizes = 2;
        _noNeedsComments = 4;
        _methodCohesion = 3;
        _deadCode = 5;
        
        _justifies = new Dictionary<string, string>
        {
            ["VariableNaming"] = "Variable names are not descriptive",
            ["FunctionSizes"] = "Functions are too long and complex",
            ["NoNeedsComments"] = "Code lacks proper documentation",
            ["MethodCohesion"] = "Methods have low cohesion",
            ["DeadCode"] = "Some dead code detected"
        };
        
        return this;
    }

    public CleanCodeRating Build()
    {
        return new CleanCodeRating(
            _variableNaming,
            _functionSizes,
            _noNeedsComments,
            _methodCohesion,
            _deadCode,
            _justifies
        );
    }
}