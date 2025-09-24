using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using RefactorScore.Application.Services;
using RefactorScore.Domain.Tests.Builders;
using Xunit;

namespace RefactorScore.Application.Tests.Services;

public class OllamaIllmServiceUnitTests
{
    private readonly ILogger<OllamaIllmService> _logger;
    private readonly IConfiguration _configuration;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly string _ollamaUrl = "http://localhost:11434";

    public OllamaIllmServiceUnitTests()
    {
        _logger = Substitute.For<ILogger<OllamaIllmService>>();
        _configuration = CreateConfiguration();
        _mockHttp = new MockHttpMessageHandler();
    }

    private IConfiguration CreateConfiguration()
    {
        var config = Substitute.For<IConfiguration>();
        config["Ollama:Model"].Returns("tinyllama");
        return config;
    }

    private OllamaIllmService CreateService()
    {
        var httpClient = _mockHttp.ToHttpClient();
        return new OllamaIllmService(_logger, httpClient, _ollamaUrl, _configuration);
    }

    #region AnalyzeFileAsync Tests

    [Fact]
    public async Task AnalyzeFileAsync_WithValidResponse_ShouldReturnCorrectAnalysis()
    {
        // Arrange
        var validJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10, \"justifications\": {\"VariableNaming\": \"Good names\", \"FunctionSizes\": \"Appropriate sizes\"}}"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validJsonResponse);
        
        var service = CreateService();
        
        // Act
        var result = await service.AnalyzeFileAsync("test code");
        
        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(8);
        result.FunctionScore.Should().Be(7);
        result.CommentScore.Should().Be(9);
        result.CohesionScore.Should().Be(8);
        result.DeadCodeScore.Should().Be(10);
        result.Justifications.Should().ContainKey("VariableNaming");
        result.Justifications["VariableNaming"].Should().Be("Good names");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithMalformedJson_ShouldReturnDefaultValues()
    {
        // Arrange
        var malformedJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10, \"justifications\": {\"VariableNaming\": \"Good names\", \"FunctionSizes\": \"Appropriate sizes\""
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", malformedJsonResponse);
        
        var service = CreateService();
        
        // Act
        var result = await service.AnalyzeFileAsync("test code");
        
        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(5); // Default value
        result.FunctionScore.Should().Be(5);
        result.CommentScore.Should().Be(5);
        result.CohesionScore.Should().Be(5);
        result.DeadCodeScore.Should().Be(5);
        result.Justifications.Should().ContainKey("VariableNaming");
        result.Justifications["VariableNaming"].Should().Be("Análise não disponível");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithNoResponseProperty_ShouldThrowException()
    {
        // Arrange
        var noResponseJsonResponse = """
        {
            "model": "tinyllama",
            "created_at": "2023-01-01T00:00:00Z"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", noResponseJsonResponse);
        
        var service = CreateService();
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyResponse_ShouldThrowException()
    {
        // Arrange
        var emptyResponseJsonResponse = """
        {
            "response": ""
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", emptyResponseJsonResponse);
        
        var service = CreateService();
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithHttpError_ShouldThrowException()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server Error");
        
        var service = CreateService();
        
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithIncompleteJson_ShouldReturnDefaults()
    {
        // Arrange
        var incompleteJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7}"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", incompleteJsonResponse);
        
        var service = CreateService();
        
        // Act
        var result = await service.AnalyzeFileAsync("test code");
        
        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(5); // Falls back to default due to incomplete JSON
    }

    #endregion

    #region GenerateSuggestionsAsync Tests

    [Fact]
    public async Task GenerateSuggestionsAsync_WithValidArray_ShouldReturnSuggestions()
    {
        // Arrange
        var validSuggestionsResponse = """
        {
            "response": "[{\"title\": \"Improve variable naming\", \"description\": \"Use more descriptive names\", \"priority\": \"High\", \"type\": \"CodeStyle\", \"difficulty\": \"Easy\", \"studyResources\": [\"Clean Code\"]}]"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validSuggestionsResponse);
        
        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();
        
        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Improve variable naming");
        result[0].Description.Should().Be("Use more descriptive names");
        result[0].Priority.Should().Be("High");
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithMalformedJson_ShouldReturnEmptyList()
    {
        // Arrange
        var malformedSuggestionsResponse = """
        {
            "response": "[{\"title\": \"Improve variable naming\", \"description\": \"Use more descriptive names\", \"priority\": \"High\""
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", malformedSuggestionsResponse);
        
        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();
        
        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithHttpError_ShouldReturnEmptyList()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond(HttpStatusCode.InternalServerError, "text/plain", "Server Error");
        
        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();
        
        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithEmptyArray_ShouldReturnEmptyList()
    {
        // Arrange
        var emptySuggestionsResponse = """
        {
            "response": "[]"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", emptySuggestionsResponse);
        
        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();
        
        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithTooManySuggestions_ShouldLimitToFive()
    {
        // Arrange
        var manySuggestionsResponse = """
        {
            "response": "[{\"title\": \"Suggestion 1\", \"description\": \"Desc 1\"}, {\"title\": \"Suggestion 2\", \"description\": \"Desc 2\"}, {\"title\": \"Suggestion 3\", \"description\": \"Desc 3\"}, {\"title\": \"Suggestion 4\", \"description\": \"Desc 4\"}, {\"title\": \"Suggestion 5\", \"description\": \"Desc 5\"}, {\"title\": \"Suggestion 6\", \"description\": \"Desc 6\"}, {\"title\": \"Suggestion 7\", \"description\": \"Desc 7\"}]"
        }
        """;
        
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", manySuggestionsResponse);
        
        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();
        
        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5); // Limited to 5 suggestions
    }

    #endregion
    
    
    #region Advanced Error Scenarios

    [Fact]
    public async Task AnalyzeFileAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond(async () =>
                 {
                     await Task.Delay(TimeSpan.FromMinutes(5)); // Simula timeout
                     return new HttpResponseMessage(HttpStatusCode.OK);
                 });

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithConnectionRefused_ShouldThrowHttpRequestException()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Throw(new HttpRequestException("Connection refused"));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_With429RateLimit_ShouldThrowHttpRequestException()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond(HttpStatusCode.TooManyRequests, "text/plain", "Rate limit exceeded");

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeFileAsync("test code"));
    }

    [Fact]
    public async Task AnalyzeFileAsync_With503ServiceUnavailable_ShouldThrowHttpRequestException()
    {
        // Arrange
        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond(HttpStatusCode.ServiceUnavailable, "text/plain", "Service temporarily unavailable");

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeFileAsync("test code"));
    }

    #endregion

    #region JSON Edge Cases

    [Fact]
    public async Task AnalyzeFileAsync_WithDeeplyNestedJson_ShouldHandleGracefully()
    {
        // Arrange - JSON com estrutura aninhada válida mas complexa
        var deeplyNestedJson = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10, \"justifications\": {\"VariableNaming\": \"Good names\", \"FunctionSizes\": \"Appropriate sizes\"}, \"nested\": {\"level1\": {\"level2\": {\"level3\": {\"level4\": {\"level5\": {\"level6\": {\"level7\": {\"level8\": {\"level9\": {\"level10\": \"deep\"}}}}}}}}}}}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", deeplyNestedJson);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync("test code");

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(8); // Should parse correctly despite deep nesting
        result.FunctionScore.Should().Be(7);
        result.CommentScore.Should().Be(9);
        result.CohesionScore.Should().Be(8);
        result.DeadCodeScore.Should().Be(10);
        result.Justifications.Should().ContainKey("VariableNaming");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10, \"justifications\": {\"VariableNaming\": \"Nomes com acentuação: ção, ã, é, ü, 中文, 🚀\", \"FunctionSizes\": \"Tamanhos apropriados\"}}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", unicodeJsonResponse);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync("test code");

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(8);
        result.Justifications["VariableNaming"].Should().Contain("acentuação");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEscapeCharacters_ShouldParseCorrectly()
    {
        // Arrange
        var escapedJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10, \"justifications\": {\"VariableNaming\": \"Strings with \\\"quotes\\\" and \\n newlines \\t tabs\", \"FunctionSizes\": \"Appropriate sizes\"}}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", escapedJsonResponse);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync("test code");

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(8);
        result.FunctionScore.Should().Be(7);
        result.CommentScore.Should().Be(9);
        result.CohesionScore.Should().Be(8);
        result.DeadCodeScore.Should().Be(10);
        result.Justifications["VariableNaming"].Should().Contain("quotes");
        result.Justifications["VariableNaming"].Should().Contain("newlines");
        result.Justifications["VariableNaming"].Should().Contain("tabs");
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithExtremelyLargeJson_ShouldHandleMemoryEfficiently()
    {
        // Arrange - JSON com 1000 sugestões
        var largeSuggestions = new StringBuilder("[");
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) largeSuggestions.Append(",");
            largeSuggestions.Append($"{{\"title\": \"Suggestion {i}\", \"description\": \"Description {i}\"}}");
        }
        largeSuggestions.Append("]");

        var largeJsonResponse = $$"""
        {
            "response": "{{largeSuggestions.ToString().Replace("\"", "\\\"")}}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", largeJsonResponse);

        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();

        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5); // Should be limited to 5
    }

    #endregion

    #region Configuration Edge Cases

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OllamaIllmService(null, _mockHttp.ToHttpClient(), "http://localhost:11434", _configuration));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OllamaIllmService(_logger, null, "http://localhost:11434", _configuration));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OllamaIllmService(_logger, _mockHttp.ToHttpClient(), "http://localhost:11434", null));
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithNullFileContent_ShouldHandleGracefully()
    {
        // Arrange
        var validJsonResponse = """
        {
            "response": "{\"variableScore\": 5, \"functionScore\": 5, \"commentScore\": 5, \"cohesionScore\": 5, \"deadCodeScore\": 5}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validJsonResponse);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(5);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyFileContent_ShouldHandleGracefully()
    {
        // Arrange
        var validJsonResponse = """
        {
            "response": "{\"variableScore\": 5, \"functionScore\": 5, \"commentScore\": 5, \"cohesionScore\": 5, \"deadCodeScore\": 5}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validJsonResponse);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync("");

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(5);
    }

    #endregion

    #region Security & Injection Tests

    [Fact]
    public async Task GenerateSuggestionsAsync_WithJsonInjectionAttempt_ShouldHandleSafely()
    {
        // Arrange - Tentativa de injection
        var injectionJsonResponse = """
        {
            "response": "[{\"title\": \"<script>alert('xss')</script>\", \"description\": \"'; DROP TABLE users; --\"}]"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", injectionJsonResponse);

        var service = CreateService();
        var rating = new CleanCodeRatingBuilder().Build();

        // Act
        var result = await service.GenerateSuggestionsAsync("test code", rating);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Title.Should().Contain("<script>"); // Should preserve as text, not execute
        result[0].Description.Should().Contain("DROP TABLE"); // Should preserve as text
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithVeryLargeFileContent_ShouldHandleEfficiently()
    {
        // Arrange - Arquivo de 1MB
        var largeFileContent = new string('x', 1024 * 1024);
        
        var validJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validJsonResponse);

        var service = CreateService();

        // Act
        var result = await service.AnalyzeFileAsync(largeFileContent);

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().Be(8);
    }

    #endregion

    #region Concurrency & Performance Tests

    [Fact]
    public async Task AnalyzeFileAsync_WithConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var validJsonResponse = """
        {
            "response": "{\"variableScore\": 8, \"functionScore\": 7, \"commentScore\": 9, \"cohesionScore\": 8, \"deadCodeScore\": 10}"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validJsonResponse);

        var service = CreateService();

        // Act - 10 requests simultâneas
        var tasks = Enumerable.Range(0, 10)
            .Select(i => service.AnalyzeFileAsync($"test code {i}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.VariableScore == 8);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithNullRating_ShouldHandleGracefully()
    {
        // Arrange
        var validSuggestionsResponse = """
        {
            "response": "[{\"title\": \"Test suggestion\", \"description\": \"Test description\"}]"
        }
        """;

        _mockHttp.When($"{_ollamaUrl}/api/generate")
                 .Respond("application/json", validSuggestionsResponse);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.GenerateSuggestionsAsync("test code", null));
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mockHttp?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}