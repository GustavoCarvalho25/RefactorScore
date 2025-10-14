using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RefactorScore.Application.Services;
using RefactorScore.CrossCutting.IoC.Configuration;
using RefactorScore.Domain.Repositories;
using RefactorScore.Domain.Services;
using RefactorScore.Infrastructure.Configurations;
using RefactorScore.Infrastructure.Mappers;
using RefactorScore.Infrastructure.Repositories;
using RefactorScore.Infrastructure.Services;

namespace RefactorScore.CrossCutting.IoC.DependenceInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        MongoDbMapper.RegisterMappings();
        
        var mongoSettings = configuration.GetSection("MongoDB").Get<MongoDbSettings>(); 
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDB"));
        
        services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoSettings.ConnectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoSettings.DatabaseName);
        });
        
        services.AddSingleton<ICommitAnalysisRepository, CommitAnalysisRepository>();

        services.AddSingleton<IGitServiceFacade>(sp =>
        {
            var gitSettings = configuration.GetSection("Git").Get<GitSettings>();
            var mapper = sp.GetRequiredService<GitMapper>();
            var logger = sp.GetRequiredService<ILogger<GitServiceFacade>>();
            return new GitServiceFacade(gitSettings.RepositoryPath, mapper, logger);
        });
        
        services.AddSingleton<GitMapper>();
        
        
        services.Configure<OllamaSettings>(configuration.GetSection("Ollama"));
        
        services.AddHttpClient("Ollama", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
            client.Timeout = Timeout.InfiniteTimeSpan;
            if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
                client.BaseAddress = new Uri(settings.BaseUrl);
        });
        services.AddSingleton<ILLMService>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpFactory.CreateClient("Ollama");
            var logger = sp.GetRequiredService<ILogger<OllamaIllmService>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var ollamaOptions = sp.GetRequiredService<IOptions<OllamaSettings>>();
            return new OllamaIllmService(logger, httpClient, config, ollamaOptions);
        });

        return services;
    }
}