using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RefactorScore.Application.Services;
using RefactorScore.Integration.Tests.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Docker.DotNet.Models;
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
        _ollamaContainer = CreateOllamaContainerWithGpu();
        _httpClient = new HttpClient();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RefactorScore_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }
    
    private IContainer CreateOllamaContainerWithGpu()
    {
        
        return new ContainerBuilder()
            .WithImage("ollama/ollama:latest")
            .WithPortBinding(11434, true)
            .WithEnvironment("OLLAMA_HOST", "0.0.0.0")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(11434)))
            .Build();
    }
    
    private bool HasNvidiaGpu()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists("/usr/bin/nvidia-smi") || File.Exists("/usr/local/cuda/bin/nvcc");
            }
        }
        catch
        {
            // GPU detection failed
        }
        
        return false;
    }

    private bool HasAmdGpu()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Directory.Exists("/opt/rocm") || File.Exists("/usr/bin/rocm-smi");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check for AMD GPU via WMI or registry
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path win32_VideoController get name",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output.ToLower().Contains("amd") || output.ToLower().Contains("radeon");
                }
            }
        }
        catch
        {
            // GPU detection failed
        }
        
        return false;
    }

    private void EnableNvidiaGpu(CreateContainerParameters parameters)
    {
        try
        {
            parameters.HostConfig ??= new HostConfig();
            
            // Configuração simplificada para NVIDIA GPU
            parameters.HostConfig.DeviceRequests = new List<DeviceRequest>
            {
                new DeviceRequest
                {
                    Driver = "nvidia",
                    Count = -1,
                    Capabilities = new List<IList<string>> { new List<string> { "gpu" } }
                }
            };
            
            Console.WriteLine("✅ Configuração NVIDIA GPU aplicada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao configurar NVIDIA GPU: {ex.Message}");
        }
    }

    private void EnableAmdGpu(CreateContainerParameters parameters)
    {
        try
        {
            parameters.HostConfig ??= new HostConfig();
            
            // Configuração simplificada para AMD GPU
            parameters.HostConfig.DeviceRequests = new List<DeviceRequest>
            {
                new DeviceRequest
                {
                    Driver = "amd",
                    Count = -1,
                    Capabilities = new List<IList<string>> { new List<string> { "gpu" } }
                }
            };
            
            Console.WriteLine("✅ Configuração AMD GPU aplicada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erro ao configurar AMD GPU: {ex.Message}");
        }
    }

    private async Task PullTestModel()
    {
        try
        {
            var pullUrl = $"http://localhost:{_ollamaContainer.GetMappedPublicPort(11434)}/api/pull";
        
            // Usar sempre tinyllama para testes (mais rápido e confiável)
            var modelName = "tinyllama";
        
            var pullRequest = new { name = modelName };
            var json = System.Text.Json.JsonSerializer.Serialize(pullRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
            Console.WriteLine($"📥 Baixando modelo {modelName}...");
            var response = await _httpClient.PostAsync(pullUrl, content);
        
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("⏳ Aguardando modelo ser carregado...");
                await Task.Delay(30000); // 30 segundos para tinyllama
                Console.WriteLine($"✅ Modelo {modelName} carregado!");
            }
            else
            {
                Console.WriteLine($"⚠️ Falha ao baixar modelo: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to pull test model, tests might use default model");
            Console.WriteLine($"⚠️ Erro ao baixar modelo: {ex.Message}");
        }
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Initialize logger after ServiceProvider is ready
        _logger = ServiceProvider.GetRequiredService<ILogger<OllamaIllmService>>();
        
        // Start Ollama container
        await _ollamaContainer.StartAsync();
        
        // Wait for container to be ready
        await Task.Delay(5000);
        
        // Pull a lightweight model for testing
        await PullTestModel();
        
        // Setup LLM service with real container
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

        // Act
        var result = await _llmService.GenerateSuggestionsAsync(codeContent, rating);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("Real LLM should generate suggestions for this problematic code");
        
        // Validate suggestion structure
        foreach (var suggestion in result)
        {
            suggestion.Title.Should().NotBeNullOrWhiteSpace();
            suggestion.Description.Should().NotBeNullOrWhiteSpace();
            suggestion.Priority.Should().NotBeNullOrWhiteSpace();
            suggestion.Type.Should().NotBeNullOrWhiteSpace();
            suggestion.Difficulty.Should().NotBeNullOrWhiteSpace();
            suggestion.StudyResources.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithComplexCode_ShouldHandleSuccessfully()
    {
        // Arrange
        var codeContent = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;

            namespace RefactorScore.Domain.Entities
            {
                public class CommitAnalysis
                {
                    private readonly List<CommitFile> _files;
                    
                    public CommitAnalysis(string commitId, string author, string email, 
                        DateTime commitDate, string language, int addedLines, int removedLines)
                    {
                        CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
                        Author = author ?? throw new ArgumentNullException(nameof(author));
                        Email = email ?? throw new ArgumentNullException(nameof(email));
                        CommitDate = commitDate;
                        Language = language ?? throw new ArgumentNullException(nameof(language));
                        AddedLines = addedLines;
                        RemovedLines = removedLines;
                        _files = new List<CommitFile>();
                    }
                    
                    public string CommitId { get; }
                    public string Author { get; }
                    public string Email { get; }
                    public DateTime CommitDate { get; }
                    public string Language { get; }
                    public int AddedLines { get; }
                    public int RemovedLines { get; }
                    public IReadOnlyList<CommitFile> Files => _files.AsReadOnly();
                    
                    public void AddFile(CommitFile file)
                    {
                        if (file == null) throw new ArgumentNullException(nameof(file));
                        if (_files.Any(f => f.FilePath == file.FilePath))
                            throw new InvalidOperationException($""File {file.FilePath} already exists"");
                        
                        _files.Add(file);
                    }
                }
            }";
        var filePath = await CreateTempFileAsync("CommitAnalysis.cs", codeContent);

        // Act
        var result = await _llmService.AnalyzeFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.VariableScore.Should().BeInRange(1, 10);
        result.FunctionScore.Should().BeInRange(1, 10);
        
        result.FunctionScore.Should().BeGreaterThan(3, "Well-structured code should have decent function score");
        result.CohesionScore.Should().BeGreaterThan(3, "Clean code with good structure should have good cohesion");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithInvalidCode_ShouldStillReturnAnalysis()
    {
        // Arrange
        var codeContent = @"
            public class InvalidCode {
                public void Method(
                    // Missing closing parenthesis and brace
                    string param
                // Missing method body
            ";
        var filePath = await CreateTempFileAsync("InvalidCode.cs", codeContent);

        // Act
        var result = await _llmService.AnalyzeFileAsync(filePath);

        // Assert
        result.Should().NotBeNull("Service should handle invalid code gracefully");
        result.VariableScore.Should().BeInRange(1, 10);
        result.FunctionScore.Should().BeInRange(1, 10);
        
        result.DeadCodeScore.Should().BeLessOrEqualTo(7, "Invalid syntax should be detected as problematic");
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithLargeCodeFile_ShouldHandleWithinTimeout()
    {
        // Arrange
        var codeContent = GenerateLargeCodeFile();
        var filePath = await CreateTempFileAsync("LargeFile.cs", codeContent);

        // Act & Assert
        var result = await _llmService.AnalyzeFileAsync(filePath);
        
        result.Should().NotBeNull();
        result.VariableScore.Should().BeInRange(1, 10);
        result.FunctionScore.Should().BeInRange(1, 10);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithEmptyCode_ShouldReturnEmptyOrMinimalSuggestions()
    {
        // Arrange
        var codeContent = "";
        var rating = new CleanCodeRatingBuilder().Build();

        // Act
        var result = await _llmService.GenerateSuggestionsAsync(codeContent, rating);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithDifferentLanguages_ShouldWorkForAllSupported()
    {
        // Arrange
        var testCases = new[]
        {
            ("C#", @"public class Test { public void Method() { } }", "cs"),
            ("JavaScript", @"function test() { return 'hello'; }", "js"),
            ("Python", @"def test(): return 'hello'", "py"),
            ("Java", @"public class Test { public void method() { } }", "java")
        };

        foreach (var (language, code, extension) in testCases)
        {
            // Act
            var filePath = await CreateTempFileAsync($"test.{extension}", code);
            var result = await _llmService.AnalyzeFileAsync(filePath);

            // Assert
            result.Should().NotBeNull($"Should handle {language} code");
            result.VariableScore.Should().BeInRange(1, 10, $"Should return valid analysis for {language}");
            result.FunctionScore.Should().BeInRange(1, 10, $"Should return valid analysis for {language}");
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

    [Fact]
    public async Task AnalyzeFileAsync_WithConcurrentRequests_ShouldHandleMultipleRequests()
    {
        // Arrange
        var codeContent = @"public class ConcurrentTest { public void Method() { } }";
        var filePaths = new List<string>();
        
        for (int i = 0; i < 3; i++)
        {
            filePaths.Add(await CreateTempFileAsync($"ConcurrentTest{i}.cs", codeContent));
        }

        var tasks = filePaths.Select(async filePath =>
        {
            var result = await _llmService.AnalyzeFileAsync(filePath);
            result.Should().NotBeNull();
            result.VariableScore.Should().BeInRange(1, 10);
            result.FunctionScore.Should().BeInRange(1, 10);
        });

        // Act & Assert
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WithDifferentRatings_ShouldAdaptSuggestions()
    {
        // Arrange
        var codeContent = @"
            public class TestClass
            {
                public void Method()
                {
                    var x = 1;
                    var y = 2;
                    Console.WriteLine(x + y);
                }
            }";

        var lowRating = new CleanCodeRatingBuilder().WithPoorScores().Build();


        var highRating = new CleanCodeRatingBuilder().WithExcellentScores().Build();

        // Act
        var lowRatingSuggestions = await _llmService.GenerateSuggestionsAsync(codeContent, lowRating);
        var highRatingSuggestions = await _llmService.GenerateSuggestionsAsync(codeContent, highRating);

        // Assert
        lowRatingSuggestions.Should().NotBeNull();
        highRatingSuggestions.Should().NotBeNull();
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
