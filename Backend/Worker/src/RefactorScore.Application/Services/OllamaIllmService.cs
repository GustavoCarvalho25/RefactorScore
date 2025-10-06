using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RefactorScore.Domain.Models;
using RefactorScore.Domain.Services;
using RefactorScore.Domain.ValueObjects;
using RefactorScore.Infrastructure.Configurations;

namespace RefactorScore.Application.Services;

public class OllamaIllmService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaIllmService> _logger;
    private readonly string _ollamaUrl;
    private readonly IConfiguration _configuration;
    private readonly OllamaSettings _ollamaSettings;

    public OllamaIllmService(ILogger<OllamaIllmService> logger, HttpClient httpClient, IConfiguration configuration, IOptions<OllamaSettings> ollamaOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (ollamaOptions == null)
            throw new ArgumentNullException(nameof(ollamaOptions));
            
        _ollamaSettings = ollamaOptions.Value ?? throw new ArgumentNullException(nameof(ollamaOptions.Value));
        _ollamaUrl = _ollamaSettings.BaseUrl ?? throw new ArgumentNullException(nameof(_ollamaSettings.BaseUrl));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<LLMAnalysisResult> AnalyzeFileAsync(string fileContent)
    {
        try
        {
            var prompt = BuildAnalysisPrompt(fileContent);
            var response = await CallOllamaAsync(prompt);
            return await ParseAnalysisResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file with LLM");
            throw;
        }
    }

    public async Task<List<LLMSuggestion>> GenerateSuggestionsAsync(string fileContent, CleanCodeRating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);

        var prompt = BuildSuggestionsPrompt(fileContent, rating);

        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("Retrying suggestions request (attempt {Attempt}/{MaxAttempts})", attempt, maxAttempts);
                }

                var response = await CallOllamaAsync(prompt);
                return await ParseSuggestionsResponse(response);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Suggestions request timed out on attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                if (attempt == maxAttempts)
                    break;
            }
            catch (HttpRequestException hex)
            {
                _logger.LogWarning(hex, "HTTP error during suggestions on attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                if (attempt == maxAttempts)
                    break;
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogWarning(oce, "Suggestions request canceled on attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                if (attempt == maxAttempts)
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating suggestions (attempt {Attempt}/{MaxAttempts})", attempt, maxAttempts);
                if (attempt == maxAttempts)
                    break;
            }

            var delaySeconds = (int)Math.Pow(2, attempt);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        return new List<LLMSuggestion>();
    }

    private string BuildSuggestionsPrompt(string fileContent, CleanCodeRating rating)
    {
        return $@"
                Gere entre 3 e 5 sugestões objetivas para melhorar o código abaixo, considerando as notas atuais de Clean Code.
                Responda SOMENTE com um array JSON válido. Não inclua texto fora do JSON.
                Cada item do array deve conter exatamente estas chaves: title, description, priority, type, difficulty, studyResources.
                As prioridades permitidas: Low, Medium, High. O campo studyResources é uma lista de textos.

                Índice de capítulos (Clean Code) para referência em studyResources:
                1 - Código Limpo; 2 - Nomes significativos; 3 - Funções; 4 - Comentários; 5 - Formatação; 6 - Objetos e Estruturas de Dados; 7 - Tratamento de Erro; 8 - Limites;
                9 - Testes de Unidade; 10 - Classes; 11 - Sistemas; 12 - Emergência; 13 - Concorrência; 14 - Refinamento Sucessivo; 15 - Características Internas do JUnit;
                16 - Refatorando o SerialDate; 17 - Odores e Heurísticas.
                Ao sugerir recursos de estudo, retorne o(s) capítulo(s) pertinente(s) por nome, por exemplo: ""Capítulo 2 - Nomes significativos"".

                Notas atuais (1-10):
                - Variable Naming: {rating.VariableNaming}
                - Function Sizes: {rating.FunctionSizes}
                - No Needs Comments: {rating.NoNeedsComments}
                - Method Cohesion: {rating.MethodCohesion}
                - Dead Code: {rating.DeadCode}

                Código:
                {fileContent}

                Exemplo de formato (apenas estrutura):
                [
                  {{
                    ""title"": ""Melhorar nomenclatura de variáveis"",
                    ""description"": ""Use nomes descritivos e consistentes para variáveis e parâmetros"",
                    ""priority"": ""Medium"",
                    ""type"": ""CodeStyle"",
                    ""difficulty"": ""Easy"",
                    ""studyResources"": [""Capítulo 2 - Nomes significativos""]
                  }}
                ]";
    }


    private string BuildAnalysisPrompt(string fileContent)
    {
        return $@"
            Avalie o código abaixo como um analista sênior de Clean Code.
            Atribua notas inteiras de 1 a 10 para as chaves: variableScore, functionScore, commentScore, cohesionScore, deadCodeScore.
            Inclua um objeto justifications com justificativas textuais por critério usando EXATAMENTE as chaves: VariableNaming, FunctionSizes, NoNeedsComments, MethodCohesion, DeadCode.
            Responda SOMENTE com um JSON válido contendo exatamente estas chaves no objeto raiz.
            Se não tiver confiança para alguma nota, escolha o valor inteiro mais apropriado entre 1 e 10.

            Índice de capítulos (Clean Code) para referência futura:
            1 - Código Limpo; 2 - Nomes significativos; 3 - Funções; 4 - Comentários; 5 - Formatação; 6 - Objetos e Estruturas de Dados; 7 - Tratamento de Erro; 8 - Limites;
            9 - Testes de Unidade; 10 - Classes; 11 - Sistemas; 12 - Emergência; 13 - Concorrência; 14 - Refinamento Sucessivo; 15 - Características Internas do JUnit;
            16 - Refatorando o SerialDate; 17 - Odores e Heurísticas.

            Código:
            {fileContent}

            Exemplo de formato (apenas estrutura):
            {{
              ""variableScore"": 8,
              ""functionScore"": 7,
              ""commentScore"": 9,
              ""cohesionScore"": 8,
              ""deadCodeScore"": 10,
              ""justifications"": {{
                ""Variable"": ""Justificativa para nota"",
                ""Functions"": ""Justificativa para nota"",
                ""Comments"": ""Justificativa para nota"",
                ""Cohesion"": ""Justificativa para nota"",
                ""DeadCode"": ""Justificativa para nota""
              }}
            }}

            Regras obrigatórias:
            - Use somente números inteiros de 1 a 10.
            - Não inclua texto fora do JSON.
            - As chaves de justifications DEVEM ser exatamente as cinco acima.";
    }

    private async Task<string> CallOllamaAsync(string prompt)
    {
        var timeoutSeconds = _ollamaSettings.AnalysisTimeoutSeconds;
        var model = _ollamaSettings.Model;
        var request = new
        {
            model,
            prompt,
            format = "json",
            options = new {temperature = 0.2},
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var cts = new CancellationTokenSource();

        if (timeoutSeconds > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            
            if (_ollamaSettings.EnableDetailedLogging)
                _logger.LogInformation("HttpClient timeout configured to {TimeoutSeconds} seconds", timeoutSeconds);
        }

        try
        {
            var response = await _httpClient.PostAsync($"{_ollamaUrl}/api/generate", content, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
        
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var responseProperty))
            {
                _logger.LogWarning("No 'response' property found in LLM response, using default values");
                throw new InvalidOperationException("No 'response' property found in LLM response");
            }
        
            var llmResponse = responseProperty.GetString();
        
            if (string.IsNullOrEmpty(llmResponse))
            {
                _logger.LogWarning("Empty LLM response, using default values");
                throw new InvalidOperationException("Empty LLM response");
            }
        
            return llmResponse;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            var usedTimeout = timeoutSeconds > 0 ? timeoutSeconds : _ollamaSettings.TimeoutSeconds;
            _logger.LogWarning("LLM request timed out after {TimeoutSeconds} seconds", usedTimeout);
            throw new TimeoutException($"LLM request timed out after {usedTimeout} seconds");
        }
    }

    private async Task<LLMAnalysisResult> ParseAnalysisResponse(string response)
    {
        var jsonContent = ExtractJsonFromResponse(response);
    
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (TryParseAnalysisJson(jsonContent, out var result))
            {
                return result;
            }
        
            jsonContent = await FixJsonWithLlmAsync(jsonContent);
            if (jsonContent == null) break;
        }
    
        return GetDefaultAnalysisResult();
    }

    private string ExtractJsonFromResponse(string response)
    {
        try
        {
            _logger.LogDebug("Extracting JSON from LLM response: {Response}", response);
            
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning("Extracting JSON from LLM response: {Response}", response);
                _logger.LogWarning("No JSON found in LLM response");
                return "{}";
            }

            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            _logger.LogDebug("Extracted JSON: {Json}", jsonContent);
            
            return jsonContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting JSON from response: {Response}", response);
            return "{}";
        }
    }

    private LLMAnalysisResult GetDefaultAnalysisResult()
    {
        return new LLMAnalysisResult
        {
            VariableScore = 5,
            FunctionScore = 5,
            CommentScore = 5,
            CohesionScore = 5,
            DeadCodeScore = 5,
            Justifications = new Dictionary<string, string>
            {
                ["VariableNaming"] = "Análise não disponível",
                ["FunctionSizes"] = "Análise não disponível",
                ["NoNeedsComments"] = "Análise não disponível",
                ["MethodCohesion"] = "Análise não disponível",
                ["DeadCode"] = "Análise não disponível"
            }
        };
    }
    
    private async Task<List<LLMSuggestion>> ParseSuggestionsResponse(string response)
    {
        var jsonContent = ExtractSuggestionsJsonFromResponse(response);
    
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (TryParseSuggestionsJson(jsonContent, out var result))
            {
                return result;
            }
        
            jsonContent = await FixSuggestionsJsonWithLlmAsync(jsonContent);
            if (jsonContent == null) break;
        }
    
        return GetDefaultSuggestions();
    }
    
    private string ExtractSuggestionsJsonFromResponse(string response)
    {
        try
        {
            _logger.LogDebug("Extracting suggestions JSON from LLM response: {Response}", response);
            
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');
            
            if (jsonStart != -1 && jsonEnd != -1)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                _logger.LogDebug("Extracted suggestions JSON array: {Json}", jsonContent);
                return jsonContent;
            }
            
            jsonStart = response.IndexOf('{');
            jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart != -1 && jsonEnd != -1)
            {
                var singleObject = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var wrappedJson = $"[{singleObject}]";
                _logger.LogDebug("Extracted single suggestion object, wrapped in array: {Json}", wrappedJson);
                return wrappedJson;
            }
            
            _logger.LogWarning("No JSON found in suggestions response");
            return "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting suggestions JSON from response: {Response}", response);
            return "[]";
        }
    }

    private bool TryParseSuggestionsJson(string jsonContent, out List<LLMSuggestion> suggestions)
    {
        suggestions = null;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            
            suggestions = JsonSerializer.Deserialize<List<LLMSuggestion>>(jsonContent, options);
            
            if (suggestions == null)
            {
                _logger.LogWarning("Deserialized suggestions list is null or empty");
                return false;
            }
            
            if (!suggestions.Any())
            {
                _logger.LogInformation("Successfully parsed empty suggestions list");
                return true;
            }

            
            var validSuggestions = suggestions
                .Where(s => !string.IsNullOrWhiteSpace(s.Title) && !string.IsNullOrWhiteSpace(s.Description))
                .Take(5)
                .ToList();
            
            suggestions = validSuggestions;
            _logger.LogInformation("Successfully parsed {Count} suggestions", validSuggestions.Count);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Suggestions JSON parsing failed: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during suggestions JSON parsing: {Error}", ex.Message);
            return false;
        }
    }

    private async Task<string> FixSuggestionsJsonWithLlmAsync(string brokenJson, int maxRetries = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting suggestions JSON correction with LLM (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                var fixPrompt = BuildSuggestionsJsonFixPrompt(brokenJson);
                var correctedResponse = await CallOllamaAsync(fixPrompt);
                
                var jsonStart = correctedResponse.IndexOf('[');
                var jsonEnd = correctedResponse.LastIndexOf(']');
                
                if (jsonStart == -1 || jsonEnd == -1)
                {
                    _logger.LogWarning("LLM suggestions correction attempt {Attempt} did not return valid JSON array structure", attempt);
                    continue;
                }
                
                var correctedJson = correctedResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                _logger.LogDebug("LLM corrected suggestions JSON (attempt {Attempt}): {Json}", attempt, correctedJson);
                
                try
                {
                    JsonDocument.Parse(correctedJson);
                    _logger.LogInformation("LLM successfully corrected suggestions JSON on attempt {Attempt}", attempt);
                    return correctedJson;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM suggestions correction attempt {Attempt} still produced invalid JSON", attempt);
                    brokenJson = correctedJson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LLM suggestions JSON correction attempt {Attempt}", attempt);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
        
        _logger.LogError("Failed to correct suggestions JSON after {MaxRetries} attempts with LLM", maxRetries);
        return null;
    }

    private string BuildSuggestionsJsonFixPrompt(string brokenJson)
    {
        return $@"
            O JSON array abaixo está malformado e precisa ser corrigido. Por favor, corrija-o mantendo exatamente os mesmos dados, apenas corrigindo problemas de sintaxe como vírgulas extras, estrutura incorreta, etc.

            JSON com problemas:
            {brokenJson}

            Retorne APENAS o JSON array corrigido, sem explicações adicionais. O JSON deve ser um array válido com esta estrutura:
            [
              {{
                ""title"": ""texto"",
                ""description"": ""texto"",
                ""priority"": ""Medium"",
                ""type"": ""CodeStyle"",
                ""difficulty"": ""Easy"",
                ""studyResources"": [""recurso1"", ""recurso2""]
              }}
            ]";
    }

    private List<LLMSuggestion> GetDefaultSuggestions()
    {
        return new List<LLMSuggestion>
        {
            new LLMSuggestion
            {
                Title = "Revisar nomenclatura de variáveis",
                Description = "Verificar se os nomes das variáveis são descritivos e seguem convenções",
                Priority = "Medium",
                Type = "CodeStyle",
                Difficulty = "Easy",
                StudyResources = new List<string> { "Clean Code - Chapter 2: Meaningful Names" }
            },
            new LLMSuggestion
            {
                Title = "Analisar tamanho das funções",
                Description = "Verificar se as funções estão pequenas e focadas em uma única responsabilidade",
                Priority = "Medium",
                Type = "Structure",
                Difficulty = "Medium",
                StudyResources = new List<string> { "Clean Code - Chapter 3: Functions" }
            },
            new LLMSuggestion
            {
                Title = "Verificar necessidade de comentários",
                Description = "Avaliar se o código é auto-explicativo ou se precisa de comentários",
                Priority = "Low",
                Type = "Documentation",
                Difficulty = "Easy",
                StudyResources = new List<string> { "Clean Code - Chapter 4: Comments" }
            }
        };
    }
    private bool TryParseAnalysisJson(string jsonContent, out LLMAnalysisResult result)
    {
        result = null;
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            JsonElement scoreElement;
            if (root.TryGetProperty("score", out var scoreProperty))
            {
                scoreElement = scoreProperty;
                _logger.LogDebug("Found nested 'score' object");
            }
            else
            {
                scoreElement = root;
                _logger.LogDebug("Using root object directly");
            }

            int GetIntOrDefault(JsonElement el, string name, int def)
            {
                if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v))
                        return ClampScore(v);
                    // casos de string numérica
                    if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var vs))
                        return ClampScore(vs);
                }
                _logger.LogWarning("Missing or invalid '{Name}' in analysis JSON. Using default.", name);
                return def;
            }

            var variableScore = GetIntOrDefault(scoreElement, "variableScore", 5);
            var functionScore = GetIntOrDefault(scoreElement, "functionScore", 5);
            var commentScore = GetIntOrDefault(scoreElement, "commentScore", 5);
            var cohesionScore = GetIntOrDefault(scoreElement, "cohesionScore", 5);
            var deadCodeScore = GetIntOrDefault(scoreElement, "deadCodeScore", 5);

            result = new LLMAnalysisResult
            {
                VariableScore = variableScore,
                FunctionScore = functionScore,
                CommentScore = commentScore,
                CohesionScore = cohesionScore,
                DeadCodeScore = deadCodeScore
            };

            if (scoreElement.TryGetProperty("justifications", out var justifications))
            {
                foreach (var prop in justifications.EnumerateObject())
                {
                    result.Justifications[prop.Name] = prop.Value.GetString() ?? "";
                }
            }
            // Ensure all five justification keys exist for consistent downstream usage
            var requiredKeys = new[] { "VariableNaming", "FunctionSizes", "NoNeedsComments", "MethodCohesion", "DeadCode" };
            foreach (var key in requiredKeys)
            {
                if (!result.Justifications.ContainsKey(key))
                {
                    result.Justifications[key] = "Justificativa não fornecida";
                    _logger.LogWarning("Missing justification '{Key}' in analysis JSON. Filled with default.", key);
                }
            }

            _logger.LogInformation("Successfully parsed analysis: Variable={Variable}, Function={Function}, Comment={Comment}, Cohesion={Cohesion}, DeadCode={DeadCode}", 
                result.VariableScore, result.FunctionScore, result.CommentScore, result.CohesionScore, result.DeadCodeScore);

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "JSON parsing failed: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during JSON parsing: {Error}", ex.Message);
            return false;
        }
    }
    
    private int ClampScore(int score)
    {
        if (score < 1)
        {
            _logger.LogWarning("Score {Score} is below minimum (1), clamping to 1", score);
            return 1;
        }
        if (score > 10)
        {
            _logger.LogWarning("Score {Score} is above maximum (10), dividing by 10", score);
            score /= 10;
            
            if (score < 1)
            {
                _logger.LogWarning("Score {Score} is below minimum (1) after clamping, clamping to 1", score);
                return 1;
            }

            if (score > 10)
            {
                _logger.LogWarning("Score {Score} is above maximum (10) after dividing, clamping to 10", score);
                score = 10;
            }
        }
        return score;
    }

    private async Task<string> FixJsonWithLlmAsync(string brokenJson)
    {
        var maxRetries = _ollamaSettings.MaxJsonFixRetries;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting JSON correction with LLM (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                var fixPrompt = BuildJsonFixPrompt(brokenJson);
                var correctedResponse = await CallOllamaAsync(fixPrompt);
                
                var jsonStart = correctedResponse.IndexOf('{');
                var jsonEnd = correctedResponse.LastIndexOf('}');
                
                if (jsonStart == -1 || jsonEnd == -1)
                {
                    _logger.LogWarning("LLM correction attempt {Attempt} did not return valid JSON structure", attempt);
                    continue;
                }
                
                var correctedJson = correctedResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                _logger.LogDebug("LLM corrected JSON (attempt {Attempt}): {Json}", attempt, correctedJson);
                
                try
                {
                    JsonDocument.Parse(correctedJson);
                    _logger.LogInformation("LLM successfully corrected JSON on attempt {Attempt}", attempt);
                    return correctedJson;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM correction attempt {Attempt} still produced invalid JSON", attempt);
                    brokenJson = correctedJson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LLM JSON correction attempt {Attempt}", attempt);
            }
        }
        
        _logger.LogError("Failed to correct JSON after {MaxRetries} attempts with LLM", maxRetries);
        return null;
    }

    private string BuildJsonFixPrompt(string brokenJson)
    {
        return $@"
            O JSON abaixo está malformado e precisa ser corrigido. Por favor, corrija-o mantendo exatamente a mesma estrutura e dados, apenas corrigindo problemas de sintaxe como vírgulas extras, quebras de linha incorretas, etc.

            JSON com problemas:
            {brokenJson}

            Retorne APENAS o JSON corrigido, sem explicações adicionais. O JSON deve ter exatamente esta estrutura:
            {{
              ""variableScore"": número,
              ""functionScore"": número,
              ""commentScore"": número,
              ""cohesionScore"": número,
              ""deadCodeScore"": número,
              ""justifications"": {{
                ""VariableNaming"": ""texto"",
                ""FunctionSizes"": ""texto""
              }}
            }}";
    }
}