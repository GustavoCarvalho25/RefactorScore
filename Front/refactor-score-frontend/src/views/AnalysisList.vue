<template>
  <div class="analysis-list-page">
    <header class="page-header">
      <h1>Análises de Commits</h1>
      <div class="filters">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Buscar por autor ou commit..."
          class="search-input"
        />
        <select v-model="selectedLanguage" class="language-filter">
          <option value="">Todas as linguagens</option>
          <option v-for="lang in languages" :key="lang" :value="lang">
            {{ lang }}
          </option>
        </select>
      </div>
    </header>

    <div v-if="loading" class="loading">Carregando análises...</div>

    <div v-else-if="error" class="error">{{ error }}</div>

    <div v-else class="analyses-grid">
      <div
        v-for="analysis in filteredAnalyses"
        :key="analysis.id"
        class="analysis-card"
        @click="goToAnalysis(analysis.id)"
      >
        <div class="card-header">
          <h3>{{ analysis.commitId.substring(0, 8) }}</h3>
          <span class="language-badge">{{ analysis.language }}</span>
        </div>
        <div class="card-body">
          <p class="author">
            <strong>{{ analysis.author }}</strong>
          </p>
          <p class="email">{{ analysis.email }}</p>
          <p class="date">{{ formatDate(analysis.commitDate) }}</p>
          <div class="stats">
            <span class="stat">
              <span class="stat-label">Arquivos:</span>
              {{ analysis.files.length }}
            </span>
            <span class="stat">
              <span class="stat-label">+{{ analysis.addedLines }}</span>
              <span class="stat-label">-{{ analysis.removedLines }}</span>
            </span>
          </div>
        </div>
        <div class="card-footer">
          <div class="score" :class="getScoreClass(analysis.overallNote)">
            <span class="score-label">Nota:</span>
            <span class="score-value">{{ analysis.overallNote.toFixed(2) }}</span>
          </div>
          <div class="suggestions-count">
            {{ analysis.suggestions.length }} sugestões
          </div>
        </div>
      </div>
    </div>

    <div v-if="!loading && filteredAnalyses.length === 0" class="no-results">
      Nenhuma análise encontrada.
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAnalysisStore } from '../stores/analysisStore';
import { useAnalysisService } from '../server/api/analysisService';
import { CommitAnalysis } from '../interfaces/CommitAnalysis';

const router = useRouter();
const analysisStore = useAnalysisStore();
const analysisService = useAnalysisService();

const analyses = ref<CommitAnalysis[]>([]);
const searchQuery = ref('');
const selectedLanguage = ref('');
const loading = ref(false);
const error = ref<string | null>(null);

const languages = computed(() => {
  const langs = new Set(analyses.value.map((a) => a.language));
  return Array.from(langs).sort();
});

const filteredAnalyses = computed(() => {
  let filtered = analyses.value;

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    filtered = filtered.filter(
      (a) =>
        a.author.toLowerCase().includes(query) ||
        a.commitId.toLowerCase().includes(query) ||
        a.email.toLowerCase().includes(query)
    );
  }

  if (selectedLanguage.value) {
    filtered = filtered.filter((a) => a.language === selectedLanguage.value);
  }

  return filtered.sort(
    (a, b) =>
      new Date(b.commitDate).getTime() - new Date(a.commitDate).getTime()
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

const goToAnalysis = (id: string) => {
  router.push({ name: 'AnalysisDetail', params: { id } });
};

const loadAnalyses = async () => {
  loading.value = true;
  error.value = null;
  try {
    const { result, error: apiError } = await analysisService.getAnalyses();
    if (apiError.value) {
      error.value = 'Erro ao carregar análises';
      console.error('Error loading analyses:', apiError.value);
    } else if (result.value) {
      analyses.value = result.value.data || [];
      analysisStore.setAnalyses(analyses.value);
    }
  } catch (err) {
    error.value = 'Erro ao carregar análises';
    console.error('Error loading analyses:', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadAnalyses();
});
</script>

<style scoped lang="scss">
.analysis-list-page {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 2rem;

  h1 {
    font-size: 2rem;
    color: #2c3e50;
    margin-bottom: 1rem;
  }

  .filters {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .search-input,
  .language-filter {
    padding: 0.75rem 1rem;
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    font-size: 1rem;
    outline: none;
    transition: border-color 0.3s ease;

    &:focus {
      border-color: #447bda;
    }
  }

  .search-input {
    flex: 1;
    min-width: 300px;
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
  color: #7f8c8d;
}

.error {
  color: #e74c3c;
}

.analyses-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1.5rem;
}

.analysis-card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: all 0.3s ease;
  overflow: hidden;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background: #f8f9fa;
    border-bottom: 1px solid #e0e0e0;

    h3 {
      font-size: 1.1rem;
      color: #2c3e50;
      font-family: monospace;
    }

    .language-badge {
      padding: 0.25rem 0.75rem;
      background: #447bda;
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
      color: #2c3e50;
      margin-bottom: 0.25rem;
    }

    .email {
      font-size: 0.85rem;
      color: #7f8c8d;
      margin-bottom: 0.5rem;
    }

    .date {
      font-size: 0.85rem;
      color: #95a5a6;
      margin-bottom: 1rem;
    }

    .stats {
      display: flex;
      gap: 1rem;
      font-size: 0.9rem;

      .stat {
        color: #7f8c8d;

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
    background: #f8f9fa;
    border-top: 1px solid #e0e0e0;

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
      color: #7f8c8d;
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
</style>
