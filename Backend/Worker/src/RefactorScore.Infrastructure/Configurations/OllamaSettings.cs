using System.ComponentModel.DataAnnotations;

namespace RefactorScore.Infrastructure.Configurations;

/// <summary>
/// Configurações para o serviço Ollama LLM
/// </summary>
public class OllamaSettings
{
    public const string SectionName = "Ollama";

    /// <summary>
    /// URL base do serviço Ollama
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Modelo LLM a ser utilizado
    /// </summary>
    [Required]
    public string Model { get; set; } = "llama2";

    /// <summary>
    /// Timeout para requisições HTTP em segundos
    /// Padrão: 300 segundos (5 minutos)
    /// </summary>
    [Range(30, 3600)]
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Timeout para análise de arquivos em segundos
    /// Padrão: 180 segundos (3 minutos)
    /// </summary>
    [Range(30, 1800)]
    public int AnalysisTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Timeout para geração de sugestões em segundos
    /// Padrão: 120 segundos (2 minutos)
    /// </summary>
    [Range(30, 1800)]
    public int SuggestionsTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Número máximo de tentativas de correção de JSON
    /// </summary>
    [Range(1, 10)]
    public int MaxJsonFixRetries { get; set; } = 5;

    /// <summary>
    /// Habilita logs detalhados para debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Timeout para operações de health check em segundos
    /// </summary>
    [Range(5, 60)]
    public int HealthCheckTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número máximo de análises simultâneas (paralelismo)
    /// Padrão: 2 (ajuste conforme VRAM disponível da GPU)
    /// 1 = sequencial, 2-3 = balanceado, 4+ = agressivo
    /// </summary>
    [Range(1, 8)]
    public int MaxConcurrentAnalysis { get; set; } = 2;
}