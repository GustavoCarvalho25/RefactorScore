
// Importa o framework Express para criar o servidor HTTP
import express from 'express'
import dotenv from 'dotenv'
import { MongoClient } from 'mongodb';

dotenv.config()

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
  // Nova implementação usando MongoDB
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({
      success: false,
      error: 'Variável de ambiente MONGODB_URI não definida ou inválida.'
    });
  }
  const client = new MongoClient(uri);
  (async () => {
    try {
      const { page = 1, limit = 10, status, autor, dataInicio, dataFim } = req.query;
      await client.connect();
  const db = client.db('local');
  const collection = db.collection('RefactorScore');

      // Monta filtro dinâmico
      const query = {};
      if (status) query.status = status;
      if (autor) query.autor = { $regex: autor, $options: 'i' };
      if (dataInicio || dataFim) {
        query.dataAnalise = {};
        if (dataInicio) query.dataAnalise.$gte = new Date(dataInicio);
        if (dataFim) query.dataAnalise.$lte = new Date(dataFim);
      }

      // Conta total filtrado
      const total = await collection.countDocuments(query);
      // Busca paginada e ordenada
      const analises = await collection
        .find(query)
        .sort({ dataAnalise: -1 })
        .skip((parseInt(page) - 1) * parseInt(limit))
        .limit(parseInt(limit))
        .toArray();

      res.json({
        success: true,
        data: analises,
        pagination: {
          page: parseInt(page),
          limit: parseInt(limit),
          total,
          totalPages: Math.ceil(total / parseInt(limit))
        }
      });
    } catch (error) {
      res.status(500).json({
        success: false,
        error: 'Erro interno do servidor',
        message: error.message
      });
    } finally {
      await client.close();
    }
  })();
})

// GET /api/analises-commits/{id} → Detalhes de uma análise específica
app.get('/api/analises-commits/:id', (req, res) => {
  // Busca análise específica no MongoDB
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({ success: false, error: 'Variável de ambiente MONGODB_URI não definida ou inválida.' });
  }
  const client = new MongoClient(uri);
  (async () => {
    try {
      const { id } = req.params;
      await client.connect();
      const db = client.db('local');
      const collection = db.collection('RefactorScore');
      const analise = await collection.findOne({ id: parseInt(id) });
      if (!analise) {
        return res.status(404).json({ success: false, error: 'Análise não encontrada' });
      }
      res.json({ success: true, data: analise });
    } catch (error) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})

// POST /api/analises-commits → Criar nova análise de commit
app.post('/api/analises-commits', (req, res) => {
  // Cria nova análise no MongoDB
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({ success: false, error: 'Variável de ambiente MONGODB_URI não definida ou inválida.' });
  }
  const client = new MongoClient(uri);
  (async () => {
    try {
      const { titulo, descricao, autor, status, pontuacaoRefatoracao, commitsAnalisados, recomendacoes } = req.body;
      if (!titulo || !autor) {
        return res.status(400).json({ success: false, error: 'Título e autor são obrigatórios' });
      }
      // Gera id único (timestamp + random)
      const novaAnalise = {
        id: Date.now(),
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
      };
      await client.connect();
      const db = client.db('local');
      const collection = db.collection('RefactorScore');
      await collection.insertOne(novaAnalise);
      res.status(201).json({ success: true, data: novaAnalise, message: 'Análise criada com sucesso' });
    } catch (error) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})

// PUT /api/analises-commits/{id} → Atualizar análise existente
app.put('/api/analises-commits/:id', (req, res) => {
  // Atualiza análise existente no MongoDB
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({ success: false, error: 'Variável de ambiente MONGODB_URI não definida ou inválida.' });
  }
  const client = new MongoClient(uri);
  (async () => {
    try {
      const { id } = req.params;
      const { titulo, descricao, autor, status, pontuacaoRefatoracao, commitsAnalisados, recomendacoes } = req.body;
      await client.connect();
      const db = client.db('local');
      const collection = db.collection('RefactorScore');
      const analise = await collection.findOne({ id: parseInt(id) });
      if (!analise) {
        return res.status(404).json({ success: false, error: 'Análise não encontrada' });
      }
      const analiseAtualizada = {
        ...analise,
        ...(titulo !== undefined && { titulo }),
        ...(descricao !== undefined && { descricao }),
        ...(autor !== undefined && { autor }),
        ...(status !== undefined && { status }),
        ...(pontuacaoRefatoracao !== undefined && { pontuacaoRefatoracao }),
        ...(commitsAnalisados !== undefined && { commitsAnalisados }),
        ...(recomendacoes !== undefined && { recomendacoes }),
        dataAtualizacao: new Date().toISOString()
      };
      await collection.replaceOne({ id: parseInt(id) }, analiseAtualizada);
      res.json({ success: true, data: analiseAtualizada, message: 'Análise atualizada com sucesso' });
    } catch (error) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})

// Rota de saúde da API
app.get('/health', (req, res) => {
  res.json({ status: 'OK', timestamp: new Date().toISOString() })
})

const PORT = process.env.PORT || 3000
app.listen(PORT, () => {
  console.log(`Servidor rodando na porta ${PORT}`)
})
