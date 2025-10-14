# RefactorScore Frontend

Frontend application for RefactorScore - A Clean Code Analysis tool for Git commits.

## 🎯 Sobre o Projeto

RefactorScore é uma aplicação de análise de commits que utiliza IA para avaliar a qualidade do código seguindo os princípios do Clean Code. Este frontend foi desenvolvido como parte do projeto de TCC e exibe os resultados das análises realizadas pelo Worker backend.

## 🚀 Tecnologias

- **Vue 3** - Framework JavaScript progressivo
- **TypeScript** - Superset tipado do JavaScript
- **Vite** - Build tool e dev server
- **Pinia** - Gerenciamento de estado
- **Vue Router** - Roteamento
- **Chart.js** - Biblioteca de gráficos
- **Axios** - Cliente HTTP
- **SCSS** - Pré-processador CSS

## 📋 Estrutura do Projeto

```
src/
├── assets/          # Recursos estáticos (estilos, imagens)
├── components/      # Componentes reutilizáveis
│   └── charts/      # Componentes de gráficos
├── composables/     # Composables Vue (lógica reutilizável)
├── interfaces/      # Interfaces TypeScript
├── plugins/         # Plugins Vue
├── router/          # Configuração de rotas
├── server/          # Serviços de API
│   └── api/         # Serviços específicos
├── stores/          # Stores Pinia
├── utils/           # Funções utilitárias
└── views/           # Views/Páginas da aplicação
```

## 🎨 Funcionalidades

### Dashboard
- Visão geral das análises
- Estatísticas gerais (total de análises, nota média, etc.)
- Gráficos de evolução da qualidade
- Distribuição por linguagem
- Análises recentes

### Análises
- Lista de todas as análises de commits
- Filtros por autor e linguagem
- Busca por commit ou autor
- Visualização de métricas por análise

### Detalhes da Análise
- Informações completas do commit
- Gráfico radar com métricas Clean Code
- Lista de arquivos analisados
- Sugestões de melhoria detalhadas
- Recursos de estudo

### Estatísticas
- Distribuição de qualidade dos commits
- Evolução temporal das notas
- Métricas médias de Clean Code
- Top autores por quantidade de commits

## 🔧 Instalação

1. Clone o repositório:
```bash
git clone <repository-url>
cd refactor-score-frontend
```

2. Instale as dependências:
```bash
npm install
```

3. Configure as variáveis de ambiente:
```bash
cp .env.example .env
```

Edite o arquivo `.env` e configure a URL da API:
```
VITE_API_URL=http://localhost:5000
```

4. Execute o projeto em modo de desenvolvimento:
```bash
npm run dev
```

5. Build para produção:
```bash
npm run build
```

## 📊 Entidades do Domínio

### CommitAnalysis
Representa uma análise completa de um commit:
- Informações do commit (ID, autor, data)
- Linguagem de programação
- Linhas adicionadas/removidas
- Arquivos analisados
- Sugestões de melhoria
- Rating geral

### CleanCodeRating
Métricas de qualidade do código:
- Variable Naming (1-10)
- Function Sizes (1-10)
- No Needs Comments (1-10)
- Method Cohesion (1-10)
- Dead Code (1-10)
- Nota calculada
- Qualidade (Excellent, Very Good, Good, etc.)

### CommitFile
Arquivo individual analisado:
- Caminho do arquivo
- Linguagem
- Linhas adicionadas/removidas
- Conteúdo
- Rating específico
- Sugestões

### Suggestion
Sugestão de melhoria:
- Título e descrição
- Prioridade (High, Medium, Low)
- Tipo
- Dificuldade (Easy, Medium, Hard)
- Referência ao arquivo
- Recursos de estudo

## 🎨 Padrões de Código

Este projeto segue os mesmos padrões do projeto frontend-v2:

- **Composition API** do Vue 3
- **TypeScript** para tipagem estática
- **SCSS** para estilos
- **Pinia** para gerenciamento de estado
- **Axios** para requisições HTTP
- Estrutura de pastas organizada por funcionalidade

## 📝 Scripts Disponíveis

```bash
# Desenvolvimento
npm run dev

# Build
npm run build

# Preview do build
npm run preview

# Lint
npm run lint
```

## 🔗 Integração com Backend

O frontend se comunica com a API backend através do serviço `analysisService`, que fornece os seguintes endpoints:

- `GET /api/v1/analysis` - Lista todas as análises
- `GET /api/v1/analysis/:id` - Detalhes de uma análise
- `GET /api/v1/analysis/commit/:commitId` - Análise por commit ID
- `GET /api/v1/analysis/statistics` - Estatísticas gerais
- `GET /api/v1/analysis/date-range` - Análises por período
- `GET /api/v1/analysis/author/:author` - Análises por autor

## 📄 Licença

Este projeto faz parte de um TCC (Trabalho de Conclusão de Curso).

## 👨‍💻 Autor

Desenvolvido como projeto de TCC sobre análise de commits seguindo critérios do Clean Code.
