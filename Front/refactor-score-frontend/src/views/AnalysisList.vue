<template>
<div class="analysis-list-page">
    <header class="page-header">
      <h1>Análises de Commits</h1>
      <div class="filters">
        <div class="search-container">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Buscar por autor ou commit..."
            class="search-input"
            @keyup.enter="searchByCommitId"
          />
          <button @click="searchByCommitId" class="search-button" title="Buscar">
            🔍
          </button>
        </div>
        <select v-model="selectedLanguage" class="language-filter">
          <option value="">Todas as linguagens</option>
          <option v-for="lang in languages" :key="lang" :value="lang">
            {{ lang }}
          </option>
        </select>
      </div>
    </header>

    <div v-show="loading" class="loading">Carregando análises...</div>

    <div v-show="error" class="error">{{ error }}</div>

    <div v-show="!loading && !error" class="commits-list">
      <div v-for="analysis in filteredAnalyses" :key="analysis.id" class="commit-container">
        <div class="analysis-container" :class="{ 'expanded': isCommitExpanded(analysis.commitId || analysis.id || '') }">
          <div class="commit-header" @click="toggleCommit(analysis.commitId || analysis.id || '')">
            <div class="commit-title">
              <span class="expand-icon">{{ isCommitExpanded(analysis.commitId || analysis.id || '') ? '▼' : '▶' }}</span>
              <h2>Commit {{ (analysis.commitId || analysis.id || '').substring(0, 8) }}</h2>
            </div>
            <div class="commit-meta">
              <span class="author" v-if="analysis.author">{{ analysis.author }}</span>
              <span class="date" v-if="analysis.analysisDate">{{ formatDate(analysis.analysisDate) }}</span>
              <span class="language-badge" v-if="getFirstLanguage(analysis)">{{ getFirstLanguage(analysis) }}</span>
            </div>
          </div>

          <div v-show="isCommitExpanded(analysis.commitId || analysis.id || '')" class="commit-details">
            <div class="score-section">
              <div class="overall-score" :class="getScoreClass(getOverallNote(analysis))">
                <h2>Nota Geral</h2>
                <div class="score-value">{{ getOverallNote(analysis).toFixed(2) }}</div>
                <div class="score-quality">{{ getFirstFileRating(analysis)?.quality ? translateQuality(getFirstFileRating(analysis).quality) : 'N/A' }}</div>
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
                      <span>{{ translateMetric('Variable Naming') }}:</span>
                      <span class="rating-value">{{ file.rating?.variableNaming ?? 'N/A' }}/10</span>
                    </div>
                    <div class="rating-item">
                      <span>{{ translateMetric('Function Sizes') }}:</span>
                      <span class="rating-value">{{ file.rating?.functionSizes ?? 'N/A' }}/10</span>
                    </div>
                    <div class="rating-item">
                      <span>{{ translateMetric('No Needs Comments') }}:</span>
                      <span class="rating-value">{{ file.rating?.noNeedsComments ?? 'N/A' }}/10</span>
                    </div>
                    <div class="rating-item">
                      <span>{{ translateMetric('Method Cohesion') }}:</span>
                      <span class="rating-value">{{ file.rating?.methodCohesion ?? 'N/A' }}/10</span>
                    </div>
                    <div class="rating-item">
                      <span>{{ translateMetric('Dead Code') }}:</span>
                      <span class="rating-value">{{ file.rating?.deadCode ?? 'N/A' }}/10</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="getAnalysisSuggestions(analysis).length > 0" class="suggestions-section">
              <h2>Alterações no Código</h2>
              <div v-for="file in getAnalysisFiles(analysis)" :key="file.fileId" class="file-diff-section">
                <h3>{{ file.path }}</h3>
                <DiffViewer v-if="file.content" :content="file.content" />
              </div>
              <h2>Sugestões de Melhoria</h2>
              <div class="suggestions-container">
                <div class="suggestions-list">
                  <div
                    v-for="(suggestion, index) in getAnalysisSuggestions(analysis)"
                    :key="index"
                    class="suggestion-card"
                    :class="`priority-${suggestion.priority.toLowerCase()}`"
                  >
                    <div class="suggestion-header">
                      <h3>{{ suggestion.title }}</h3>
                    </div>
                    <div class="suggestion-meta">
                      <div class="meta-item">
                        <span class="meta-label">Prioridade:</span>
                        <span class="badge priority" :class="`priority-${suggestion.priority.toLowerCase()}`">
                          {{ translatePriority(suggestion.priority) }}
                        </span>
                      </div>
                      <div class="meta-item">
                        <span class="meta-label">Dificuldade:</span>
                        <span class="badge difficulty" :class="`difficulty-${suggestion.difficult.toLowerCase()}`">
                          {{ translateDifficulty(suggestion.difficult) }}
                        </span>
                      </div>
                      <div class="meta-item">
                        <span class="meta-label">Tipo:</span>
                        <span class="badge type">
                          {{ translateType(suggestion.type) }}
                        </span>
                      </div>
                    </div>
                    <p class="suggestion-description">{{ suggestion.description }}</p>
                    <div class="suggestion-footer">
                      <span class="file-ref">{{ suggestion.fileReference }}</span>
                      <div v-if="suggestion.studyResources && suggestion.studyResources.length > 0" class="resources">
                        <span class="resources-label">Material de Estudo:</span>
                        <div class="resources-list">
                          <span
                            v-for="(resource, idx) in suggestion.studyResources"
                            :key="idx"
                            class="resource-text"
                          >
                            {{ resource }}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="!loading && filteredAnalyses.length === 0" class="no-results">
      Nenhuma análise encontrada.
    </div>
  </div>
  </div>
</template><script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAnalysisStore } from '../stores/analysisStore';
import { useAnalysisService } from '../server/api/analysisService';
import { CommitAnalysis } from '../interfaces/CommitAnalysis';
import { Suggestion } from '../interfaces/Suggestion';
import DiffViewer from '../components/DiffViewer.vue';
import { translateQuality, translateMetric } from '../utils/translations';


const router = useRouter();
const route = useRoute();
const analysisStore = useAnalysisStore();
const analysisService = useAnalysisService();

const analyses = ref<CommitAnalysis[]>([]);
const searchQuery = ref('');
const selectedLanguage = ref('');
const loading = ref(false);
const error = ref<string | null>(null);
const expandedCommits = ref<Set<string>>(new Set());

const getFirstLanguage = (analysis: CommitAnalysis): string => {
  if (!analysis?.filesDetails?.[0]?.language) return 'N/A';
  return analysis.filesDetails[0].language;
};

const getOverallNote = (analysis: CommitAnalysis): number => {
  if (!analysis) return 0;
  return analysis.OverallNote || 0;
};

const getFirstFileRating = (analysis: CommitAnalysis) => {
  if (!analysis) return null;

  if (analysis.filesDetails && analysis.filesDetails.length > 0 && analysis.filesDetails[0]?.rating) {
    return analysis.filesDetails[0].rating;
  }
  if (analysis.files && analysis.files.length > 0 && analysis.files[0]?.rating) {
    return analysis.files[0].rating;
  }
  return null;
};

const getAnalysisFiles = (analysis: CommitAnalysis) => {
  if (!analysis?.filesDetails) return [];
  return analysis.filesDetails.filter(file => file != null);
};

const getAnalysisSuggestions = (analysis: CommitAnalysis) => {
  if (!analysis?.filesDetails) return [];
  return analysis.filesDetails.reduce((allSuggestions: Suggestion[], file) => {
    if (file.suggestions) {
      allSuggestions.push(...file.suggestions);
    }
    return allSuggestions;
  }, []);
};

const getTotalAddedLines = (analysis: CommitAnalysis): number => {
  const files = getAnalysisFiles(analysis);
  return files.reduce((total, file) => total + (file.addedLines || 0), 0);
};

const getTotalRemovedLines = (analysis: CommitAnalysis): number => {
  const files = getAnalysisFiles(analysis);
  return files.reduce((total, file) => total + (file.removedLines || 0), 0);
};

const languages = computed<string[]>(() => {
  const langs = new Set<string>();
  analyses.value.forEach(analysis => {
    if (analysis?.filesDetails) {
      analysis.filesDetails.forEach(file => {
        if (typeof file?.language === 'string') {
          langs.add(file.language);
        }
      });
    }
  });
  return Array.from(langs).sort();
});

const filteredAnalyses = computed<CommitAnalysis[]>(() => {
  let filtered = analyses.value;

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    filtered = filtered.filter(
      (a) =>
        (a?.author?.toLowerCase() || '').includes(query) ||
        (a?.commitId?.toLowerCase() || '').includes(query) ||
        (a?.email?.toLowerCase() || '').includes(query)
    );
  }

  if (selectedLanguage.value) {
    filtered = filtered.filter((a) => {
      return a?.filesDetails?.some(file => file?.language === selectedLanguage.value) || false;
    });
  }

  return filtered.sort(
    (a, b) => {
      const dateA = a?.analysisDate ? new Date(a.analysisDate).getTime() : 0;
      const dateB = b?.analysisDate ? new Date(b.analysisDate).getTime() : 0;
      return dateB - dateA;
    }
  );
});

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

// Funções de tradução para badges
const translatePriority = (priority: string): string => {
  const translations: Record<string, string> = {
    'Low': 'Baixa',
    'Medium': 'Média',
    'High': 'Alta',
  };
  return translations[priority] || priority;
};

const translateDifficulty = (difficulty: string): string => {
  const translations: Record<string, string> = {
    'Easy': 'Fácil',
    'Medium': 'Média',
    'Hard': 'Difícil',
  };
  return translations[difficulty] || difficulty;
};

const translateType = (type: string): string => {
  const translations: Record<string, string> = {
    'DeadCode': 'Código Morto',
    'Documentation': 'Documentação',
    'Naming': 'Nomenclatura',
    'Structure': 'Estrutura',
    'Cohesion': 'Coesão',
    'Refactoring': 'Refatoração',
    'CodeStyle': 'Estilo de Código',
    'Performance': 'Performance',
    'Security': 'Segurança',
  };
  return translations[type] || type;
};

const toggleCommit = (commitId: string) => {
  if (expandedCommits.value.has(commitId)) {
    expandedCommits.value.delete(commitId);
  } else {
    expandedCommits.value.add(commitId);
  }
};

const isCommitExpanded = (commitId: string): boolean => {
  return expandedCommits.value.has(commitId);
};

const fetchAnalyses = async () => {
  try {
    loading.value = true;
    error.value = null;
    
    console.log('Fazendo requisição para a API...');
    const apiUrl = '/api/v1/analysis';
    console.log('URL da API:', apiUrl);

    const response = await fetch(apiUrl, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json'
      },
      credentials: 'same-origin'
    });
    
    console.log('Status da resposta:', response.status);
    console.log('Headers:', Object.fromEntries(response.headers.entries()));
    
    if (!response.ok) {
      const errorText = await response.text();
      console.error('Erro da API:', {
        status: response.status,
        statusText: response.statusText,
        body: errorText,
        url: response.url
      });
      throw new Error(`Erro na resposta da API: ${response.status} - ${errorText}`);
    }
    
    const data = await response.json();
    console.log('Dados recebidos:', {
      data,
      type: typeof data,
      isArray: Array.isArray(data),
      hasData: data?.data !== undefined,
      keys: Object.keys(data || {})
    });
    
    return data;
  } catch (err) {
    const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
    error.value = `Erro ao carregar análises: ${errorMessage}`;
    console.error('Error loading analyses:', {
      error: err,
      message: errorMessage,
      stack: err instanceof Error ? err.stack : undefined,
      type: err?.constructor?.name
    });
    return null;
  }
};

const loadAnalyses = async () => {
  console.log('Iniciando carregamento...');
  try {
    const data = await fetchAnalyses();
    console.log('Resposta completa:', data);

    if (!data) {
      console.warn('Nenhum dado recebido da API');
      analyses.value = [];
      return;
    }

    // Tenta encontrar os dados da análise em diferentes locais da resposta
    let analysesData;
    
    if (data.success && Array.isArray(data.analysis)) {
      console.log('Dados recebidos no formato correto');
      analysesData = data.analysis;
    } else if (Array.isArray(data)) {
      console.log('Dados recebidos como array');
      analysesData = data;
    } else if (data.data && Array.isArray(data.data)) {
      console.log('Dados recebidos dentro de data.data como array');
      analysesData = data.data;
    } else if (typeof data === 'object') {
      if (data.id || data.commitId) {
        console.log('Dados recebidos como objeto único de análise');
        analysesData = [data];
      } else if (data.data && typeof data.data === 'object') {
        console.log('Dados recebidos como objeto único dentro de data');
        analysesData = [data.data];
      }
    }

    // Log para debug dos dados recebidos
    console.log('Dados brutos recebidos:', JSON.stringify(analysesData, null, 2));

    if (!analysesData) {
      console.error('Formato de dados não reconhecido:', data);
      analyses.value = [];
      error.value = 'Formato de dados inválido recebido da API';
      return;
    }

    // Filtra e valida cada item
    analyses.value = analysesData.filter((item: any) => {
      if (!item || typeof item !== 'object') {
        console.warn('Item inválido encontrado:', item);
        return false;
      }
      return true;
    }).map((item: any) => {
      // Garante que campos essenciais existam
      return {
        ...item,
        id: item.id || crypto.randomUUID(),
        commitId: item.commitId,
        author: item.author || 'Autor desconhecido',
        analysisDate: item.analysisDate || new Date().toISOString(),
        OverallNote: item.OverallNote || 0,
        filesDetails: Array.isArray(item.filesDetails) ? item.filesDetails.map((file: any) => ({
          ...file,
          fileId: file.fileId || crypto.randomUUID(),
          path: file.path || '',
          language: file.language || 'N/A',
          addedLines: file.addedLines || 0,
          removedLines: file.removedLines || 0,
          content: file.content || '',
          rating: file.rating || null,
          suggestions: Array.isArray(file.suggestions) ? file.suggestions : []
        })) : []
      };
    });

    console.log('Análises processadas:', {
      total: analyses.value.length,
      items: analyses.value
    });

    analysisStore.setAnalyses(analyses.value);
  } catch (err) {
    console.error('Erro ao processar dados:', err);
    analyses.value = [];
    error.value = 'Erro ao processar dados da análise';
  } finally {
    loading.value = false;
  }
};

// Função para buscar por CommitId específico
const searchByCommitId = async () => {
  const commitId = searchQuery.value.trim();
  
  if (!commitId) {
    // Se não houver busca, carrega todas as análises
    loadAnalyses();
    return;
  }
  
  loading.value = true;
  error.value = null;
  
  try {
    // Tenta buscar pelo endpoint específico de CommitId
    const { result, error: apiError } = await analysisService.getAnalysisById(commitId);
    
    if (apiError.value) {
      console.error('Erro ao buscar por CommitId:', apiError.value);
      // Se falhar, tenta buscar na lista geral
      loadAnalyses();
    } else if (result.value?.success && result.value.data) {
      // Sucesso: encontrou a análise específica (já no formato correto)
      const data = result.value.data;
      
      analyses.value = [data];
      console.log('Análise encontrada:', data);
      
      // Expande automaticamente o commit encontrado
      const foundCommitId = data.commitId || data.id;
      if (foundCommitId) {
        expandedCommits.value.add(foundCommitId);
      }
    } else {
      // Se não encontrou, busca na lista geral (filtro por texto)
      loadAnalyses();
    }
  } catch (err) {
    console.error('Erro na busca:', err);
    // Em caso de erro, tenta buscar na lista geral
    loadAnalyses();
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  // Verificar se há commitId na query string
  const commitId = route.query.commitId as string;
  if (commitId) {
    searchQuery.value = commitId;
    console.log('Buscando commit:', commitId);
    // Executa busca específica pelo CommitId
    searchByCommitId();
  } else {
    // Carrega lista geral
    loadAnalyses();
  }
});
</script>

<style scoped lang="scss">
.analysis-list-page {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.commit-container {
  margin-bottom: 1rem;
}

.analysis-container {
  background: var(--card-background);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  overflow: hidden;
  transition: all 0.3s ease;

  .commit-header {
    padding: 1rem 1.5rem;
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--card-background);
    border-bottom: 1px solid var(--border-color);
    transition: all 0.3s ease;

    &:hover {
      background: var(--hover-color, rgba(0, 0, 0, 0.05));
    }

    .commit-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;

      .expand-icon {
        font-size: 0.8rem;
        color: var(--text-secondary);
        transition: transform 0.3s ease;
      }

      h2 {
        margin: 0;
        font-size: 1.1rem;
      }
    }
    
    .commit-meta {
      display: flex;
      align-items: center;
      gap: 1rem;
      font-size: 0.9rem;
      
      .author {
        color: var(--text-secondary);
      }
      
      .date {
        color: var(--text-secondary);
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

  .commit-details {
    padding: 1.5rem;
    border-top: 1px solid var(--border-color);
    animation: slideDown 0.3s ease;
  }

  &.expanded {
    box-shadow: 0 2px 8px var(--shadow-color);
    
    .commit-header {
      background: var(--light-gray, #f5f5f5);
    }
  }
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.suggestions-container {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  background: var(--card-background);
  padding: 1rem;
  margin-top: 1rem;
  
  &::-webkit-scrollbar {
    width: 8px;
  }
  
  &::-webkit-scrollbar-track {
    background: var(--card-background);
    border-radius: 4px;
  }
  
  &::-webkit-scrollbar-thumb {
    background: var(--border-color);
    border-radius: 4px;
    
    &:hover {
      background: var(--primary-color);
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
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 1.5rem;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }

  .suggestion-header {
    margin-bottom: 1rem;

    h3 {
      font-size: 1.1rem;
      font-weight: 600;
      margin-bottom: 0.75rem;
      color: var(--text-primary);
    }
  }

  .suggestion-description {
    line-height: 1.6;
    margin-bottom: 1.25rem;
    color: var(--text-secondary);
  }

  .resources {
    background: var(--light-gray);
    padding: 1rem;
    border-radius: 6px;
    margin-top: 1rem;

    .resources-label {
      font-weight: 600;
      color: var(--text-primary);
      display: block;
      margin-bottom: 0.5rem;
    }

    .resources-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;

      .resource-text {
        color: var(--text-secondary);
        font-size: 0.95rem;
        padding: 0.25rem 0;
      }
    }
  }
}

.page-header {
  margin-bottom: 2rem;

  h1 {
    font-size: 2rem;
    color: var(--text-primary);
    margin-bottom: 1rem;
    transition: color 0.3s ease;
  }

  .filters {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .search-container {
    display: flex;
    gap: 0.5rem;
    flex: 1;
    min-width: 300px;
  }

  .search-input,
  .language-filter {
    padding: 0.75rem 1rem;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    font-size: 1rem;
    outline: none;
    background: var(--card-background);
    color: var(--text-primary);
    transition: all 0.3s ease;

    &:focus {
      border-color: var(--primary-color);
    }
  }

  .search-input {
    flex: 1;
    min-width: 200px;
  }

  .search-button {
    padding: 0.75rem 1.5rem;
    background: var(--card-background);
    color: white;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    font-size: 1.2rem;
    cursor: pointer;
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;

    &:hover {
      background: var(--primary-hover);
      transform: translateY(-2px);
    }

    &:active {
      transform: translateY(0);
    }
  }

  .language-filter {
    min-width: 200px;
  }
}

.loading,
.error,
.no-results {
  text-align: center;
  padding: 3rem;
  font-size: 1.1rem;
  color: var(--text-secondary);
  transition: color 0.3s ease;
}

.error {
  color: var(--danger-color);
}

.analyses-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1.5rem;
}

.analysis-card {
  background: var(--card-background);
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  cursor: pointer;
  transition: all 0.3s ease;
  overflow: hidden;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 16px var(--shadow-color);
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background: var(--light-gray);
    border-bottom: 1px solid var(--border-color);

    h3 {
      font-size: 1.1rem;
      color: var(--text-primary);
      font-family: monospace;
      transition: color 0.3s ease;
    }

    .language-badge {
      padding: 0.25rem 0.75rem;
      background: var(--primary-color);
      color: white;
      border-radius: 12px;
      font-size: 0.85rem;
      font-weight: 500;
    }
  }

  .card-body {
    padding: 1.5rem;

    .author {
      font-size: 1rem;
      color: var(--text-primary);
      margin-bottom: 0.25rem;
      transition: color 0.3s ease;
    }

    .email {
      font-size: 0.85rem;
      color: var(--text-secondary);
      margin-bottom: 0.5rem;
      transition: color 0.3s ease;
    }

    .date {
      font-size: 0.85rem;
      color: var(--medium-gray);
      margin-bottom: 1rem;
      transition: color 0.3s ease;
    }

    .stats {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;

      .stat {
        color: var(--text-secondary);
        transition: color 0.3s ease;

        .stat-label {
          font-weight: 500;
        }
      }
    }
  }

  .card-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background: var(--light-gray);
    border-top: 1px solid var(--border-color);

    .score {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: bold;
      padding: 0.5rem 1rem;
      border-radius: 6px;

      .score-label {
        font-size: 0.85rem;
      }

      .score-value {
        font-size: 1.25rem;
      }
    }

    .suggestions-count {
      font-size: 0.85rem;
      color: var(--text-secondary);
      transition: color 0.3s ease;
    }
  }
}

.score-excellent {
  color: #27ae60;
  background: rgba(39, 174, 96, 0.1);
}

.score-very-good {
  color: #2ecc71;
  background: rgba(46, 204, 113, 0.1);
}

.score-good {
  color: #f39c12;
  background: rgba(243, 156, 18, 0.1);
}

.score-acceptable {
  color: #e67e22;
  background: rgba(230, 126, 34, 0.1);
}

.score-needs-improvement {
  color: #e74c3c;
  background: rgba(231, 76, 60, 0.1);
}

.score-section {
  margin-bottom: 2rem;
  display: flex;
  gap: 2rem;
  align-items: flex-start;

  .overall-score {
    padding: 1.5rem;
    border-radius: 8px;
    text-align: center;
    min-width: 200px;

    h2 {
      margin: 0 0 1rem;
      font-size: 1.2rem;
    }

    .score-value {
      font-size: 2.5rem;
      font-weight: bold;
      margin-bottom: 0.5rem;
    }

    .score-quality {
      font-size: 1rem;
      opacity: 0.8;
    }
  }

  .metrics {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 1rem;
    flex: 1;

    .metric {
      background: var(--card-background);
      padding: 1rem;
      border-radius: 6px;
      border: 1px solid var(--border-color);

      .metric-label {
        display: block;
        font-size: 0.9rem;
        color: var(--text-secondary);
        margin-bottom: 0.5rem;
      }

      .metric-value {
        font-size: 1.25rem;
        font-weight: 500;

        &.added {
          color: #27ae60;
        }

        &.removed {
          color: #e74c3c;
        }
      }
    }
  }
}

.files-section {
  margin-bottom: 2rem;

  h2 {
    margin-bottom: 1rem;
    font-size: 1.2rem;
  }

  .files-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(800px, 1fr));
    gap: 1.5rem;
    margin: 1.5rem 0;
  }

  .file-card {
    background: var(--card-background);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 1.5rem;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    transition: transform 0.2s ease, box-shadow 0.2s ease;

    .file-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;

      h3 {
        font-size: 0.9rem;
        margin: 0;
        color: var(--text-primary);
        font-family: monospace;
      }

      .file-language {
        font-size: 0.8rem;
        color: var(--text-secondary);
        padding: 0.25rem 0.5rem;
        background: var(--light-gray);
        border-radius: 4px;
      }
    }

    .file-stats {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.5rem;

      .stat {
        font-size: 0.9rem;

        &.added {
          color: #27ae60;
        }

        &.removed {
          color: #e74c3c;
        }
      }
    }

    .file-rating {
      border-top: 1px solid var(--border-color);
      padding-top: 1rem;
      margin-top: 1rem;

      .rating-item {
        display: flex;
        justify-content: space-between;
        font-size: 0.9rem;
        margin-bottom: 0.5rem;
        padding: 0.5rem;
        border-radius: 4px;
        background: var(--light-gray);
        transition: background-color 0.2s ease;

        &:hover {
          background: var(--hover-color);
        }

        span:first-child {
          color: var(--text-secondary);
          font-weight: 500;
        }

        .rating-value {
          font-weight: 600;
          color: var(--primary-color);
        }
      }
    }

    &:hover {
      transform: translateY(-4px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }
  }
}

.suggestions-section {
  h2 {
    margin-bottom: 1rem;
    font-size: 1.2rem;
  }
}

.file-diff-section {
  margin: 1.5rem 0;
  
  h3 {
    font-size: 1rem;
    color: var(--text-primary);
    font-family: monospace;
    margin-bottom: 0.5rem;
  }
}

.suggestions-container {
  margin-top: 1rem;
}

.suggestions-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.suggestion-card {
  background: var(--card-background);
  border-radius: 8px;
  padding: 1.5rem;
  border-left: 4px solid var(--border-color);
  transition: all 0.3s ease;

  &:hover {
    transform: translateX(4px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
}

.suggestion-header {
  margin-bottom: 1rem;

  h3 {
    margin: 0;
    font-size: 1.1rem;
    color: var(--text-primary);
  }
}

.suggestion-meta {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.meta-label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.badge {
  padding: 0.35rem 0.85rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
}

// Priority badges - usando cores consistentes com score
.badge.priority-low {
  color: #27ae60;
  background: rgba(39, 174, 96, 0.15);
}

.badge.priority-medium {
  color: #f39c12;
  background: rgba(243, 156, 18, 0.15);
}

.badge.priority-high {
  color: #e74c3c;
  background: rgba(231, 76, 60, 0.15);
}

// Difficulty badges
.badge.difficulty-easy {
  color: #27ae60;
  background: rgba(39, 174, 96, 0.15);
}

.badge.difficulty-medium {
  color: #f39c12;
  background: rgba(243, 156, 18, 0.15);
}

.badge.difficulty-hard {
  color: #e74c3c;
  background: rgba(231, 76, 60, 0.15);
}

.badge.type {
  color: var(--text-primary);
  background: var(--border-color);
}

.suggestion-description {
  color: var(--text-secondary);
  line-height: 1.6;
  margin-bottom: 1rem;
}

.suggestion-footer {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.file-ref {
  font-family: monospace;
  font-size: 0.85rem;
  color: var(--text-secondary);
  background: var(--background);
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  display: inline-block;
}
</style>
