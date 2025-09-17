using Bogus;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.ValueObjects;

namespace RefactorScore.Domain.Tests.Builders;

public class CommitFileBuilder
{
    private readonly Faker _faker = new();
    private string _path = string.Empty;
    private int _addedLines = 0;
    private int _removedLines = 0;
    private string _language = "C#";
    private string _content = string.Empty;
    private CleanCodeRating? _rating;
    private readonly List<Suggestion> _suggestions = new();

    public static CommitFileBuilder Create() => new();

    public CommitFileBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    public CommitFileBuilder WithRandomData()
    {
        var extensions = new[] { ".cs", ".java", ".js", ".py", ".ts" };
        var extension = _faker.PickRandom(extensions);
        
        _path = $"{_faker.System.DirectoryPath()}/{_faker.System.FileName(extension)}";
        _addedLines = _faker.Random.Int(1, 200);
        _removedLines = _faker.Random.Int(0, 50);
        _language = DetermineLanguageFromExtension(extension);
        _content = GenerateCodeContent(_language);
        
        return this;
    }

    public CommitFileBuilder WithAddedLines(int addedLines)
    {
        _addedLines = addedLines;
        return this;
    }

    public CommitFileBuilder WithRemovedLines(int removedLines)
    {
        _removedLines = removedLines;
        return this;
    }

    public CommitFileBuilder WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    public CommitFileBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public CommitFileBuilder WithRating(CleanCodeRating rating)
    {
        _rating = rating;
        return this;
    }

    public CommitFileBuilder WithRandomRating()
    {
        _rating = CleanCodeRatingBuilder.Create().WithRandomData().Build();
        return this;
    }

    public CommitFileBuilder WithSuggestions(params Suggestion[] suggestions)
    {
        _suggestions.AddRange(suggestions);
        return this;
    }

    public CommitFileBuilder WithRandomSuggestions(int count = 2)
    {
        for (int i = 0; i < count; i++)
        {
            var suggestion = SuggestionBuilder.Create()
                .WithRandomData()
                .WithFileReference(_path)
                .Build();
            _suggestions.Add(suggestion);
        }
        return this;
    }

    public CommitFile Build()
    {
        if (string.IsNullOrEmpty(_path))
            _path = $"{_faker.System.DirectoryPath()}/{_faker.System.FileName(".cs")}";
        
        if (string.IsNullOrEmpty(_content))
            _content = GenerateCodeContent(_language);

        var file = new CommitFile(_path, _addedLines, _removedLines, _language, _content);

        if (_rating != null)
        {
            file.SetAnalysis(_rating, _suggestions);
        }

        return file;
    }

    private string DetermineLanguageFromExtension(string extension) => extension switch
    {
        ".cs" => "C#",
        ".java" => "Java",
        ".js" => "JavaScript",
        ".py" => "Python",
        ".ts" => "TypeScript",
        _ => "Unknown"
    };

    private string GenerateCodeContent(string language) => language switch
    {
        "C#" => GenerateCSharpCode(),
        "Java" => GenerateJavaCode(),
        "JavaScript" => GenerateJavaScriptCode(),
        "Python" => GeneratePythonCode(),
        _ => _faker.Lorem.Paragraphs(3)
    };

    private string GenerateCSharpCode()
    {
        var companyName = _faker.Company.CompanyName().Replace(" ", "").Replace("-", "").Replace(".", "");
        var className = _faker.Name.LastName().Replace(" ", "");
        var methodName = ToTitleCase(_faker.Hacker.Verb());
        var variableName = _faker.Hacker.Noun().Replace(" ", "").Replace("-", "");
        
        return $@"using System;

namespace {companyName}
{{
    public class {className}Service
    {{
        public void {methodName}()
        {{
            // {_faker.Hacker.Phrase()}
            var {variableName} = ""{_faker.Lorem.Word()}"";
            Console.WriteLine({variableName});
        }}
    }}
}}";
    }

    private string GenerateJavaCode()
    {
        var packageName = _faker.Company.CompanyName().ToLower().Replace(" ", "").Replace("-", "").Replace(".", "");
        var className = _faker.Name.LastName().Replace(" ", "");
        var methodName = _faker.Hacker.Verb().Replace(" ", "").Replace("-", "");
        var variableName = _faker.Hacker.Noun().Replace(" ", "").Replace("-", "");
        
        return $@"package com.{packageName};

public class {className}Service {{
    public void {methodName}() {{
        // {_faker.Hacker.Phrase()}
        String {variableName} = ""{_faker.Lorem.Word()}"";
        System.out.println({variableName});
    }}
}}";
    }

    private string GenerateJavaScriptCode()
    {
        var functionName = _faker.Hacker.Verb().Replace(" ", "").Replace("-", "");
        var variableName = _faker.Hacker.Noun().Replace(" ", "").Replace("-", "");
        
        return $@"// {_faker.Hacker.Phrase()}
function {functionName}() {{
    const {variableName} = '{_faker.Lorem.Word()}';
    console.log({variableName});
}}

module.exports = {{ {functionName} }};";
    }

    private string GeneratePythonCode()
    {
        var functionName = _faker.Hacker.Verb().Replace(" ", "_").Replace("-", "_").ToLower();
        var variableName = _faker.Hacker.Noun().Replace(" ", "_").Replace("-", "_").ToLower();
        
        return $@"# {_faker.Hacker.Phrase()}
def {functionName}():
    {variableName} = '{_faker.Lorem.Word()}'
    print({variableName})

if __name__ == '__main__':
    {functionName}()";
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}
