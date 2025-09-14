
// Importa o framework Express para criar o servidor HTTP
import express from 'express'

// Cria uma instância do aplicativo Express
const app = express()

// Middleware para parsing de JSON
app.use(express.json())

// Array para armazenar análises de commits (em produção, usaríamos um banco de dados)
let analisesCommits = []
let nextId = 1

// GET /api/analises-commits → Lista análises (com filtros 
// e paginação)
app.get('/api/analises-commits', (req, res) => {
  try {
    const { page = 1, limit = 10, status, autor, dataInicio, dataFim } = req.query
    let filteredAnalises = [...analisesCommits]

    // Aplicar filtros
    if (status) {
      filteredAnalises = filteredAnalises.filter(analise => analise.status === status)
    }
    if (autor) {
      filteredAnalises = filteredAnalises.filter(analise =>
        analise.autor.toLowerCase().includes(autor.toLowerCase())
      )
    }
    if (dataInicio) {
      const inicio = new Date(dataInicio)
      filteredAnalises = filteredAnalises.filter(analise =>
        new Date(analise.dataAnalise) >= inicio
      )
    }
    if (dataFim) {
      const fim = new Date(dataFim)
      filteredAnalises = filteredAnalises.filter(analise =>
        new Date(analise.dataAnalise) <= fim
      )
    }

    // Ordenar por data decrescente (mais recentes primeiro)
    filteredAnalises.sort((a, b) => new Date(b.dataAnalise) - new Date(a.dataAnalise))

    // Paginação
    const startIndex = (parseInt(page) - 1) * parseInt(limit)
    const endIndex = startIndex + parseInt(limit)
    const paginatedAnalises = filteredAnalises.slice(startIndex, endIndex)

    res.json({
      success: true,
      data: paginatedAnalises,
      pagination: {
        page: parseInt(page),
        limit: parseInt(limit),
        total: filteredAnalises.length,
        totalPages: Math.ceil(filteredAnalises.length / parseInt(limit))
      }
    })
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Erro interno do servidor',
      message: error.message
    })
  }
})

// GET /api/analises-commits/{id} → Detalhes de uma análise específica
app.get('/api/analises-commits/:id', (req, res) => {
  try {
    const { id } = req.params
    const analise = analisesCommits.find(a => a.id === parseInt(id))

    if (!analise) {
      return res.status(404).json({
        success: false,
        error: 'Análise não encontrada'
      })
    }

    res.json({
      success: true,
      data: analise
    })
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Erro interno do servidor',
      message: error.message
    })
  }
})

// POST /api/analises-commits → Criar nova análise de commit
app.post('/api/analises-commits', (req, res) => {
  try {
    const { titulo, descricao, autor, status, pontuacaoRefatoracao, commitsAnalisados, recomendacoes } = req.body

    // Validação básica
    if (!titulo || !autor) {
      return res.status(400).json({
        success: false,
        error: 'Título e autor são obrigatórios'
      })
    }

    const novaAnalise = {
      id: nextId++,
      titulo,
      descricao: descricao || '',
      autor,
      status: status || 'pendente',
      pontuacaoRefatoracao: pontuacaoRefatoracao || 0,
      commitsAnalisados: commitsAnalisados || [],
      recomendacoes: recomendacoes || [],
      dataAnalise: new Date().toISOString(),
      dataCriacao: new Date().toISOString(),
      dataAtualizacao: new Date().toISOString()
    }

    analisesCommits.push(novaAnalise)

    res.status(201).json({
      success: true,
      data: novaAnalise,
      message: 'Análise criada com sucesso'
    })
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Erro interno do servidor',
      message: error.message
    })
  }
})

// PUT /api/analises-commits/{id} → Atualizar análise existente
app.put('/api/analises-commits/:id', (req, res) => {
  try {
    const { id } = req.params
    const analiseIndex = analisesCommits.findIndex(a => a.id === parseInt(id))

    if (analiseIndex === -1) {
      return res.status(404).json({
        success: false,
        error: 'Análise não encontrada'
      })
    }

    const { titulo, descricao, autor, status, pontuacaoRefatoracao, commitsAnalisados, recomendacoes } = req.body

    // Atualizar apenas os campos fornecidos
    const analiseAtualizada = {
      ...analisesCommits[analiseIndex],
      ...(titulo !== undefined && { titulo }),
      ...(descricao !== undefined && { descricao }),
      ...(autor !== undefined && { autor }),
      ...(status !== undefined && { status }),
      ...(pontuacaoRefatoracao !== undefined && { pontuacaoRefatoracao }),
      ...(commitsAnalisados !== undefined && { commitsAnalisados }),
      ...(recomendacoes !== undefined && { recomendacoes }),
      dataAtualizacao: new Date().toISOString()
    }

    analisesCommits[analiseIndex] = analiseAtualizada

    res.json({
      success: true,
      data: analiseAtualizada,
      message: 'Análise atualizada com sucesso'
    })
  } catch (error) {
    res.status(500).json({
      success: false,
      error: 'Erro interno do servidor',
      message: error.message
    })
  }
})

// Rota de saúde da API
app.get('/health', (req, res) => {
  res.json({ status: 'OK', timestamp: new Date().toISOString() })
})

const PORT = process.env.PORT || 3000
app.listen(PORT, () => {
  console.log(`Servidor rodando na porta ${PORT}`)
})
