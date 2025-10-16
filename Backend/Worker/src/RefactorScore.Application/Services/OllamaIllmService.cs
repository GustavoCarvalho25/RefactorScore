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
    private readonly OllamaSettings _ollamaSettings;

    public OllamaIllmService(ILogger<OllamaIllmService> logger, HttpClient httpClient, IOptions<OllamaSettings> ollamaOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (ollamaOptions == null)
            throw new ArgumentNullException(nameof(ollamaOptions));
            
        _ollamaSettings = ollamaOptions.Value ?? throw new ArgumentNullException(nameof(ollamaOptions.Value));
        _ollamaUrl = _ollamaSettings.BaseUrl ?? throw new ArgumentNullException(nameof(_ollamaSettings.BaseUrl));
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

                var response = await CallOllamaAsync(prompt, forceJsonObject: false);
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
        var scores = new List<(string Criterion, int Score, string Type, string Chapter)>
        {
            ("Variable Naming", rating.VariableNaming, "Naming", "Capítulo 2 - Nomes significativos"),
            ("Function Sizes", rating.FunctionSizes, "Structure", "Capítulo 3 - Funções"),
            ("Comments", rating.NoNeedsComments, "Documentation", "Capítulo 4 - Comentários"),
            ("Cohesion", rating.MethodCohesion, "Cohesion", "Capítulo 10 - Classes"),
            ("Dead Code", rating.DeadCode, "DeadCode", "Capítulo 17 - Odores e Heurísticas")
        };
        
        var priorityAreas = scores.OrderBy(s => s.Score).Take(3).ToList();
        var priorityAreasText = string.Join(", ", priorityAreas.Select(p => $"{p.Criterion} ({p.Score}/10)"));
        
        return $@"
        NOVA TAREFA - IGNORE INSTRUÇÕES ANTERIORES

        Você deve gerar EXATAMENTE 3 sugestões de melhoria para o código.

        IMPORTANTE:
        - Responda SOMENTE com um array JSON (iniciando com [ e terminando com ])
        - NÃO retorne um objeto com chaves como VariableScore, FunctionScore, etc
        - NÃO retorne justifications
        - Retorne um ARRAY de 3 objetos de sugestões

        Índice de capítulos (Clean Code) para referência futura:
            1 - Código Limpo; 2 - Nomes significativos; 3 - Funções; 4 - Comentários; 5 - Formatação; 6 - Objetos e Estruturas de Dados; 7 - Tratamento de Erro; 8 - Limites;
            9 - Testes de Unidade; 10 - Classes; 11 - Sistemas; 12 - Emergência; 13 - Concorrência; 14 - Refinamento Sucessivo; 15 - Características Internas do JUnit;
            16 - Refatorando o SerialDate; 17 - Odores e Heurísticas.

        Notas já calculadas (1-10):
        - Variable Naming: {rating.VariableNaming}/10
        - Function Sizes: {rating.FunctionSizes}/10
        - Comments: {rating.NoNeedsComments}/10
        - Cohesion: {rating.MethodCohesion}/10
        - Dead Code: {rating.DeadCode}/10

        ÁREAS PRIORITÁRIAS (notas mais baixas): {priorityAreasText}

        REGRAS PARA SUGESTÕES:
        1. FOQUE nas 3 áreas com notas mais baixas listadas acima
        2. Para notas < 7: Sugira correções específicas de problemas identificados
        3. Para notas 7-8: Sugira melhorias incrementais
        4. Para notas > 8: Sugira boas práticas avançadas ou otimizações
        5. Seja ESPECÍFICO ao código fornecido (cite nomes de variáveis/funções/classes)
        6. NÃO sugira melhorias para áreas com notas altas (> 8) a menos que seja uma otimização avançada
        7. CRÍTICO: Cite APENAS elementos que existem no código fornecido acima
        8. CRÍTICO: NÃO invente nomes de variáveis, funções ou classes
        9. CRÍTICO: Se não houver problemas específicos, sugira boas práticas gerais sem inventar exemplos

        Código:
        {fileContent}

        Tipos válidos: CodeStyle, Naming, Structure, Documentation, Testing, ErrorHandling, Performance, Refactoring, Cohesion, DeadCode
        Prioridades válidas: Low, Medium, High
        Dificuldades válidas: Easy, Medium, Hard

        FORMATO OBRIGATÓRIO (retorne exatamente assim, com 3 objetos):
        [
          {{
            ""title"": ""Sugestão específica para {priorityAreas[0].Criterion}"",
            ""description"": ""Descrição detalhada baseada no código"",
            ""priority"": ""High"",
            ""type"": ""{priorityAreas[0].Type}"",
            ""difficulty"": ""Medium"",
            ""studyResources"": [""{priorityAreas[0].Chapter}""]
          }},
          {{
            ""title"": ""Sugestão específica para {priorityAreas[1].Criterion}"",
            ""description"": ""Descrição detalhada baseada no código"",
            ""priority"": ""Medium"",
            ""type"": ""{priorityAreas[1].Type}"",
            ""difficulty"": ""Easy"",
            ""studyResources"": [""{priorityAreas[1].Chapter}""]
          }},
          {{
            ""title"": ""Sugestão específica para {priorityAreas[2].Criterion}"",
            ""description"": ""Descrição detalhada baseada no código"",
            ""priority"": ""Low"",
            ""type"": ""{priorityAreas[2].Type}"",
            ""difficulty"": ""Easy"",
            ""studyResources"": [""{priorityAreas[2].Chapter}""]
          }}
        ]

        RESPONDA APENAS COM O ARRAY JSON. Não adicione texto antes ou depois.";
    }


    private string BuildAnalysisPrompt(string fileContent)
    {
        return $@"
            Avalie o código abaixo como um analista sênior de Clean Code.
            Atribua notas inteiras de 1 a 10 para as chaves: variableScore, functionScore, commentScore, cohesionScore, deadCodeScore.
            Sendo 10 a melhor nota, para casos de boa qualidade ou quando não houve alterações ou problemas detectados a esse critério, e 0 a pior nota, para casos de código muito ruim ou que precisa de muitas melhorias. 
            A nota 5 deve ser usada para código mediano, que pode melhorar em vários aspectos. Tem que haver fundamento para as avaliações muito boas, medianas ou ruins, sempre pensando no critério específico e nas alterações que houveram no código.
            Faça uma analise considerando as notas de 0 a 10, ponderando a qualidade e considerando o significado dos critérios. VariableScore deve considerar a clareza e consistência dos nomes de variáveis. FunctionScore deve avaliar o tamanho e foco das funções.
            CommentScore deve analisar a necessidade e utilidade dos comentários, lembrando que o CleanCode recomenda não ter comentários, e só ter em casos muito necessários (Se houver comentarios demais ou desnecessários, a nota deve ser ruim).
            CohesionScore deve medir a coesão das responsabilidades dentro das classes/métodos, lembrando que classes e funções devem ser relativamente pequenas e ter uma responsabilidade única.
            DeadCodeScore deve identificar a presença de código morto ou redundante que poderia ser eliminado (Se houver deadCode, a nota deve cair proporcionalmente à quantidade, mas se não tiver deadCode, a nota deve ser boa).
            Inclua um objeto justifications com justificativas textuais por critério usando EXATAMENTE as chaves: VariableNaming, FunctionSizes, NoNeedsComments, MethodCohesion, DeadCode.
            Responda SOMENTE com um JSON válido contendo exatamente estas chaves no objeto raiz.

            Regras das justificativas (obrigatório):
            - Devem ser específicas e baseadas EXCLUSIVAMENTE no código fornecido, em português claro.
            - Proibido usar frases genéricas como: ""Justificativa para nota"", ""Genérico"", ""N/A"", ""Sem detalhes"".
            - Cada justificativa deve referenciar pelo menos 2 elementos concretos (ex.: nomes de funções/métodos/variáveis, estruturas como if/for/linq) e, quando possível, citar o capítulo pertinente.
            - Tamanho recomendado entre 10 e 35 palavras por justificativa (curta e objetiva).
            - NÃO COPIE o exemplo abaixo; gere justificativas originais com base no código fornecido.
            - CRÍTICO: NÃO invente nomes de variáveis, funções ou classes que NÃO existem no código fornecido.
            - CRÍTICO: Cite APENAS elementos que aparecem literalmente no código acima.

            Índice de capítulos (Clean Code) para referência futura:
            1 - Código Limpo; 2 - Nomes significativos; 3 - Funções; 4 - Comentários; 5 - Formatação; 6 - Objetos e Estruturas de Dados; 7 - Tratamento de Erro; 8 - Limites;
            9 - Testes de Unidade; 10 - Classes; 11 - Sistemas; 12 - Emergência; 13 - Concorrência; 14 - Refinamento Sucessivo; 15 - Características Internas do JUnit;
            16 - Refatorando o SerialDate; 17 - Odores e Heurísticas.

            Código:
            ""{fileContent}""

            RESPONDER SOMENTE com um JSON válido nessa estrutura, com as notas e justificativas coerentes ao código fornecido e seguindo TODAS as regras:
            {{
              ""variableScore"": número,
              ""functionScore"": número,
              ""commentScore"": número,
              ""cohesionScore"": número,
              ""deadCodeScore"": número,
              ""justifications"": {{
                ""VariableNaming"": ""texto"",
                ""FunctionSizes"": ""texto"",
                ""NoNeedsComments"": ""texto"",
                ""MethodCohesion"": ""texto"",
                ""DeadCode"": ""texto""
              }}
            }}

            Regras obrigatórias:
            - Use somente números inteiros de 1 a 10.
            - As justificativas DEVEM ter entre 10 e 35 palavras.
            - As justificativas DEVEM ser específicas e baseadas no código fornecido.
            - Não inclua texto fora do JSON.
            - As chaves de justifications DEVEM ser exatamente as cinco acima.

            Exemplo de formato (apenas estrutura e NÃO DEVE SER COPIADO. Você deve gerar justificativas originais com base no código fornecido):
            {{
              ""variableScore"": 7,
              ""functionScore"": 6,
              ""commentScore"": 5,
              ""cohesionScore"": 8,
              ""deadCodeScore"": 9,
              ""justifications"": {{
                ""VariableNaming"": ""[EXEMPLO] A nomenclatura de variáveis é clara, mas alguns nomes como 'x' e 'y' poderiam ser mais descritivos. Recomendo revisar o Capítulo 2 - Nomes significativos."",
                ""FunctionSizes"": ""[EXEMPLO] As funções são geralmente concisas, porém a função 'Calculate' tem 50 linhas, o que dificulta a leitura. Sugiro dividir em funções menores conforme o Capítulo 3 - Funções."",
                ""NoNeedsComments"": ""[EXEMPLO] O código é autoexplicativo na maioria das partes, mas há comentários redundantes como '// Incrementa i'. Considere eliminar comentários desnecessários seguindo o Capítulo 4 - Comentários."",
                ""MethodCohesion"": ""[EXEMPLO] A coesão dos métodos é boa, mas a classe 'UserManager' mistura responsabilidades de autenticação e gerenciamento de usuários. Veja o Capítulo 10 - Classes para melhorar a coesão."",
                ""DeadCode"": ""[EXEMPLO] Não foram encontrados trechos significativos de código morto. O código está limpo e sem variáveis ou funções não utilizadas. Recomendo manter essa prática conforme o Capítulo 17 - Odores e Heurísticas.""
              }}
            }}";
    }

    private async Task<string> CallOllamaAsync(string prompt, bool forceJsonObject = true)
    {
        var timeoutSeconds = _ollamaSettings.AnalysisTimeoutSeconds;
        var model = _ollamaSettings.Model;
        
        var requestObject = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["stream"] = false,
            ["options"] = new {
                temperature = 0.4,
                top_p = 0.95,
                top_k = 60, 
                repeat_penalty = 1.1,
            }
        };
        
        if (forceJsonObject)
        {
            requestObject["format"] = "json";
        }

        var json = JsonSerializer.Serialize(requestObject);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var cts = new CancellationTokenSource();

        if (timeoutSeconds > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            
            if (_ollamaSettings.EnableDetailedLogging)
                _logger.LogInformation("HttpClient timeout configured to {TimeoutSeconds} seconds (forceJsonObject: {ForceJsonObject})", 
                    timeoutSeconds, forceJsonObject);
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

            string NormalizePriority(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return "Medium";
                var val = v.Trim().ToLowerInvariant();
                return val switch
                {
                    "low" => "Low",
                    "medium" => "Medium",
                    "med" => "Medium",
                    "high" => "High",
                    _ => "Medium"
                };
            }

            string NormalizeDifficulty(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return "Medium";
                var val = v.Trim().ToLowerInvariant();
                return val switch
                {
                    "easy" => "Easy",
                    "medium" => "Medium",
                    "med" => "Medium",
                    "hard" => "Hard",
                    _ => "Medium"
                };
            }

            string NormalizeType(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return "Refactoring";
                var val = v.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
                return val switch
                {
                    "codestyle" => "CodeStyle",
                    "naming" => "Naming",
                    "structure" => "Structure",
                    "documentation" => "Documentation",
                    "testing" => "Testing",
                    "errorhandling" => "ErrorHandling",
                    "performance" => "Performance",
                    "refactoring" => "Refactoring",
                    "cohesion" => "Cohesion",
                    "deadcode" => "DeadCode",
                    _ => "Refactoring"
                };
            }

            string[] ChapterForType(string type)
            {
                return type switch
                {
                    "Naming" => new[] { "Capítulo 2 - Nomes significativos" },
                    "Structure" => new[] { "Capítulo 3 - Funções", "Capítulo 11 - Sistemas" },
                    "Documentation" => new[] { "Capítulo 4 - Comentários" },
                    "Testing" => new[] { "Capítulo 9 - Testes de Unidade" },
                    "ErrorHandling" => new[] { "Capítulo 7 - Tratamento de Erro" },
                    "Performance" => new[] { "Capítulo 17 - Odores e Heurísticas" },
                    "Refactoring" => new[] { "Capítulo 17 - Odores e Heurísticas" },
                    "Cohesion" => new[] { "Capítulo 10 - Classes", "Capítulo 11 - Sistemas" },
                    "DeadCode" => new[] { "Capítulo 17 - Odores e Heurísticas" },
                    _ => new[] { "Capítulo 1 - Código Limpo" }
                };
            }

            foreach (var s in suggestions.Where(s => s != null))
            {
                s.Priority = NormalizePriority(s.Priority);
                s.Difficulty = NormalizeDifficulty(s.Difficulty);
                s.Type = NormalizeType(s.Type);
                if (s.StudyResources == null)
                    s.StudyResources = new List<string>();
                if (s.StudyResources.Count == 0)
                    s.StudyResources.AddRange(ChapterForType(s.Type));
            }

            var validSuggestions = suggestions
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Title) && !string.IsNullOrWhiteSpace(s.Description))
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
            else
            {
                // Log available keys to help diagnose schema mismatches
                var keys = string.Join(", ", scoreElement.EnumerateObject().Select(p => p.Name));
                _logger.LogWarning("'justifications' not found. Available root keys: {Keys}", keys);
            }
            
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
              ""VariableScore"": número,
              ""FunctionScore"": número,
              ""CommentScore"": número,
              ""CohesionScore"": número,
              ""DeadCodeScore"": número,
              ""justifications"": {{
                ""VariableNaming"": ""texto"",
                ""FunctionSizes"": ""texto"",
                ""NoNeedsComments"": ""texto"",
                ""MethodCohesion"": ""texto"",
                ""DeadCode"": ""texto""
              }}
            }}";
    }
}