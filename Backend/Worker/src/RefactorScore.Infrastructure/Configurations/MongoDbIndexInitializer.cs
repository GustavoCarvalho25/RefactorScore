using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RefactorScore.Domain.Entities;

namespace RefactorScore.Infrastructure.Configurations;

/// <summary>
/// Inicializa índices do MongoDB para garantir performance nas queries.
/// Executado automaticamente no startup da aplicação.
/// </summary>
public class MongoDbIndexInitializer
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbIndexInitializer> _logger;

    public MongoDbIndexInitializer(IMongoDatabase database, ILogger<MongoDbIndexInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <summary>
    /// Cria todos os índices necessários na collection CommitAnalysis.
    /// Índices são criados em background para não bloquear operações.
    /// </summary>
    public async Task InitializeIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB indexes...");
            
            var collection = _database.GetCollection<CommitAnalysis>("CommitAnalysis");
            
            await CreateProjectIndexAsync(collection);
            
            await CreateCommitIdIndexAsync(collection);
            
            await CreateProjectDateIndexAsync(collection);
            
            _logger.LogInformation("MongoDB indexes initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MongoDB indexes");
        }
    }

    private async Task CreateProjectIndexAsync(IMongoCollection<CommitAnalysis> collection)
    {
        try
        {
            var indexModel = new CreateIndexModel<CommitAnalysis>(
                Builders<CommitAnalysis>.IndexKeys.Ascending(x => x.Project),
                new CreateIndexOptions 
                { 
                    Name = "idx_project",
                    Background = true 
                }
            );
            
            await collection.Indexes.CreateOneAsync(indexModel);
            _logger.LogInformation("  ✓ Index 'idx_project' created/verified");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            _logger.LogDebug("Index 'idx_project' already exists");
        }
    }

    private async Task CreateCommitIdIndexAsync(IMongoCollection<CommitAnalysis> collection)
    {
        try
        {
            var indexModel = new CreateIndexModel<CommitAnalysis>(
                Builders<CommitAnalysis>.IndexKeys.Ascending(x => x.CommitId),
                new CreateIndexOptions 
                { 
                    Name = "idx_commitid",
                    Background = true,
                    Unique = true
                }
            );
            
            await collection.Indexes.CreateOneAsync(indexModel);
            _logger.LogInformation("Index 'idx_commitid' created/verified");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            _logger.LogDebug("Index 'idx_commitid' already exists");
        }
    }

    private async Task CreateProjectDateIndexAsync(IMongoCollection<CommitAnalysis> collection)
    {
        try
        {
            var indexModel = new CreateIndexModel<CommitAnalysis>(
                Builders<CommitAnalysis>.IndexKeys
                    .Ascending(x => x.Project)
                    .Descending(x => x.CommitDate),
                new CreateIndexOptions 
                { 
                    Name = "idx_project_date",
                    Background = true 
                }
            );
            
            await collection.Indexes.CreateOneAsync(indexModel);
            _logger.LogInformation("Index 'idx_project_date' created/verified");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            _logger.LogDebug("Index 'idx_project_date' already exists");
        }
    }
}
