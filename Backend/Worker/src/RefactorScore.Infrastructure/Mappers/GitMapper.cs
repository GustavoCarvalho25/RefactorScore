using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using RefactorScore.Domain.Enum;
using RefactorScore.Domain.Models;

namespace RefactorScore.Infrastructure.Mappers;

public partial class GitMapper
{
    private readonly ILogger<GitMapper> _logger;
    public GitMapper(ILogger<GitMapper> logger)
    {
        _logger = logger;
    }
    
    public CommitData MapCommitToCommitData(Commit libGitCommit, string repositoryPath = null)
    {
        return new CommitData
        {
            Id = libGitCommit.Sha,
            Author = libGitCommit.Author.Name,
            Email = libGitCommit.Author.Email,
            Date = libGitCommit.Author.When.DateTime,
            Message = libGitCommit.Message.Trim(),
            MessageShort = libGitCommit.MessageShort,
            ProjectName = ExtractProjectName(repositoryPath)
        };
    }
    
    private string ExtractProjectName(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            return "Unknown";
        
        try
        {
            var cleanPath = repositoryPath.TrimEnd('\\', '/');
            if (cleanPath.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = Path.GetDirectoryName(cleanPath);
            }
            
            var projectName = Path.GetFileName(cleanPath);
            
            return string.IsNullOrWhiteSpace(projectName) ? "Unknown" : projectName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting project name from path: {RepositoryPath}", repositoryPath);
            return "Unknown";
        }
    }

    public List<FileChange> MapCommitChangesToFileChanges(Commit libGitCommit, Repository repo)
    {
        var changes = new List<FileChange>();

        if (libGitCommit.Parents.Count() == 0)
        {
            changes.AddRange(MapTreeToFileChanges(libGitCommit.Tree, FileChangeType.Added, repo));
        }
        else
        {
            var parent = libGitCommit.Parents.First();
            var patch = repo.Diff.Compare<Patch>(parent.Tree, libGitCommit.Tree);

            changes.AddRange(MapPatchToFileChanges(patch));
        }

        return changes;
    }

    private List<FileChange> MapTreeToFileChanges(Tree tree, FileChangeType type, Repository repo)
    {
        var changes = new List<FileChange>();

        try
        {
            foreach (var entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    var blob = (Blob)entry.Target;
                    var content = blob.GetContentText();
                    
                    var lines = content?.Split('\n') ?? Array.Empty<string>();
                    var addedLines = lines.Length;
                    
                    var change = new FileChange
                    {
                        Path = entry.Path,
                        Language = DetermineLanguage(entry.Path),
                        AddedLines = addedLines,
                        RemovedLines = 0,
                        ChangeType = type,
                        IsSourceCode = IsSourceCode(entry.Path),
                        Content = content
                    };
                    
                    changes.Add(change);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error mapping tree to file changes");
        }
        
        return changes;
    }

    private List<FileChange> MapPatchToFileChanges(Patch patch)
    {
        var changes = new List<FileChange>();

        foreach (var patchEntryChange in patch)
        {
            var change = new FileChange
            {
                Path = patchEntryChange.Path,
                Language = DetermineLanguage(patchEntryChange.Path),
                AddedLines = patchEntryChange.LinesAdded,
                RemovedLines = patchEntryChange.LinesDeleted,
                ChangeType = MapChangeType(patchEntryChange.Status),
                IsSourceCode = IsSourceCode(patchEntryChange.Path)
            };
            
            if (patchEntryChange.Status != ChangeKind.Deleted)
            {
                change.Content = GetFileContentDiff(patchEntryChange);
            }
            
            changes.Add(change);
        }
        
        return changes;
    }

    private string DetermineLanguage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "Unknown";
        
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
    
        return extension switch
        {
            ".cs" => "C#",
            ".java" => "Java",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".py" => "Python",
            ".rb" => "Ruby",
            ".php" => "PHP",
            ".go" => "Go",
            ".c" => "C",
            ".cpp" => "C++",
            ".h" => "C/C++",
            ".swift" => "Swift",
            ".kt" => "Kotlin",
            ".rs" => "Rust",
            ".sh" => "Shell",
            ".pl" => "Perl",
            ".sql" => "SQL",
            ".html" => "HTML",
            ".css" => "CSS",
            ".scss" => "SCSS",
            ".less" => "LESS",
            ".xml" => "XML",
            ".json" => "JSON",
            ".yaml" => "YAML",
            ".yml" => "YAML",
            _ => extension.TrimStart('.').ToUpperInvariant()
        };
    }
    
    private FileChangeType MapChangeType(ChangeKind changeKind)
    {
        return changeKind switch
        {
            ChangeKind.Added => FileChangeType.Added,
            ChangeKind.Modified => FileChangeType.Modified,
            ChangeKind.Deleted => FileChangeType.Deleted,
            ChangeKind.Renamed => FileChangeType.Renamed,
            _ => FileChangeType.Modified
        };
    }

    private bool IsSourceCode(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var blacklistedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "package-lock.json",
            "yarn.lock",
            "pnpm-lock.yaml",
            "composer.lock",
            ".gitignore",
            ".dockerignore",
            ".editorconfig",
            ".prettierrc",
            ".eslintrc",
            ".eslintrc.json",
            "tsconfig.json",
            "jsconfig.json",
            "vite.config.ts",
            "vite.config.js",
            "webpack.config.js",
            "rollup.config.js"
        };

        var fileName = Path.GetFileName(filePath);
        if (blacklistedFiles.Contains(fileName))
        {
            _logger.LogDebug("Skipping blacklisted file: {FilePath}", filePath);
            return false;
        }

        var blacklistedDirectories = new[] 
        { 
            "node_modules", "bin", "obj", ".git", ".vs", ".vscode", 
            "dist", "build", "coverage", ".nuxt", ".next"
        };
        
        if (blacklistedDirectories.Any(dir => 
            filePath.Contains($"/{dir}/") || 
            filePath.Contains($"\\{dir}\\")))
        {
            _logger.LogDebug("Skipping file in blacklisted directory: {FilePath}", filePath);
            return false;
        }

        var executableCodeExtensions = new HashSet<string>
        {
            ".cs", ".java", ".py", ".rb", ".php", ".go",
            ".c", ".cpp", ".cc", ".cxx", ".h", ".hpp", ".hxx",
            ".swift", ".kt", ".rs", ".scala", ".clj", ".hs", ".ml",
            ".pl", ".pm", ".r", ".m", ".mm", ".f", ".f90", ".f95",
            ".dart", ".lua",
            
            ".js", ".ts", ".jsx", ".tsx",
            ".vue", ".svelte",
            
            ".sh", ".bash", ".zsh", ".fish", ".ps1", ".bat", ".cmd",
            ".sql"
        };

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var isCode = executableCodeExtensions.Contains(extension);
        
        if (!isCode)
        {
            _logger.LogDebug("File extension {Extension} not recognized as source code: {FilePath}", extension, filePath);
        }
        
        return isCode;
    }

    private string GetFileContentDiff(PatchEntryChanges patchEntry)
    {
        try
        {
            if (patchEntry.Status == ChangeKind.Deleted)
                return string.Empty;
            
            var patch = patchEntry.Patch;
            if (string.IsNullOrEmpty(patch))
                return string.Empty;
            
            var lines = patch.Split("\n");
            var diffContent = new List<string>();
            
            bool inHunk = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    inHunk = true;
                    diffContent.Add(line);
                    continue;
                }
                
                if (!inHunk) continue;
                
                if (line.StartsWith("-") || line.StartsWith("+") || line.StartsWith(" "))
                {
                    diffContent.Add(line);
                }
                else if (line.StartsWith("\\"))
                {
                    diffContent.Add(line);
                }
            }
            
            return string.Join("\n", diffContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file content: {Message}", ex.Message);
            return string.Empty;
        }
    }
}