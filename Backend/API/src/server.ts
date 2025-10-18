import express, { Request, Response } from 'express'
import cors from 'cors'
import dotenv from 'dotenv'
import { MongoClient } from 'mongodb'

dotenv.config()

const app = express()

app.use(cors({
  origin: 'http://localhost:5173', 
  credentials: true
}))

app.use(express.json())


// Rota de saúde da API
app.get('/health', (req: Request, res: Response) => {
  res.json({ status: 'OK', timestamp: new Date().toISOString() })
})

// Rota principal de estatísticas: total de análises e nota média
app.get('/api/v1/analysis', (req: Request, res: Response) => {
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
      await client.connect();
      const db = client.db('local');
      const collection = db.collection('RefactorScore');

      // optional: filter to specific commit ids passed as comma-separated query param
      const commitIdsParam = (req.query.commitIds as string) || '';
      const commitIds = commitIdsParam.split(',').map(s => s.trim()).filter(s => s.length > 0);

      // Conta total de documentos (aplica filtro se commitIds foram informados)
      const total = commitIds.length > 0
        ? await collection.countDocuments({ _id: { $in: commitIds as any } })
        : await collection.countDocuments();

      // Calculando a média das notas dos commits
      // Cada commit tem um campo Rating.Note que representa a nota da avaliação
      const basePipeline: any[] = [
        // Projetar o campo da nota, tolerando diferentes nomenclaturas
        {
          $project: {
            commitNote: { 
              $ifNull: [
                '$Rating.Note',
                { $ifNull: [
                  '$rating.note',
                  { $ifNull: [
                    '$OverallNote',
                    { $ifNull: ['$overallNote', { $ifNull: ['$pontuacaoRefatoracao', 0] }] }
                  ]}
                ]}
              ]
            }
          }
        },
        // Agrupar todos os commits e calcular a média
        {
          $group: {
            _id: null,
            somaNotas: { $sum: { $cond: [{ $gt: ['$commitNote', 0] }, '$commitNote', 0] } },
            totalCommitsComNota: { $sum: { $cond: [{ $gt: ['$commitNote', 0] }, 1, 0] } }
          }
        },
        // Calcular a média: soma das notas / quantidade de commits com nota
        {
          $project: {
            average: {
              $cond: [
                { $gt: ['$totalCommitsComNota', 0] },
                { $round: [{ $divide: ['$somaNotas', '$totalCommitsComNota'] }, 2] },
                0
              ]
            }
          }
        }
      ];

      const pipeline = commitIds.length > 0 
        ? [{ $match: { _id: { $in: commitIds as any } } }, ...basePipeline] 
        : basePipeline;

      const agg = await collection.aggregate(pipeline).toArray();
      const averageNote = (agg && agg[0] && typeof agg[0].average === 'number') ? agg[0].average : 0;

      // Conta arquivos únicos por id dentro de Files/files
      const uniqueFilesPipeline: any[] = [];
      if (commitIds.length > 0) {
        uniqueFilesPipeline.push({ $match: { _id: { $in: commitIds } } });
      }
      uniqueFilesPipeline.push(
        { $project: { filesArr: { $ifNull: ['$Files', '$files'] } } },
        { $unwind: { path: '$filesArr', preserveNullAndEmptyArrays: false } },
        { $project: { fileId: { $ifNull: ['$filesArr._id', '$filesArr.id'] } } },
        { $match: { fileId: { $ne: null } } },
        { $group: { _id: '$fileId' } },
        { $group: { _id: null, count: { $sum: 1 } } }
      );

      const uniqueFilesAgg = await collection.aggregate(uniqueFilesPipeline).toArray();
      const uniqueFilesCount = (uniqueFilesAgg && uniqueFilesAgg[0] && typeof uniqueFilesAgg[0].count === 'number') ? uniqueFilesAgg[0].count : 0;

      // Conta frequência de linguagens dentro de Files/files de todos os documentos
      const languageFrequencyPipeline: any[] = [];
      if (commitIds.length > 0) {
        languageFrequencyPipeline.push({ $match: { _id: { $in: commitIds } } });
      }
      languageFrequencyPipeline.push(
        { $project: { filesArr: { $ifNull: ['$Files', '$files'] } } },
        { $unwind: { path: '$filesArr', preserveNullAndEmptyArrays: false } },
        { 
          $project: { 
            language: { 
              $ifNull: [
                '$filesArr.Language', 
                { $ifNull: ['$filesArr.language', 'Unknown'] }
              ] 
            } 
          } 
        },
        { $match: { language: { $ne: null } } },
        { $group: { _id: '$language', count: { $sum: 1 } } },
        { $sort: { count: -1 } },
        { 
          $project: {
            _id: 0,
            language: '$_id',
            count: 1
          }
        }
      );

      const languageFrequencyAgg = await collection.aggregate(languageFrequencyPipeline).toArray();
      const languageFrequency = languageFrequencyAgg.map((item: any) => ({
        language: item.language || 'Unknown',
        count: item.count || 0
      }));

      // Conta sugestões totais (campo Suggestions no nível do documento)
      const suggestionsPipeline: any[] = [];
      if (commitIds.length > 0) {
        suggestionsPipeline.push({ $match: { _id: { $in: commitIds } } });
      }
      suggestionsPipeline.push(
        { 
          $project: { 
            suggestions: { 
              $ifNull: [
                '$Suggestions', 
                { $ifNull: ['$suggestions', []] }
              ] 
            } 
          } 
        },
        { $unwind: { path: '$suggestions', preserveNullAndEmptyArrays: false } },
        { $group: { _id: null, count: { $sum: 1 } } }
      );

      const suggestionsAgg = await collection.aggregate(suggestionsPipeline).toArray();
      const totalSuggestions = (suggestionsAgg && suggestionsAgg[0] && typeof suggestionsAgg[0].count === 'number') 
        ? suggestionsAgg[0].count 
        : 0;

      res.json({ 
        success: true, 
        total, 
        averageNote, 
        uniqueFilesCount, 
        languageFrequency,
        totalSuggestions
      });
    } catch (error: any) {
      // On error, send a single 500 response. Do not reference `collection` here because it may be undefined.
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})

// Rota de debug para inspecionar documentos e notas calculadas
app.get('/api/v1/analysis/debug', (req: Request, res: Response) => {
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({ success: false, error: 'Variável de ambiente MONGODB_URI não definida ou inválida.' });
  }

  const client = new MongoClient(uri);
  (async () => {
    try {
      await client.connect();
      const db = client.db('local');
      const collection = db.collection('RefactorScore');

      const limit = parseInt((req.query.limit as string) || '5', 10);
      const docs = await collection.find({}).limit(limit).toArray();

      const result = docs.map((d: any) => {
        const filesArr: any[] = d.Files ?? d.files ?? [];

        const filesDetails = (filesArr || []).map((f: any) => {
          const fv1 = f.Rating?.VariableNaming ?? f.rating?.variableNaming ?? 0;
          const fv2 = f.Rating?.FunctionSizes ?? f.rating?.functionSizes ?? 0;
          const fv3 = f.Rating?.NoNeedsComments ?? f.rating?.noNeedsComments ?? 0;
          const fv4 = f.Rating?.MethodCohesion ?? f.rating?.methodCohesion ?? 0;
          const fv5 = f.Rating?.DeadCode ?? f.rating?.deadCode ?? 0;
          const perFileSum = (Number(fv1) || 0) + (Number(fv2) || 0) + (Number(fv3) || 0) + (Number(fv4) || 0) + (Number(fv5) || 0);

          return {
            fileId: f._id ?? f.id ?? null,
            path: f.Path ?? f.path ?? null,
            ratingFields: { v1: fv1, v2: fv2, v3: fv3, v4: fv4, v5: fv5 },
            perFileSum,
          };
        });

        const totalFiles = filesDetails.length;
  const commitAverage = totalFiles > 0 ? (filesDetails.reduce((s, x) => s + (Number(x.perFileSum) || 0), 0) / totalFiles) : 0;
  // escala 0-50 -> 0-100 e arredonda
  const commitScore = Math.round((commitAverage / 50) * 100);

        const overallNote = d.overallNote ?? null;
        const pontuacaoRefatoracao = d.pontuacaoRefatoracao ?? null;

        // nota efetiva usada para esse commit (prioriza commitAverage se > 0)
        const noteUsed = commitAverage > 0 ? commitAverage : (overallNote ?? (pontuacaoRefatoracao ?? 0));

        return {
          id: d._id,
          keys: Object.keys(d),
          totalFiles,
          files: filesDetails,
          commitAverage,
          commitScore,
          overallNote,
          pontuacaoRefatoracao,
          noteUsed,
        };
      });

      res.json({ success: true, sample: result });
    } catch (error: any) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})


const PORT = process.env.PORT || 3000
app.listen(PORT, () => {
  console.log(`Servidor rodando na porta ${PORT}`)
})
