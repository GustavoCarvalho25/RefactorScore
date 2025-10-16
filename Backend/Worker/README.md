# RefactorScore - Backend Worker

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Docker](https://img.shields.io/badge/Docker-27.4.0-blue.svg)
![MongoDB](https://img.shields.io/badge/MongoDB-8.0.12-green.svg)
![Ollama](https://img.shields.io/badge/Ollama-0.11.4-orange.svg)

RefactorScore é um sistema avançado para análise de qualidade de commits Git utilizando Large Language Models (LLMs) locais. O sistema fornece análise detalhada de mudanças de código baseada nos princípios de Clean Code, ajudando desenvolvedores e equipes a melhorar a qualidade do código ao longo do tempo.

> **Contexto Acadêmico**: Este projeto é um Trabalho de Conclusão de Curso (TCC) focado na aplicação prática dos princípios de Clean Code de Robert C. Martin.

## Índice

- [Funcionalidades](#-funcionalidades)
- [Arquitetura do Sistema](#-arquitetura-do-sistema)
- [Stack Tecnológico](#-stack-tecnológico)
- [Instalação e Configuração](#-instalação-e-configuração)
- [Como Usar](#-como-usar)
- [Configurações Avançadas](#️-configurações-avançadas)
- [Análise de Código](#-análise-de-código)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Testes](#-testes)
- [Desenvolvimento](#-desenvolvimento)
- [Licença](#-licença)
- [Contribuindo](#-contribuindo)

## ✨ Funcionalidades

- **Análise Automatizada de Commits Git**: Analisa automaticamente commits para avaliar a qualidade do código.
- **Avaliação de Clean Code**: Avalia mudanças de código baseadas nos princípios de Clean Code.
- **Integração com LLM Local**: Utiliza modelos de linguagem hospedados localmente (via Ollama) para privacidade e customização.
- **Processamento Inteligente**: Lida com arquivos grandes usando abordagem de "janela deslizante" para gerenciar limitações de contexto.
- **Detecção de Tipo de Mudança**: Adapta a análise baseada no tipo de mudança de código (adicionado, modificado ou renomeado).
- **Filtragem de Tipo de Arquivo**: Foca a análise em arquivos de código, ignorando arquivos binários e recursos.
- **Resultados Persistentes**: Armazena resultados de análise no MongoDB para rastreamento histórico e revisão.
- **Processamento em Background**: Processa commits em segundo plano com agendamentos configuráveis.
- **Design Extensível**: Clean Architecture permite fácil extensão e adaptação a diferentes necessidades.

## 🏗 Arquitetura do Sistema

RefactorScore segue os princípios de Clean Architecture e Domain-Driven Design (DDD), com camadas bem separadas:

### **1. Domain Layer (RefactorScore.Domain)**
Contém todas as entidades, value objects, enums e interfaces de domínio. Esta camada não possui dependências externas.

**Entidades (Aggregates):**
- `CommitAnalysis`: Agregado raiz que representa uma análise completa de um commit
  - Propriedades: CommitId, Author, Email, CommitDate, AnalysisDate, Language, AddedLines, RemovedLines
  - Coleções: Files (CommitFile), Suggestions (Suggestion)
  - Rating calculado automaticamente baseado nos arquivos analisados

- `CommitFile`: Representa um arquivo modificado em um commit
  - Propriedades: Path, Language, Content, AddedLines, RemovedLines
  - Rating (CleanCodeRating) e Suggestions associadas

**Value Objects:**
- `CleanCodeRating`: Avaliação de Clean Code com 5 critérios (1-10):
  - VariableNaming: Qualidade da nomenclatura de variáveis
  - FunctionSizes: Tamanho e complexidade das funções
  - NoNeedsComments: Código auto-explicativo
  - MethodCohesion: Coesão dos métodos
  - DeadCode: Presença de código morto
  - Justifications: Dicionário com justificativas detalhadas por critério

- `Suggestion`: Sugestão de melhoria gerada pelo LLM
  - Title, Description, Priority (Low/Medium/High)
  - Type (Naming, Structure, Documentation, Testing, etc.)
  - Difficulty (Easy/Medium/Hard)
  - StudyResources: Capítulos do Clean Code recomendados
  - FileReference, LastUpdate

**Interfaces de Serviços:**
- `ILLMService`: Interface para integração com LLM
- `IGitServiceFacade`: Interface para operações Git
- `ICommitAnalysisService`: Interface para serviço de análise

**Repositories:**
- `ICommitAnalysisRepository`: Interface para persistência de análises

### **2. Application Layer (RefactorScore.Application)**
Contém a lógica de negócio e orquestra o fluxo de dados.

**Serviços:**
- `CommitAnalysisService`: Orquestra a análise de commits
  - Busca commits do repositório Git
  - Filtra arquivos de código fonte
  - Coordena análise via LLM
  - Persiste resultados no MongoDB

- `OllamaIllmService`: Implementação do ILLMService
  - Integração com Ollama API
  - Parsing inteligente de respostas JSON
  - Retry logic e correção automática de JSON malformado
  - Geração de sugestões baseadas nas notas de análise

### **3. Infrastructure Layer (RefactorScore.Infrastructure)**
Implementações concretas das interfaces de domínio.

**Repositories:**
- `CommitAnalysisRepository`: Persistência no MongoDB
  - Mapeamento de entidades para documentos MongoDB
  - Queries otimizadas

**Services:**
- `GitServiceFacade`: Integração com LibGit2Sharp
  - Leitura de commits e diffs
  - Detecção de tipo de mudança (Added/Modified/Deleted/Renamed)

**Mappers:**
- `GitMapper`: Conversão entre objetos LibGit2Sharp e modelos de domínio

**Configurations:**
- `OllamaSettings`: Configurações do serviço Ollama
- `MongoDbSettings`: Configurações do MongoDB

### **4. CrossCutting Layer (RefactorScore.CrossCutting.IoC)**
Configuração de injeção de dependências e serviços transversais.

**Dependency Injection:**
- `InfrastructureServiceExtensions`: Registro de serviços de infraestrutura
- `ApplicationServiceExtensions`: Registro de serviços de aplicação

### **5. WorkerService Layer (RefactorScore.WorkerService)**
Serviço de background que processa commits periodicamente.

**Worker:**
- `Worker`: BackgroundService que executa análises em intervalos configuráveis
  - Busca commits recentes
  - Processa cada commit de forma assíncrona
  - Tratamento de erros e logging detalhado

## 🔧 Stack Tecnológico

### **Core Technologies**
- **Linguagem**: C# 12
- **Framework**: .NET 8.0
- **Arquitetura**: Clean Architecture + Domain-Driven Design (DDD)

### **LLM Integration**
- **Ollama**: 0.11.4 (servidor LLM local)
- **Modelo**: qwen2.5-coder:7b (4.7 GB)
  - Especializado em análise de código
  - Suporte a múltiplas linguagens de programação
  - Execução local para privacidade

### **Storage**
- **MongoDB**: 8.0.12
  - Armazenamento de análises de commits
  - Coleção única com subdocumentos (CommitAnalysis → CommitFile → Suggestions)

### **Git Integration**
- **LibGit2Sharp**: 0.30.0
  - Leitura de commits e diffs
  - Análise de mudanças em arquivos

### **Containerization**
- **Docker**: 27.4.0
- **Docker Compose**: Para orquestração de serviços

### **Logging & Monitoring**
- **Serilog**: Logging estruturado
  - Console sink para desenvolvimento
  - File sink para produção

### **Testing**
- **xUnit**: Framework de testes unitários
- **NSubstitute**: Mocking framework
- **FluentAssertions**: Assertions fluentes

### **Additional Libraries**
- **Ardalis.GuardClauses**: Validação de argumentos
- **Mongo.Driver**: Driver oficial do MongoDB para .NET

## 📦 Instalação e Configuração

### Pré-requisitos

#### **Software**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://docs.docker.com/get-docker/) 27.4.0 ou superior
- [Git](https://git-scm.com/downloads)

#### **Hardware**
- **RAM**: Mínimo 8GB (recomendado 16GB para o modelo LLM)
- **Disco**: Mínimo 10GB de espaço livre
- **GPU (Altamente Recomendado)**: 
  - **NVIDIA GPU** com suporte a CUDA (GTX 1060 6GB ou superior)
  - **VRAM**: Mínimo 6GB (recomendado 8GB ou mais)
  - **Drivers**: NVIDIA Driver atualizado + NVIDIA Container Toolkit
  - **Nota**: Embora seja possível executar em CPU, o desempenho será **significativamente mais lento** (10-20x mais lento). Para uso em produção ou análises frequentes, uma GPU NVIDIA é **essencial**.

#### **Alternativa sem GPU NVIDIA**
Se você não possui GPU NVIDIA, considere:
- Executar em CPU (muito mais lento, mas funcional)
- Usar modelos menores (qwen2.5-coder:1.5b ao invés de 7b)
- Aumentar os timeouts no `appsettings.json`

### Passo 1: Clonar o Repositório

```bash
git clone https://github.com/GustavoCarvalho25/RefactorScore
cd RefactorScore/Backend/Worker
```

### Passo 2: Iniciar Serviços com Docker

Inicie todos os serviços necessários usando Docker Compose:

```bash
docker-compose up -d
```

Este comando irá iniciar:
- **Ollama** (LLM server) - `localhost:11434`
- **MongoDB** (banco de dados) - `localhost:27017`
- **Mongo Express** (UI admin MongoDB) - `localhost:8081`

### Passo 3: Baixar o Modelo LLM

Aguarde o Ollama iniciar e baixe o modelo qwen2.5-coder:

```bash
# Verificar se o Ollama está rodando
docker ps | grep ollama

# Baixar o modelo (isso pode levar alguns minutos - 4.7 GB)
docker exec refactorscore-ollama ollama pull qwen2.5-coder:7b

# Verificar se o modelo foi baixado
docker exec refactorscore-ollama ollama list
```

### Passo 4: Configurar o Aplicativo

Edite o arquivo `src/RefactorScore.WorkerService/appsettings.json`:

```json
{
  "Git": {
    "RepositoryPath": "D:\\Estudos\\Projects\\SeuRepositorio",
    "DefaultBranch": "master"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen2.5-coder:7b",
    "TimeoutSeconds": 300,
    "AnalysisTimeoutSeconds": 300,
    "SuggestionsTimeoutSeconds": 300,
    "MaxJsonFixRetries": 5,
    "EnableDetailedLogging": true,
    "HealthCheckTimeoutSeconds": 30
  },
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017/?authSource=admin",
    "DatabaseName": "RefactorScore",
    "CollectionName": "CommitAnalyses"
  }
}
```

**Importante:** Ajuste o `RepositoryPath` para o caminho absoluto do repositório Git que você deseja analisar.

### Passo 5: Restaurar Dependências

```bash
dotnet restore
```

### Passo 6: Executar o Worker

```bash
dotnet run --project src/RefactorScore.WorkerService/RefactorScore.WorkerService.csproj
```

Ou usando o Visual Studio/Rider, execute o projeto `RefactorScore.WorkerService`.

## 🚀 Como Usar

### Executando o Worker

Após configurar o `appsettings.json`, execute o worker:

```bash
dotnet run --project src/RefactorScore.WorkerService/RefactorScore.WorkerService.csproj
```

O worker irá:
1. Conectar ao repositório Git configurado
2. Buscar commits recentes (últimos 400 dias por padrão)
3. Analisar cada commit usando o modelo LLM
4. Salvar os resultados no MongoDB

### Visualizando Resultados

#### MongoDB (Mongo Express)
Acesse http://localhost:8081 para visualizar os dados:
- **Database**: RefactorScore
- **Collection**: CommitAnalyses

Estrutura de um documento:
```json
{
  "_id": "uuid",
  "CommitId": "hash-do-commit",
  "Author": "Nome do Autor",
  "Email": "email@example.com",
  "CommitDate": "2025-10-15T00:00:00Z",
  "AnalysisDate": "2025-10-15T01:00:00Z",
  "Language": "C#",
  "AddedLines": 100,
  "RemovedLines": 20,
  "Files": [
    {
      "Path": "src/Example.cs",
      "Language": "C#",
      "AddedLines": 50,
      "RemovedLines": 10,
      "Rating": {
        "VariableNaming": 8,
        "FunctionSizes": 7,
        "NoNeedsComments": 6,
        "MethodCohesion": 9,
        "DeadCode": 8,
        "Justifications": {
          "VariableNaming": "As variáveis têm nomes claros...",
          "FunctionSizes": "As funções são bem dimensionadas...",
          ...
        }
      },
      "Suggestions": [
        {
          "Title": "Melhorar nomenclatura de variáveis",
          "Description": "Renomear variável 'data' para 'userData'",
          "Priority": "Medium",
          "Type": "Naming",
          "Difficulty": "Easy",
          "StudyResources": ["Capítulo 2 - Nomes significativos"]
        }
      ]
    }
  ]
}
```

### Logs

Os logs são exibidos no console durante a execução. Para logs mais detalhados, configure `EnableDetailedLogging: true` no `appsettings.json`.

## ⚙️ Configurações Avançadas

### Parâmetros do Ollama

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "qwen2.5-coder:7b",
  "TimeoutSeconds": 300,              // Timeout geral para requisições
  "AnalysisTimeoutSeconds": 300,      // Timeout para análise de código
  "SuggestionsTimeoutSeconds": 300,   // Timeout para geração de sugestões
  "MaxJsonFixRetries": 5,             // Tentativas de correção de JSON
  "EnableDetailedLogging": true,      // Logs detalhados
  "HealthCheckTimeoutSeconds": 30     // Timeout para health check
}
```

**Parâmetros do Modelo LLM:**
- `temperature`: 0.4 (controla criatividade - menor = mais determinístico)
- `top_p`: 0.95 (nucleus sampling)
- `top_k`: 60 (limita tokens considerados)
- `repeat_penalty`: 1.1 (penaliza repetições)

### Configuração do Worker

O worker pode ser configurado para ajustar o período de análise:

```csharp
// Em Worker.cs, linha ~51
var recentCommits = await _gitService.GetCommitsByPeriodAsync(
    DateTime.Now.AddDays(-400),  // Ajuste este valor
    DateTime.Now
);
```

### MongoDB

```json
"MongoDB": {
  "ConnectionString": "mongodb://admin:admin123@localhost:27017/?authSource=admin",
  "DatabaseName": "RefactorScore",
  "CollectionName": "AnaliseDeCommits"
}
```

**Nota**: A aplicação usa uma única coleção (`AnaliseDeCommits`) com subdocumentos para Files e Suggestions, seguindo o padrão de agregados do DDD.

### Configuração de GPU (NVIDIA)

Para melhor desempenho, configure o Docker para usar GPU NVIDIA:

#### **1. Instalar NVIDIA Container Toolkit**

**Windows (WSL2):**
```bash
# No WSL2 Ubuntu
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-docker2
sudo systemctl restart docker
```

**Linux:**
```bash
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

#### **2. Verificar GPU no Docker**

```bash
# Verificar se a GPU está disponível
docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi
```

#### **3. Executar sem GPU (CPU only)**

Se você não possui GPU NVIDIA, edite o `docker-compose.yml` e remova a seção `deploy`:

```yaml
ollama:
  image: ollama/ollama:latest
  container_name: refactorscore-ollama
  ports:
    - "11434:11434"
  volumes:
    - ollama_data:/root/.ollama
  environment:
    - OLLAMA_MODELS=/root/.ollama/models
  # Remova toda a seção 'deploy' abaixo
  restart: unless-stopped
```

**⚠️ Atenção**: Executar em CPU resultará em análises **muito mais lentas** (pode levar 5-10 minutos por arquivo ao invés de 30-60 segundos).

## 📊 Análise de Código

O RefactorScore avalia os seguintes aspectos do Clean Code (em uma escala de 0-10):

1. **Nomenclatura de Variáveis**: Avalia se os nomes das variáveis são claros, descritivos e seguem convenções de nomenclatura.
2. **Tamanho de Funções**: Avalia se as funções são pequenas, focadas e têm uma única responsabilidade.
3. **Uso de Comentários**: Verifica a presença e qualidade de comentários úteis (não código autoexplicativo).
4. **Coesão de Métodos**: Analisa se os métodos fazem uma coisa e se estão organizados logicamente.
5. **Ausência de Código Morto**: Identifica e penaliza código redundante ou não utilizado.

Cada análise produz:
- Notas individuais (0-10) para cada critério
- Uma nota geral (média de todos os critérios)
- Justificativa textual explicando a avaliação

## 📂 Estrutura do Projeto

```
RefactorScore/
├── docker-compose.yml        # Configuração dos serviços Docker
├── ModelFiles/               # Arquivos de configuração do modelo LLM
│   └── Modelfile             # Definição do modelo Ollama personalizado
├── src/                      # Código fonte
│   ├── Domain/               # Camada de domínio
│   │   ├── Entities/         # Entidades de domínio
│   │   ├── ValueObjects/     # Objetos de valor
│   │   ├── Enums/           # Enumerações
│   │   └── Interfaces/      # Interfaces de domínio
│   ├── Application/         # Camada de aplicação
│   │   └── Services/        # Serviços de aplicação
│   ├── Infrastructure/      # Camada de infraestrutura
│   │   ├── Repositories/    # Implementações de repositórios
│   │   ├── Services/        # Serviços de infraestrutura
│   │   └── Mappers/         # Mapeadores
│   ├── CrossCutting.IoC/    # Injeção de dependências
│   │   └── DependenceInjection/
│   └── WorkerService/       # Serviço de background
│       └── Worker.cs        # Implementação do worker
└── tests/                   # Projetos de teste
    ├── Domain.Tests/        # Testes unitários da camada de domínio
    ├── Application.Tests/   # Testes unitários da camada de aplicação
    └── Integration.Tests/   # Testes de integração
```

## 🧪 Testes

O projeto inclui testes abrangentes:

### Testes Unitários

```bash
dotnet test tests/Domain.Tests/RefactorScore.Domain.Tests.csproj
dotnet test tests/Application.Tests/RefactorScore.Application.Tests.csproj
```

### Testes de Integração

```bash
dotnet test tests/Integration.Tests/RefactorScore.Integration.Tests.csproj
```

**Nota**: Os testes de integração requerem a execução da infraestrutura (MongoDB, Redis e Ollama).

## 🛠 Desenvolvimento

### Compilando o Projeto

```bash
dotnet build
```

### Executando com Diferentes Configurações

Para ambiente de desenvolvimento:

```bash
dotnet run --project src/WorkerService/RefactorScore.WorkerService.csproj --environment Development
```

### Estendendo o Sistema

Para adicionar novos critérios de análise:
1. Estenda a classe `CleanCodeRating` na camada de domínio
2. Atualize o prompt do LLM no `Modelfile`
3. Modifique o `CommitAnalysisService` para tratar os novos critérios

## 📄 Licença

Este projeto está licenciado sob a MIT License - veja o arquivo LICENSE para detalhes.

## 📚 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para submeter um Pull Request.

1. Faça um fork do projeto
2. Crie sua feature branch (`git checkout -b feature/amazing-feature`)
3. Commit suas mudanças (`git commit -m 'Adiciona uma funcionalidade incrível'`)
4. Push para a branch (`git push origin feature/amazing-feature`)
5. Abra um Pull Request
