using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.Exceptions;
using RefactorScore.Domain.Models;
using RefactorScore.Domain.Repositories;
using RefactorScore.Domain.Services;
using RefactorScore.Domain.ValueObjects;
using RefactorScore.Domain.Enum;
using RefactorScore.Infrastructure.Configurations;

namespace RefactorScore.Application.Services;

public class CommitAnalysisService : ICommitAnalysisService
{
    private readonly ICommitAnalysisRepository _repository;
    private readonly ILogger<CommitAnalysisService> _logger;
    private readonly IGitServiceFacade _gitRepository;
    private readonly ILLMService _illmService;
    private readonly SemaphoreSlim _semaphore;

    public CommitAnalysisService(
        ICommitAnalysisRepository repository, 
        ILogger<CommitAnalysisService> logger, 
        IGitServiceFacade gitRepository, 
        ILLMService illmService,
        IOptions<OllamaSettings> ollamaOptions)
    {
        _repository = repository;
        _logger = logger;
        _gitRepository = gitRepository;
        _illmService = illmService;
        
        var maxConcurrency = ollamaOptions.Value.MaxConcurrentAnalysis;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        
        _logger.LogInformation("CommitAnalysisService initialized with MaxConcurrentAnalysis={MaxConcurrency}", maxConcurrency);
    }

    public async Task<CommitAnalysis> AnalyzeCommitAsync(string commitId)
    {
        var existing = await _repository.GetByCommitIdAsync(commitId);
        if (existing != null)
        {
            _logger.LogInformation("Commit analysis already exists for commit {CommitId}", commitId);
            return existing;
        }
        
        var commitData = await _gitRepository.GetCommitByIdAsync(commitId);

        if (commitData == null)
        {
            throw new DomainException($"Commit with id {commitId} not found");
        }
        
        var filesChanges = await _gitRepository.GetCommitChangesAsync(commitId);
        commitData.Changes = filesChanges;
        
        var analysis = new CommitAnalysis(
            commitData.Id,
            commitData.Author,
            commitData.Email,
            commitData.Date,
            DateTime.UtcNow,
            DetermineOverallLanguage(filesChanges),
            filesChanges.Sum(f => f.AddedLines),
            filesChanges.Sum(f => f.RemovedLines),
            commitData.ProjectName
        );

        // Filtrar arquivos válidos para análise
        var filesToAnalyze = filesChanges
            .Where(c => c.IsSourceCode)
            .Where(f => f.ChangeType != FileChangeType.Deleted)
            .Where(f => !string.IsNullOrWhiteSpace(f.Content))
            .ToList();
        
        // Adicionar arquivos à análise primeiro (sequencial)
        foreach (var file in filesToAnalyze)
        {
            var commitFile = new CommitFile(
                file.Path,
                file.AddedLines,
                file.RemovedLines,
                file.Language,
                file.Content
            );
            analysis.AddFile(commitFile);
        }
        
        // Processar análises LLM em paralelo
        var analysisStartTime = DateTime.UtcNow;
        _logger.LogInformation("Starting parallel analysis of {FileCount} files for commit {CommitId}", 
            filesToAnalyze.Count, commitId);
        
        var analysisTasks = filesToAnalyze.Select(file => AnalyzeFileWithLLMAsync(file, analysis, commitId));
        await Task.WhenAll(analysisTasks);
        
        var analysisElapsed = DateTime.UtcNow - analysisStartTime;
        _logger.LogInformation("Completed parallel analysis in {ElapsedSeconds:F1}s for commit {CommitId}", 
            analysisElapsed.TotalSeconds, commitId);
        
        if (analysis.Files.Count == 0)
        {
            _logger.LogWarning(
                "Skipping commit {CommitId} - no source code files were analyzed. Language={Language}, TotalFiles={TotalFiles}",
                commitId, analysis.Language, filesChanges.Count);
            return analysis;
        }
        
        if (analysis.Language == "Unknown")
        {
            _logger.LogWarning(
                "Skipping commit {CommitId} - language could not be determined. AnalyzedFiles={AnalyzedFiles}",
                commitId, analysis.Files.Count);
            return analysis;
        }
        
        await _repository.AddAsync(analysis);
        _logger.LogInformation(
            "Commit analysis saved successfully for {CommitId}. Files analyzed: {FileCount}, Language: {Language}",
            commitId, analysis.Files.Count, analysis.Language);
        return analysis;
    }

    private async Task AnalyzeFileWithLLMAsync(FileChange file, CommitAnalysis analysis, string commitId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Analyzing file {FilePath} for commit {CommitId}", file.Path, commitId);
            var fileStartTime = DateTime.UtcNow;
            
            // Análise do arquivo
            var llmResult = await _illmService.AnalyzeFileAsync(file.Content);
            
            var rating = new CleanCodeRating(
                llmResult.VariableScore,
                llmResult.FunctionScore,
                llmResult.CommentScore,
                llmResult.CohesionScore,
                llmResult.DeadCodeScore,
                llmResult.Justifications
            );
            
            // Geração de sugestões
            var llmSuggestions = await _illmService.GenerateSuggestionsAsync(file.Content, rating);
            
            var suggestions = llmSuggestions
                .Select(s => new Suggestion(
                    s.Title,
                    s.Description,
                    s.Priority,
                    s.Type,
                    s.Difficulty,
                    file.Path,
                    DateTime.UtcNow,
                    s.StudyResources
                )).ToList();
            
            // Thread-safe: CompleteFileAnalysis precisa ser sincronizado
            lock (analysis)
            {
                analysis.CompleteFileAnalysis(file.Path, rating, suggestions);
            }
            
            var fileElapsed = DateTime.UtcNow - fileStartTime;
            _logger.LogInformation("Completed analysis of {FilePath} in {ElapsedSeconds:F1}s", 
                file.Path, fileElapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file {FilePath} for commit {CommitId}", file.Path, commitId);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string DetermineOverallLanguage(List<FileChange> filesChanges)
    {
        if (filesChanges == null || filesChanges.Count == 0)
        {
            _logger.LogWarning("DetermineOverallLanguage: No files in change set. Returning 'Unknown'.");
            return "Unknown";
        }
        
        var source = filesChanges
            .Where(f => f.IsSourceCode && f.ChangeType != FileChangeType.Deleted)
            .ToList();
        
        if (!source.Any())
        {
            var total = filesChanges.Count;
            var nonSource = filesChanges.Count(f => !f.IsSourceCode);
            var deleted = filesChanges.Count(f => f.ChangeType == FileChangeType.Deleted);
            _logger.LogWarning(
                "DetermineOverallLanguage: No source-code files (non-deleted) found. total={Total}, nonSource={NonSource}, deleted={Deleted}. Returning 'Unknown'.",
                total, nonSource, deleted);
        }

        var language = source
            .GroupBy(f => f.Language)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()
            ?.Key;

        if (string.IsNullOrWhiteSpace(language))
        {
            var total = filesChanges.Count;
            var sourceCount = source.Count;
            _logger.LogWarning(
                "DetermineOverallLanguage: Could not infer language (empty result). total={Total}, source={Source}. Returning 'Unknown'.",
                total, sourceCount);
            return "Unknown";
        }

        return language;
    }
}