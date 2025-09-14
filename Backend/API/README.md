# RefactorScore API - Análises de Commits

API REST para gerenciamento de análises de commits usando Node.js e Express.

## Rotas Implementadas

### GET /api/analises-commits
Lista todas as análises de commits com suporte a filtros e paginação.

**Parâmetros de Query:**
- `page` (opcional): Página atual (padrão: 1)
- `limit` (opcional): Itens por página (padrão: 10)
- `status` (opcional): Filtrar por status (ex: "pendente", "concluida")
- `autor` (opcional): Filtrar por autor (busca parcial, case insensitive)
- `dataInicio` (opcional): Filtrar análises a partir desta data (formato ISO)
- `dataFim` (opcional): Filtrar análises até esta data (formato ISO)

**Exemplo de resposta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "titulo": "Análise de refatoração do módulo X",
      "descricao": "Análise detalhada dos commits...",
      "autor": "João Silva",
      "status": "concluida",
      "pontuacaoRefatoracao": 85,
      "commitsAnalisados": ["abc123", "def456"],
      "recomendacoes": ["Melhorar cobertura de testes"],
      "dataAnalise": "2025-09-14T12:00:00.000Z",
      "dataCriacao": "2025-09-14T10:00:00.000Z",
      "dataAtualizacao": "2025-09-14T12:00:00.000Z"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 10,
    "total": 25,
    "totalPages": 3
  }
}
```

### GET /api/analises-commits/{id}
Retorna os detalhes de uma análise específica.

**Parâmetros:**
- `id` (path): ID da análise

**Exemplo de resposta (sucesso):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "titulo": "Análise de refatoração do módulo X",
    "descricao": "Análise detalhada dos commits...",
    "autor": "João Silva",
    "status": "concluida",
    "pontuacaoRefatoracao": 85,
    "commitsAnalisados": ["abc123", "def456"],
    "recomendacoes": ["Melhorar cobertura de testes"],
    "dataAnalise": "2025-09-14T12:00:00.000Z",
    "dataCriacao": "2025-09-14T10:00:00.000Z",
    "dataAtualizacao": "2025-09-14T12:00:00.000Z"
  }
}
```

### POST /api/analises-commits
Cria uma nova análise de commits.

**Campos obrigatórios:**
- `titulo`: Título da análise
- `autor`: Autor da análise

**Campos opcionais:**
- `descricao`: Descrição detalhada
- `status`: Status da análise (padrão: "pendente")
- `pontuacaoRefatoracao`: Pontuação de 0-100 (padrão: 0)
- `commitsAnalisados`: Array de hashes de commits
- `recomendacoes`: Array de strings com recomendações

**Exemplo de request:**
```json
{
  "titulo": "Análise de refatoração do módulo X",
  "descricao": "Análise detalhada dos commits de refatoração",
  "autor": "João Silva",
  "status": "pendente",
  "pontuacaoRefatoracao": 0,
  "commitsAnalisados": ["abc123", "def456"],
  "recomendacoes": []
}
```

### PUT /api/analises-commits/{id}
Atualiza uma análise existente. Todos os campos são opcionais - apenas os fornecidos serão atualizados.

**Parâmetros:**
- `id` (path): ID da análise a ser atualizada

**Exemplo de request (atualizar apenas status e pontuação):**
```json
{
  "status": "concluida",
  "pontuacaoRefatoracao": 85,
  "recomendacoes": ["Melhorar cobertura de testes", "Adicionar documentação"]
}
```

## Como executar

1. Instalar dependências:
```bash
npm install
```

2. Executar o servidor:
```bash
node server.js
```

O servidor será iniciado na porta 3000 (ou na porta definida pela variável de ambiente `PORT`).

## Rota de saúde

### GET /health
Verifica se o servidor está funcionando.

**Resposta:**
```json
{
  "status": "OK",
  "timestamp": "2025-09-14T12:00:00.000Z"
}
```

## Tratamento de Erros

Todas as rotas incluem tratamento de erros consistente:

- **400 Bad Request**: Dados inválidos ou campos obrigatórios faltando
- **404 Not Found**: Análise não encontrada
- **500 Internal Server Error**: Erro interno do servidor

**Formato de erro:**
```json
{
  "success": false,
  "error": "Mensagem de erro",
  "message": "Detalhes técnicos do erro"
}
```

## Estrutura de Dados

Cada análise contém os seguintes campos:

- `id`: Identificador único (gerado automaticamente)
- `titulo`: Título da análise
- `descricao`: Descrição detalhada
- `autor`: Autor da análise
- `status`: Status atual ("pendente", "em_andamento", "concluida", etc.)
- `pontuacaoRefatoracao`: Pontuação de qualidade da refatoração (0-100)
- `commitsAnalisados`: Array com hashes dos commits analisados
- `recomendacoes`: Array com recomendações de melhoria
- `dataAnalise`: Data da análise (ISO string)
- `dataCriacao`: Data de criação do registro (ISO string)
- `dataAtualizacao`: Data da última atualização (ISO string)

## Observações

- Os dados são armazenados em memória (array) e serão perdidos ao reiniciar o servidor
- Em produção, recomenda-se usar um banco de dados persistente
- Todas as datas seguem o formato ISO 8601
- A paginação e filtros são aplicados na rota GET principal
