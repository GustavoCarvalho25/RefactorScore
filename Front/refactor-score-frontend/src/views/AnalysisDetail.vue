<template>
  <div class="analysis-detail">
    <div v-if="loading" class="loading">Carregando análise...</div>

    <div v-else-if="error" class="error">{{ error }}</div>

    <div v-else-if="analyses.length > 0" class="detail-content">
      <header class="detail-header">
        <button @click="goBack" class="back-button">← Voltar</button>
        <div class="header-info">
          <h1>Análises de Commits</h1>
          <div class="meta-info">
            <span class="total-commits">{{ analyses.length }} commits analisados</span>
          </div>
        </div>
      </header>

      <!-- Container para cada commit -->
      <div v-for="(analysis, index) in analyses" :key="analysis.id" class="commit-container">
        <div class="analysis-container">
          <div class="commit-header">
            <h2>Commit {{ analysis.commitId ? analysis.commitId.substring(0, 8) : 'N/A' }}</h2>
            <div class="commit-meta">
              <span class="author" v-if="analysis.author">{{ analysis.author }}</span>
              <span class="date" v-if="analysis.analysisDate">{{ formatDate(analysis.analysisDate) }}</span>
              <span class="language-badge" v-if="getFirstLanguage(analysis)">{{ getFirstLanguage(analysis) }}</span>
            </div>
          </div>

      <div class="score-section">
        <div class="overall-score" :class="getScoreClass(getOverallNote(analysis))">
          <h2>Nota Geral</h2>
          <div class="score-value">{{ getOverallNote(analysis).toFixed(2) }}</div>
          <div class="score-quality">{{ getFirstFileRating(analysis)?.quality || 'N/A' }}</div>
        </div>

        <div class="metrics">
          <div class="metric">
            <span class="metric-label">Arquivos</span>
            <span class="metric-value">{{ getAnalysisFiles(analysis).length }}</span>
          </div>
          <div class="metric">
            <span class="metric-label">Linhas Adicionadas</span>
            <span class="metric-value added">+{{ getTotalAddedLines(analysis) }}</span>
          </div>
          <div class="metric">
            <span class="metric-label">Linhas Removidas</span>
            <span class="metric-value removed">-{{ getTotalRemovedLines(analysis) }}</span>
          </div>
          <div class="metric">
            <span class="metric-label">Sugestões</span>
            <span class="metric-value">{{ getAnalysisSuggestions(analysis).length }}</span>
          </div>
        </div>
      </div>

      <div v-if="getFirstFileRating(analysis)" class="rating-chart">
        <h2>Avaliação Clean Code</h2>
        <RadarChart
          :chart-id="`analysis-rating-${analysis.id}`"
          :rating="getFirstFileRating(analysis)!"
          title="Métricas de Qualidade"
        />
      </div>

      <div class="files-section">
        <h2>Arquivos Analisados</h2>
        <div class="files-list">
          <div
            v-for="file in getAnalysisFiles(analysis)"
            :key="file.fileId || file.id"
            class="file-card"
            :class="{ 'has-analysis': file.rating }"
          >
            <div class="file-header">
              <h3>{{ file.path }}</h3>
              <span class="file-language">{{ file.language }}</span>
            </div>
            <div class="file-stats">
              <span class="stat added">+{{ file.addedLines }}</span>
              <span class="stat removed">-{{ file.removedLines }}</span>
            </div>
            <div v-if="file.rating" class="file-rating">
              <div class="rating-item">
                <span>Variable Naming:</span>
                <span class="rating-value">{{ file.rating.variableNaming }}/10</span>
              </div>
              <div class="rating-item">
                <span>Function Sizes:</span>
                <span class="rating-value">{{ file.rating.functionSizes }}/10</span>
              </div>
              <div class="rating-item">
                <span>No Needs Comments:</span>
                <span class="rating-value">{{ file.rating.noNeedsComments }}/10</span>
              </div>
              <div class="rating-item">
                <span>Method Cohesion:</span>
                <span class="rating-value">{{ file.rating.methodCohesion }}/10</span>
              </div>
              <div class="rating-item">
                <span>Dead Code:</span>
                <span class="rating-value">{{ file.rating.deadCode }}/10</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="getAnalysisSuggestions(analysis).length > 0" class="suggestions-section">
        <h2>Sugestões de Melhoria</h2>
        <div class="suggestions-list">
          <div
            v-for="(suggestion, index) in getAnalysisSuggestions(analysis)"
            :key="index"
            class="suggestion-card"
            :class="`priority-${suggestion.priority.toLowerCase()}`"
          >
            <div class="suggestion-header">
              <h3>{{ suggestion.title }}</h3>
              <div class="suggestion-badges">
                <span class="badge priority">{{ suggestion.priority }}</span>
                <span class="badge difficulty">{{ suggestion.difficult }}</span>
                <span class="badge type">{{ suggestion.type }}</span>
              </div>
            </div>
            <p class="suggestion-description">{{ suggestion.description }}</p>
            <div class="suggestion-footer">
              <span class="file-ref">{{ suggestion.fileReference }}</span>
              <div v-if="suggestion.studyResources && suggestion.studyResources.length > 0" class="resources">
                <span>Recursos:</span>
                <a
                  v-for="(resource, idx) in suggestion.studyResources"
                  :key="idx"
                  :href="typeof resource === 'string' && resource.startsWith('http') ? resource : '#'"
                  :target="typeof resource === 'string' && resource.startsWith('http') ? '_blank' : undefined"
                  class="resource-link"
                >
                  {{ typeof resource === 'string' && resource.startsWith('http') ? `Link ${idx + 1}` : resource }}
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAnalysisService } from '../server/api/analysisService';
import { CommitAnalysis } from '../interfaces/CommitAnalysis';
import { Suggestion } from '../interfaces/Suggestion';
import { ApiResponse } from '../interfaces/ApiResponse';
import RadarChart from '../components/charts/RadarChart.vue';

const router = useRouter();
const route = useRoute();
const analysisService = useAnalysisService();

const analyses = ref<CommitAnalysis[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const getFirstLanguage = (analysis: CommitAnalysis): string => {
  const files = getAnalysisFiles(analysis);
  return files[0]?.language || 'N/A';
};

const getOverallNote = (analysis: CommitAnalysis): number => {
  // Usar o campo note diretamente da análise
  if (typeof analysis.note === 'number') {
    return analysis.note;
  }
  
  return 0;
};

const getFirstFileRating = (analysis: CommitAnalysis) => {
  if (analysis.filesDetails && analysis.filesDetails.length > 0) {
    return analysis.filesDetails[0]?.rating;
  }
  if (analysis.files && analysis.files.length > 0) {
    return analysis.files[0]?.rating;
  }
  return null;
};

const getAnalysisFiles = (analysis: CommitAnalysis) => {
  return analysis.filesDetails || analysis.files || [];
};

const getAnalysisSuggestions = (analysis: CommitAnalysis) => {
  // Agregador de sugestões de todos os arquivos
  if (analysis.filesDetails) {
    return analysis.filesDetails.reduce((allSuggestions: Suggestion[], file) => {
      if (file.suggestions) {
        allSuggestions.push(...file.suggestions);
      }
      return allSuggestions;
    }, []);
  }
  
  if (analysis.suggestions) {
    return analysis.suggestions;
  }
  
  return [];
};

const getTotalAddedLines = (analysis: CommitAnalysis): number => {
  const files = getAnalysisFiles(analysis);
  return files.reduce((total, file) => total + (file.addedLines || 0), 0);
};

const getTotalRemovedLines = (analysis: CommitAnalysis): number => {
  const files = getAnalysisFiles(analysis);
  return files.reduce((total, file) => total + (file.removedLines || 0), 0);
};

const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const getScoreClass = (score: number) => {
  if (score >= 9) return 'score-excellent';
  if (score >= 7.5) return 'score-very-good';
  if (score >= 6) return 'score-good';
  if (score >= 5) return 'score-acceptable';
  return 'score-needs-improvement';
};

const goBack = () => {
  router.back();
};

const loadAnalysis = async () => {
  const id = route.params.id as string;
  if (!id) {
    error.value = 'ID da análise não fornecido';
    return;
  }

  loading.value = true;
  error.value = null;

  try {
    const { result, error: apiError } = await analysisService.getAnalysisById(id);
    if (apiError.value) {
      error.value = 'Erro ao carregar análise';
      console.error('Error loading analysis:', apiError.value);
    } else if (result.value?.success && result.value.data) {
      // Novo formato: retorna um único objeto em data
      analyses.value = [result.value.data];
    } else {
      error.value = 'Formato de resposta inválido';
      console.error('Formato inesperado:', result.value);
    }
    console.log('Análises carregadas:', analyses.value);
  } catch (err) {
    error.value = 'Erro ao carregar análise';
    console.error('Error loading analysis:', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadAnalysis();
});
</script>

<style scoped lang="scss">
.analysis-detail {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.commit-container {
  background: var(--card-background);
  border-radius: 12px;
  box-shadow: 0 2px 8px var(--shadow-color);
  margin-bottom: 2rem;
  padding: 2rem;
  transition: all 0.3s ease;

  &:last-child {
    margin-bottom: 0;
  }
}

.commit-header {
  margin-bottom: 2rem;
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 1rem;

  h2 {
    font-size: 1.5rem;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
    font-family: monospace;
    transition: color 0.3s ease;
  }

  .commit-meta {
    display: flex;
    gap: 1rem;
    align-items: center;
    flex-wrap: wrap;
    color: var(--text-secondary);
    font-size: 0.9rem;
    transition: color 0.3s ease;

    .author {
      font-weight: 500;
    }

    .date {
      opacity: 0.8;
    }

    .language-badge {
      padding: 0.25rem 0.75rem;
      background: var(--primary-color);
      color: white;
      border-radius: 12px;
      font-size: 0.85rem;
    }
  }
}

.loading,
.error {
  text-align: center;
  padding: 3rem;
  font-size: 1.1rem;
  color: var(--text-primary);
  transition: color 0.3s ease;
}

.error {
  color: var(--danger-color);
}

.detail-header {
  margin-bottom: 2rem;

  .back-button {
    background: none;
    border: none;
    color: var(--primary-color);
    font-size: 1rem;
    cursor: pointer;
    padding: 0.5rem 0;
    margin-bottom: 1rem;
    transition: color 0.3s ease;

    &:hover {
      opacity: 0.8;
    }
  }

  h1 {
    font-size: 2rem;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
    transition: color 0.3s ease;
  }

  .meta-info {
    display: flex;
    gap: 1rem;
    align-items: center;
    flex-wrap: wrap;

    .author {
      font-weight: 500;
      color: var(--text-primary);
      transition: color 0.3s ease;
    }

    .date {
      color: var(--text-secondary);
      transition: color 0.3s ease;
    }

    .language-badge {
      padding: 0.25rem 0.75rem;
      background: var(--primary-color);
      color: white;
      border-radius: 12px;
      font-size: 0.85rem;
    }
  }
}

.score-section {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 2rem;
  margin-bottom: 2rem;

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

.overall-score {
  background: var(--card-background);
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  text-align: center;
  transition: all 0.3s ease;

  h2 {
    font-size: 1.2rem;
    color: var(--text-primary);
    margin-bottom: 1rem;
    transition: color 0.3s ease;
  }

  .score-value {
    font-size: 4rem;
    font-weight: bold;
    margin-bottom: 0.5rem;
  }

  .score-quality {
    font-size: 1.1rem;
    font-weight: 500;
    text-transform: uppercase;
  }
}

.metrics {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 1rem;
}

.metric {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  text-align: center;
  transition: all 0.3s ease;

  .metric-label {
    display: block;
    font-size: 0.85rem;
    color: var(--text-secondary);
    margin-bottom: 0.5rem;
    text-transform: uppercase;
    transition: color 0.3s ease;
  }

  .metric-value {
    font-size: 2rem;
    font-weight: bold;
    color: var(--text-primary);
    transition: color 0.3s ease;

    &.added {
      color: var(--success-color);
    }

    &.removed {
      color: var(--danger-color);
    }
  }
}

.rating-chart {
  background: var(--card-background);
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  margin-bottom: 2rem;
  min-height: 500px;
  transition: all 0.3s ease;

  h2 {
    margin-bottom: 1rem;
    color: var(--text-primary);
    transition: color 0.3s ease;
  }
}

.files-section,
.suggestions-section {
  margin-bottom: 2rem;

  h2 {
    font-size: 1.5rem;
    color: var(--text-primary);
    margin-bottom: 1rem;
    transition: color 0.3s ease;
  }
}

.files-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.file-card {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  border-left: 4px solid var(--border-color);
  transition: all 0.3s ease;

  &.has-analysis {
    border-left-color: var(--primary-color);
  }

  .file-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;

    h3 {
      font-size: 1rem;
      color: var(--text-primary);
      font-family: monospace;
      transition: color 0.3s ease;
    }

    .file-language {
      padding: 0.25rem 0.75rem;
      background: var(--light-gray);
      border-radius: 12px;
      font-size: 0.85rem;
      color: var(--text-secondary);
      transition: all 0.3s ease;
    }
  }

  .file-stats {
    display: flex;
    gap: 1rem;
    margin-bottom: 1rem;

    .stat {
      font-weight: 500;

      &.added {
        color: var(--success-color);
      }

      &.removed {
        color: var(--danger-color);
      }
    }
  }

  .file-rating {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.5rem;
    padding-top: 1rem;
    border-top: 1px solid var(--border-color);

    .rating-item {
      display: flex;
      justify-content: space-between;
      font-size: 0.9rem;
      color: var(--text-secondary);
      transition: color 0.3s ease;

      .rating-value {
        font-weight: bold;
        color: var(--primary-color);
      }
    }
  }
}

.suggestions-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.suggestion-card {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  border-left: 4px solid var(--border-color);
  transition: all 0.3s ease;

  &.priority-high {
    border-left-color: var(--danger-color);
  }

  &.priority-medium {
    border-left-color: var(--warning-color);
  }

  &.priority-low {
    border-left-color: var(--info-color);
  }

  .suggestion-header {
    display: flex;
    justify-content: space-between;
    align-items: start;
    margin-bottom: 1rem;

    h3 {
      font-size: 1.1rem;
      color: var(--text-primary);
      flex: 1;
      transition: color 0.3s ease;
    }

    .suggestion-badges {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;

      .badge {
        padding: 0.25rem 0.75rem;
        border-radius: 12px;
        font-size: 0.75rem;
        font-weight: 500;
        text-transform: uppercase;

        &.priority {
          background: #e74c3c;
          color: white;
        }

        &.difficulty {
          background: #f39c12;
          color: white;
        }

        &.type {
          background: #3498db;
          color: white;
        }
      }
    }
  }

  .suggestion-description {
    color: var(--text-secondary);
    line-height: 1.6;
    margin-bottom: 1rem;
    transition: color 0.3s ease;
  }

  .suggestion-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-top: 1rem;
    border-top: 1px solid var(--border-color);
    font-size: 0.85rem;

    .file-ref {
      color: var(--text-secondary);
      font-family: monospace;
      transition: color 0.3s ease;
    }

    .resources {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      color: var(--text-secondary);
      transition: color 0.3s ease;

      .resource-link {
        color: var(--primary-color);
        text-decoration: none;

        &:hover {
          text-decoration: underline;
        }
      }
    }
  }
}

.score-excellent {
  .score-value,
  .score-quality {
    color: #27ae60;
  }
}

.score-very-good {
  .score-value,
  .score-quality {
    color: #2ecc71;
  }
}

.score-good {
  .score-value,
  .score-quality {
    color: #f39c12;
  }
}

.score-acceptable {
  .score-value,
  .score-quality {
    color: #e67e22;
  }
}

.score-needs-improvement {
  .score-value,
  .score-quality {
    color: #e74c3c;
  }
}
</style>
