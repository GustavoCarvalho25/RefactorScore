using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using RefactorScore.Domain.Repositories;
using RefactorScore.Infrastructure.Mappers;
using RefactorScore.Infrastructure.Repositories;
using Xunit;

namespace RefactorScore.Integration.Tests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private ServiceProvider? _serviceProvider;
    protected IServiceProvider ServiceProvider => _serviceProvider!;
    protected IMongoDatabase Database { get; private set; } = null!;
    protected ICommitAnalysisRepository Repository { get; private set; } = null!;

    protected IntegrationTestBase()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
        
        var connectionString = _mongoContainer.GetConnectionString();
        
        MongoDbMapper.RegisterMappings();
        
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = connectionString,
                ["MongoDB:DatabaseName"] = "RefactorScoreTestDb",
                ["Git:RepositoryPath"] = "C:\\temp\\test-repo",
                ["Ollama:BaseUrl"] = "http://localhost:11434",
                ["Ollama:Model"] = "llama3.1"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        
        services.AddSingleton<IMongoClient>(provider =>
        {
            var mongoClient = new MongoClient(connectionString);
            return mongoClient;
        });
        
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            return client.GetDatabase("RefactorScoreTestDb");
        });
        
        services.AddScoped<ICommitAnalysisRepository, CommitAnalysisRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        Database = _serviceProvider.GetRequiredService<IMongoDatabase>();
        Repository = _serviceProvider.GetRequiredService<ICommitAnalysisRepository>();
        
        await CleanupDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await CleanupDatabaseAsync();
        }
        catch
        {
        }

        try
        {
            await _mongoContainer.DisposeAsync();
        }
        catch
        {
        }

        _serviceProvider?.Dispose();
    }

    protected async Task CleanupDatabaseAsync()
    {
        try
        {
            await Database.DropCollectionAsync("CommitAnalysis");
        }
        catch
        {
        }
    }

    protected async Task<IMongoCollection<T>> GetCollectionAsync<T>(string collectionName)
    {
        return Database.GetCollection<T>(collectionName);
    }
}
