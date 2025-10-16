using Microsoft.Extensions.Logging;
using RefactorScore.Domain.Entities;
using RefactorScore.Domain.Exceptions;
using RefactorScore.Domain.Models;
using RefactorScore.Domain.Repositories;
using RefactorScore.Domain.Services;
using RefactorScore.Domain.ValueObjects;
using RefactorScore.Domain.Enum;

namespace RefactorScore.Application.Services;

public class CommitAnalysisService : ICommitAnalysisService
{
    private ICommitAnalysisRepository _repository;
    private ILogger<CommitAnalysisService> _logger;
    private IGitServiceFacade _gitRepository;
    private ILLMService _illmService;

    public CommitAnalysisService(ICommitAnalysisRepository repository, ILogger<CommitAnalysisService> logger, IGitServiceFacade gitRepository, ILLMService illmService)
    {
        _repository = repository;
        _logger = logger;
        _gitRepository = gitRepository;
        _illmService = illmService;
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
            filesChanges.Sum(f => f.RemovedLines)
        );

        foreach (var file in filesChanges.Where(c => c.IsSourceCode))
        {
            if (file.ChangeType == FileChangeType.Deleted)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.Content))
            {
                _logger.LogWarning("Skipping file with empty content: {Path} (ChangeType={ChangeType})", file.Path, file.ChangeType);
                continue;
            }

            var commitFile = new CommitFile(
                file.Path,
                file.AddedLines,
                file.RemovedLines,
                file.Language,
                file.Content
            );
            
            analysis.AddFile(commitFile);
            
            var llmResult = await _illmService.AnalyzeFileAsync(file.Content);
            
            var rating = new CleanCodeRating(
                llmResult.VariableScore,
                llmResult.FunctionScore,
                llmResult.CommentScore,
                llmResult.CohesionScore,
                llmResult.DeadCodeScore,
                llmResult.Justifications
            );
            
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
            
            analysis.CompleteFileAnalysis(file.Path, rating, suggestions);
        }
        
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