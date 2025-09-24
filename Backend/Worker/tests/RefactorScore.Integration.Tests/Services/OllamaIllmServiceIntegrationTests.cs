using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Docker.DotNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RefactorScore.Application.Services;
using RefactorScore.Integration.Tests.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using RefactorScore.Domain.Services;
using RefactorScore.Domain.Tests.Builders;
using Xunit;

namespace RefactorScore.Integration.Tests.Services;

public class OllamaIllmServiceIntegrationTests : IntegrationTestBase
{
    private readonly IContainer _ollamaContainer;
    private ILLMService _llmService;
    private readonly HttpClient _httpClient;
    private ILogger<OllamaIllmService> _logger;
    private readonly string _tempDirectory;

    public OllamaIllmServiceIntegrationTests()
    {
        _ollamaContainer = CreateOllamaContainer();
        _httpClient = new HttpClient();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RefactorScore_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }
    
    private IContainer CreateOllamaContainer()
    {
        return new ContainerBuilder()
            .WithImage("ollama/ollama:latest")
            .WithPortBinding(11434, true)
            .WithEnvironment("OLLAMA_HOST", "0.0.0.0")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(11434).ForPath("/")))
            .Build();
    }
    

    private async Task PullTestModel()
    {
        try
        {
            var pullUrl = $"http://localhost:{_ollamaContainer.GetMappedPublicPort(11434)}/api/pull";
        
            var modelName = "tinyllama";
        
            var pullRequest = new { name = modelName };
            var json = System.Text.Json.JsonSerializer.Serialize(pullRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
            Console.WriteLine($"📥 Baixando modelo {modelName}...");
            var response = await _httpClient.PostAsync(pullUrl, content);
        
            if (response.IsSuccessStatusCode)
            {
                await Task.Delay(30000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pull test model, tests might use default model");
        }
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        _logger = ServiceProvider.GetRequiredService<ILogger<OllamaIllmService>>();
        
        await _ollamaContainer.StartAsync();
        
        await Task.Delay(5000);
        
        await PullTestModel();
        
        var configuration = CreateConfiguration();
        var ollamaUrl = $"http://localhost:{_ollamaContainer.GetMappedPublicPort(11434)}";
        _llmService = new OllamaIllmService(_logger, _httpClient, ollamaUrl, configuration);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithRealOllama_ShouldReturnValidAnalysis()
    {
        // Arrange
        var codeContent = @"
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }";
        var filePath = await CreateTempFileAsync("Calculator.cs", codeContent);

        // Act
        var result = await _llmService.AnalyzeFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().BeInRange(1, 10);
        result.FunctionScore.Should().BeInRange(1, 10);
        result.CommentScore.Should().BeInRange(1, 10);
        result.CohesionScore.Should().BeInRange(1, 10);
        result.DeadCodeScore.Should().BeInRange(1, 10);
        result.Justifications.Should().NotBeNull();
        result.Suggestions.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithRealOllama_ShouldReturnValidSuggestions()
    {
        // Arrange
        var codeContent = @"
            public class UserService
            {
                public void ProcessUser(string name, string email)
                {
                    // No validation
                    var user = new User(name, email);
                    SaveToDatabase(user);
                }
                
                private void SaveToDatabase(User user)
                {
                    // Direct database access without error handling
                    Database.Save(user);
                }
            }";
        var rating = new CleanCodeRatingBuilder().Build();

        var result = await _llmService.GenerateSuggestionsAsync(codeContent, rating);

        result.Should().NotBeNull("Service should return a result even if empty");
        
        if (result.Any())
        {
            foreach (var suggestion in result)
            {
                suggestion.Title.Should().NotBeNullOrWhiteSpace();
                suggestion.Description.Should().NotBeNullOrWhiteSpace();
            }
        }
    }


    [Fact]
    public async Task OllamaContainer_ShouldBeHealthyAndResponsive()
    {
        // Arrange
        var healthCheckUrl = $"http://localhost:{_ollamaContainer.GetMappedPublicPort(11434)}";

        // Act
        var response = await _httpClient.GetAsync(healthCheckUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ollama container should be healthy");
    }

    private IConfiguration CreateConfiguration()
    {
        var config = Substitute.For<IConfiguration>();
        config["Ollama:BaseUrl"].Returns($"http://localhost:{_ollamaContainer.GetMappedPublicPort(11434)}");
        
        // Usar sempre tinyllama para testes (mais rápido e confiável)
        config["Ollama:Model"].Returns("tinyllama");
        
        return config;
    }

    private async Task<string> CreateTempFileAsync(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    private string GenerateLargeCodeFile()
    {
        var lines = new List<string>
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Linq;",
            "",
            "namespace RefactorScore.Test",
            "{",
            "    public class LargeClass",
            "    {"
        };

        // Generate 50 methods
        for (int i = 0; i < 50; i++)
        {
            lines.AddRange(new[]
            {
                $"        public void Method{i}()",
                "        {",
                $"            var data = \"Method {i} implementation\";",
                "            Console.WriteLine(data);",
                $"            ProcessData{i}(data);",
                "        }",
                "",
                $"        private void ProcessData{i}(string data)",
                "        {",
                "            if (string.IsNullOrEmpty(data)) return;",
                "            var result = data.ToUpper();",
                "            Console.WriteLine(result);",
                "        }",
                ""
            });
        }

        lines.AddRange(new[] { "    }", "}" });
        
        return string.Join(Environment.NewLine, lines);
    }

    public new async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        
        // Cleanup temp directory
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to cleanup temp directory: {TempDirectory}", _tempDirectory);
        }
        
        if (_ollamaContainer != null)
        {
            await _ollamaContainer.StopAsync();
            await _ollamaContainer.DisposeAsync();
        }
        
        await base.DisposeAsync();
    }
}
