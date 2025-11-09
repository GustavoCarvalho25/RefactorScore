import express from 'express' // Importa o valor runtime do Express (a função app)
import type { Request, Response } from 'express' // Importa APENAS os tipos para TypeScript
import cors from 'cors'
import dotenv from 'dotenv'
import { MongoClient } from 'mongodb'

dotenv.config()

const app = express()

app.use(cors({
  origin: 'http://RefactorScorehost:5173',
  credentials: true
}))

app.use(express.json())


// Rota de saúde da API
app.get('/health', (req: Request, res: Response) => {
  res.json({ status: 'OK', timestamp: new Date().toISOString() })
})

// Rota principal de estatísticas: total de análises e nota média
app.get('/api/v1/main', (req: Request, res: Response) => {
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
      const db = client.db('RefactorScore');
      const collection = db.collection('CommitAnalysis');

      const projectFilter = (req.query.project as string) || '';
      
      // optional: filter to specific commit ids passed as comma-separated query param
      const commitIdsParam = (req.query.commitIds as string) || '';
      const commitIds = commitIdsParam.split(',').map(s => s.trim()).filter(s => s.length > 0);

      // Construir filtro base
      const baseFilter: any = {};
      if (projectFilter) {
        baseFilter.Project = projectFilter;
      }
      if (commitIds.length > 0) {
        baseFilter._id = { $in: commitIds as any };
      }

      // Conta total de documentos (aplica filtros)
      const total = await collection.countDocuments(baseFilter);

      // Conta total de arquivos analisados em todos os commits
      const filesCountPipeline: any[] = [];
      if (Object.keys(baseFilter).length > 0) {
        filesCountPipeline.push({ $match: baseFilter });
      }
      filesCountPipeline.push(
        { $project: { files: { $ifNull: ['$Files', '$files'] } } },
        { $unwind: { path: '$files', preserveNullAndEmptyArrays: false } },
        { $group: { _id: null, count: { $sum: 1 } } }
      );
      
      const filesCountAgg = await collection.aggregate(filesCountPipeline).toArray();
      const totalFilesAnalyzed = (filesCountAgg && filesCountAgg[0] && typeof filesCountAgg[0].count === 'number')
        ? filesCountAgg[0].count
        : 0;

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

      const pipeline = Object.keys(baseFilter).length > 0
        ? [{ $match: baseFilter }, ...basePipeline]
        : basePipeline;

      const agg = await collection.aggregate(pipeline).toArray();
      const averageNote = (agg && agg[0] && typeof agg[0].average === 'number') ? agg[0].average : 0;

      // Conta arquivos únicos por id dentro de Files/files
      const uniqueFilesPipeline: any[] = [];
      if (Object.keys(baseFilter).length > 0) {
        uniqueFilesPipeline.push({ $match: baseFilter });
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
      if (Object.keys(baseFilter).length > 0) {
        languageFrequencyPipeline.push({ $match: baseFilter });
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

      // Buscar commits individuais com suas notas para gráfico de evolução
      const commitsPipeline: any[] = [];
      if (commitIds.length > 0) {
        commitsPipeline.push({ $match: { _id: { $in: commitIds } } });
      }
      commitsPipeline.push(
        {
          $project: {
            _id: 1,
            commitId: { $ifNull: ['$CommitId', '$commitId'] },
            author: { $ifNull: ['$Author', '$author'] },
            commitDate: { $ifNull: ['$CommitDate', '$commitDate'] },
            analysisDate: { $ifNull: ['$AnalysisDate', '$analysisDate'] },
            language: { $ifNull: ['$Language', '$language'] },
            note: {
              $ifNull: [
                '$OverallNote',
                { $ifNull: [
                  '$overallNote',
                  0
                ]}
              ]
            },
            quality: {
              $ifNull: [
                '$Rating.Quality',
                { $ifNull: ['$rating.quality', 'Unknown'] }
              ]
            }
          }
        },
        { $sort: { commitDate: 1 } } // Ordena por data para mostrar evolução temporal
      );

      const commitsAgg = await collection.aggregate(commitsPipeline).toArray();
      const commitsEvolution = commitsAgg.map((commit: any) => {
        // Extrair data do formato {"$date": "..."} se necessário
        const extractDate = (dateField: any) => {
          if (!dateField) return null;
          if (typeof dateField === 'string') return dateField;
          if (dateField.$date) return dateField.$date;
          if (dateField instanceof Date) return dateField.toISOString();
          return dateField;
        };

        return {
          id: commit._id,
          commitId: commit.commitId || null,
          commitDate: extractDate(commit.commitDate),
          analysisDate: extractDate(commit.analysisDate),
          note: commit.note || 0
        };
      });

      res.json({
        success: true,
        total,
        totalFilesAnalyzed,
        averageNote,
        uniqueFilesCount,
        languageFrequency,
        totalSuggestions,
        commitsEvolution
      });
    } catch (error: any) {
      // On error, send a single 500 response. Do not reference `collection` here because it may be undefined.
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
})

app.get('/api/v1/statistics', (req: Request, res: Response) => {
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
      const db = client.db('RefactorScore');
      const collection = db.collection('CommitAnalysis');

      // Buscar commits individuais com suas notas, metadados e métricas
      const commitsPipeline = [
        {
          $project: {
            _id: 1,
            author: { $ifNull: ['$Author', '$author'] },
            language: { $ifNull: ['$Language', '$language'] },
            note: {
              $ifNull: [
                '$OverallNote',
                { $ifNull: [
                  '$overallNote',
                  0
                ]}
              ]
            },
            quality: {
              $ifNull: [
                '$Rating.Quality',
                { $ifNull: ['$rating.quality', 'Unknown'] }
              ]
            },
            files: { $ifNull: ['$Files', '$files'] }
          }
        }
      ];

      const commitsAgg = await collection.aggregate(commitsPipeline).toArray();
      const commitsStats = commitsAgg.map((commit: any) => {
        const filesArr = commit.files || [];
        const metrics = filesArr.reduce((acc: any, file: any) => {
          const rating = file.Rating || file.rating || {};
          acc.variableNaming += Number(rating.VariableNaming || rating.variableNaming || 0);
          acc.functionSizes += Number(rating.FunctionSizes || rating.functionSizes || 0);
          acc.noNeedsComments += Number(rating.NoNeedsComments || rating.noNeedsComments || 0);
          acc.methodCohesion += Number(rating.MethodCohesion || rating.methodCohesion || 0);
          acc.deadCode += Number(rating.DeadCode || rating.deadCode || 0);
          return acc;
        }, {
          variableNaming: 0,
          functionSizes: 0,
          noNeedsComments: 0,
          methodCohesion: 0,
          deadCode: 0
        });

        // Calcular média das métricas pelo número de arquivos
        const numFiles = filesArr.length || 1;
        Object.keys(metrics).forEach(key => {
          metrics[key] = Number((metrics[key] / numFiles).toFixed(2));
        });

        return {
          id: commit._id,
          author: commit.author || 'Unknown',
          language: commit.language || 'Unknown',
          note: commit.note || 0,
          quality: commit.quality || 'Unknown',
          metrics: metrics
        };
      });

      // Calcular melhor e pior nota
      const notasValidas = commitsStats.map(c => c.note).filter(n => n > 0);
      const bestNote = notasValidas.length > 0 ? Math.max(...notasValidas) : 0;
      const worstNote = notasValidas.length > 0 ? Math.min(...notasValidas) : 0;

      res.json({
        success: true,
        bestNote,
        worstNote,
        commits: commitsStats
      });
    } catch (error: any) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
});

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
      const db = client.db('RefactorScore');
      const collection = db.collection('CommitAnalysis');

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

// Endpoint para listar todos os projetos com suas métricas
app.get('/api/v1/projects', (req: Request, res: Response) => {
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
      const db = client.db('RefactorScore');
      const collection = db.collection('CommitAnalysis');

      // Buscar todos os projetos únicos
      const projectNames = await collection.distinct('Project');

      // Para cada projeto, agregar suas métricas
      const projectsWithMetrics = await Promise.all(
        projectNames.map(async (projectName: string) => {
          // Pipeline de agregação para calcular métricas do projeto
          const metricsPipeline = [
            { $match: { Project: projectName } },
            {
              $group: {
                _id: '$Project',
                totalCommits: { $sum: 1 },
                lastAnalysisDate: { $max: '$AnalysisDate' },
                totalFiles: { $sum: { $size: { $ifNull: ['$Files', []] } } },
                // Calcular média das notas
                avgNote: {
                  $avg: {
                    $ifNull: [
                      '$Rating.Note',
                      { $ifNull: ['$OverallNote', 0] }
                    ]
                  }
                }
              }
            }
          ];

          const metricsResult = await collection.aggregate(metricsPipeline).toArray();
          const metrics = metricsResult[0] || {};

          // Buscar linguagem mais comum no projeto
          const languagePipeline = [
            { $match: { Project: projectName } },
            { $group: { _id: '$Language', count: { $sum: 1 } } },
            { $sort: { count: -1 } },
            { $limit: 1 }
          ];

          const languageResult = await collection.aggregate(languagePipeline).toArray();
          const mainLanguage = languageResult[0]?._id || 'Unknown';

          return {
            name: projectName,
            totalCommits: metrics.totalCommits || 0,
            averageNote: Math.round((metrics.avgNote || 0) * 100) / 100,
            lastAnalysisDate: metrics.lastAnalysisDate || null,
            mainLanguage: mainLanguage,
            totalFiles: metrics.totalFiles || 0
          };
        })
      );

      // Ordenar por data de última análise (mais recente primeiro)
      projectsWithMetrics.sort((a, b) => {
        if (!a.lastAnalysisDate) return 1;
        if (!b.lastAnalysisDate) return -1;
        return new Date(b.lastAnalysisDate).getTime() - new Date(a.lastAnalysisDate).getTime();
      });

      res.json({ 
        success: true, 
        projects: projectsWithMetrics 
      });
    } catch (error: any) {
      res.status(500).json({ 
        success: false, 
        error: 'Erro ao buscar projetos', 
        message: error.message 
      });
    } finally {
      await client.close();
    }
  })();
});

app.get('/api/v1/analysis', (req: Request, res: Response) => {
  const uri = process.env.MONGODB_URI;
  if (!uri || typeof uri !== 'string' || uri.trim() === '') {
    return res.status(500).json({ success: false, error: 'Variável de ambiente MONGODB_URI não definida ou inválida.' });
  }

  const client = new MongoClient(uri);
  (async () => {
    try {
      await client.connect();
      const db = client.db('RefactorScore');
      const collection = db.collection('CommitAnalysis');
      
      // Filtro por projeto (query param ?project=)
      const projectFilter = (req.query.project as string) || '';
      const filter: any = {};
      if (projectFilter) {
        filter.Project = projectFilter;
      }
      
      const limit = parseInt((req.query.limit as string) || '5', 10);
      const docs = await collection.find(filter).limit(limit).toArray();
      const result = docs.map((d: any) => {
        const filesArr: any[] = d.Files ?? d.files ?? [];
        const filesDetails = (filesArr || []).map((f: any) => {
          // Extract rating fields with fallbacks
          const rating = f.Rating ?? f.rating ?? {};
          const fv1 = rating.VariableNaming ?? rating.variableNaming ?? 0;
          const fv2 = rating.FunctionSizes ?? rating.functionSizes ?? 0;
          const fv3 = rating.NoNeedsComments ?? rating.noNeedsComments ?? 0;
          const fv4 = rating.MethodCohesion ?? rating.methodCohesion ?? 0;
          const fv5 = rating.DeadCode ?? rating.deadCode ?? 0;
          const perFileSum = (Number(fv1) || 0) + (Number(fv2) || 0) + (Number(fv3) || 0) + (Number(fv4) || 0) + (Number(fv5) || 0);

          // Extract justifications with proper fallbacks
          const justifications = rating.Justifications ?? rating.justifications ?? {};

          // Extract suggestions with proper array handling
          const suggestions = (f.Suggestions ?? f.suggestions ?? []).map((s: any) => ({
            title: s.Title ?? s.title,
            description: s.Description ?? s.description,
            priority: s.Priority ?? s.priority,
            type: s.Type ?? s.type,
            difficult: s.Difficult ?? s.difficult,
            fileReference: s.FileReference ?? s.fileReference,
            lastUpdate: s.LastUpdate ?? s.lastUpdate,
            studyResources: s.StudyResources ?? s.studyResources ?? []
          }));

          return {
            fileId: f._id ?? f.id ?? null,
            path: f.Path ?? f.path,
            language: f.Language ?? f.language,
            addedLines: f.AddedLines ?? f.addedLines ?? 0,
            removedLines: f.RemovedLines ?? f.removedLines ?? 0,
            content: f.Content ?? f.content,
            rating: {
              variableNaming: fv1,
              functionSizes: fv2,
              noNeedsComments: fv3,
              methodCohesion: fv4,
              deadCode: fv5,
              note: rating.Note ?? rating.note ?? 0,
              quality: rating.Quality ?? rating.quality ?? 'Unknown',
              justifications: {
                variableNaming: justifications.VariableNaming ?? justifications.variableNaming,
                functionSizes: justifications.FunctionSizes ?? justifications.functionSizes,
                noNeedsComments: justifications.NoNeedsComments ?? justifications.noNeedsComments,
                methodCohesion: justifications.MethodCohesion ?? justifications.methodCohesion,
                deadCode: justifications.DeadCode ?? justifications.deadCode
              }
            },
            suggestions,
            perFileSum,
          };
        });

        const totalFiles = filesDetails.length;
        const commitAverage = totalFiles > 0 ? (filesDetails.reduce((s, x) => s + (Number(x.perFileSum) || 0), 0) / totalFiles) : 0;

        // Get commit note with fallbacks like in main route
        const commitNote = d.Rating?.Note ?? d.rating?.note ?? d.OverallNote ?? d.overallNote ?? d.pontuacaoRefatoracao ?? 0;
        
        return {
          id: d._id,
          author: d.Author ?? d.author ?? 'Unknown',
          commitId: d.CommitId ?? d.commitId,
          analysisDate: d.AnalysisDate ?? d.analysisDate,
          OverallNote: d.OverallNote ?? d.overallNote ?? 0,
          filesDetails
        };
      });

      res.json({ success: true, analysis: result });
    } catch (error: any) {
      res.status(500).json({ success: false, error: 'Erro interno do servidor', message: error.message });
    } finally {
      await client.close();
    }
  })();
});

const PORT = process.env.PORT || 3000
app.listen(PORT, () => {
  console.log(`Servidor rodando na porta ${PORT}`)
})


