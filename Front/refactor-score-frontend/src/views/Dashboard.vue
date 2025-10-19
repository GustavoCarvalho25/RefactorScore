<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div class="header-content">
        <div>
          <h1>RefactorScore Dashboard</h1>
          <p>Análise de Commits com Clean Code</p>
        </div>
        <button class="theme-toggle" @click="toggleTheme" :title="isDark ? 'Modo Claro' : 'Modo Escuro'">
          <span v-if="isDark">☀️</span>
          <span v-else>🌙</span>
        </button>
      </div>
    </header>

    <div class="dashboard-stats">
      <div class="stat-card">
        <h3>Total de Análises</h3>
        <p class="stat-value">{{ commitCount }}</p>
      </div>
      <div class="stat-card">
        <h3>Nota Média</h3>
        <p class="stat-value">{{ averageNote }}</p>
      </div>
      <div class="stat-card">
        <h3>Arquivos Analisados</h3>
        <p class="stat-value">{{ uniqueFilesCount }}</p>
      </div>
      <div class="stat-card">
        <h3>Sugestões Geradas</h3>
        <p class="stat-value">{{ totalSuggestions }}</p>
      </div>
    </div>

    <div class="dashboard-charts">
      <div class="chart-card">
        <div class="chart-wrapper">
          <LineChart
            v-if="lineChartData.labels.length > 0"
            chart-id="quality-evolution"
            :labels="lineChartData.labels"
            :datasets="lineChartData.datasets"
            :y-axis-step-size="1"
            :y-axis-max="10"
            title="Evolução das Notas dos Commits ao Longo do Tempo"
          />
          <div v-else class="no-data">
            <p>Sem dados para exibir</p>
          </div>
        </div>
      </div>

      <div class="chart-card">
        <div class="chart-wrapper">
          <PieChart
            v-if="languageChartData.labels.length > 0"
            chart-id="language-distribution"
            :labels="languageChartData.labels"
            :datasets="languageChartData.datasets"
            title="Distribuição de Arquivos por Linguagem"
          />
          <div v-else class="no-data">
            <p>Sem dados para exibir</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAnalysisStore } from '../stores/analysisStore';
import { useAnalysisService } from '../server/api/analysisService';
import { useTheme } from '../composables/useTheme';
import LineChart from '../components/charts/LineChart.vue';
import BarChart from '../components/charts/BarChart.vue';
import PieChart from '../components/charts/PieChart.vue';

const router = useRouter();
const analysisStore = useAnalysisStore();
const analysisService = useAnalysisService();
const { isDark, toggleTheme, initTheme } = useTheme();

const analyses = ref<any[]>([]);
const commitCount = ref<number>(0);
const averageNote = ref<number>(0);
const uniqueFilesCount = ref<number>(0);
const totalSuggestions = ref<number>(0);
const bestNote = ref<number>(0);
const worstNote = ref<number>(0);
const languageFrequency = ref<{ language: string; count: number }[]>([]);
const commitsEvolution = ref<any[]>([]);

const lineChartData = computed(() => {
  if (!commitsEvolution.value || commitsEvolution.value.length === 0) {
    return { labels: [], datasets: [] };
  }
  
  // Ordenar commits por data
  const sortedCommits = [...commitsEvolution.value].sort((a, b) =>
    new Date(a.commitDate || 0).getTime() - new Date(b.commitDate || 0).getTime()
  );

  return {
    labels: sortedCommits.map((commit) => formatDate(commit.commitDate)),
    datasets: [
      {
        label: 'Nota do Commit',
        data: sortedCommits.map((commit) => commit.note || 0),
        borderColor: 'rgba(68, 123, 218, 1)',
        backgroundColor: 'rgba(68, 123, 218, 0.2)',
      },
    ],
  };
});

const languageChartData = computed(() => {
  if (!languageFrequency.value || languageFrequency.value.length === 0) {
    return { labels: [], datasets: [] };
  }

  return {
    labels: languageFrequency.value.map((item) => item.language),
    datasets: [
      {
        label: 'Quantidade de Arquivos',
        data: languageFrequency.value.map((item) => item.count),
        backgroundColor: [
          'rgba(68, 123, 218, 0.8)',
          'rgba(75, 192, 192, 0.8)',
          'rgba(255, 206, 86, 0.8)',
          'rgba(153, 102, 255, 0.8)',
          'rgba(255, 159, 64, 0.8)',
          'rgba(255, 99, 132, 0.8)',
          'rgba(54, 162, 235, 0.8)',
          'rgba(255, 159, 64, 0.8)',
        ],
        borderColor: [
          'rgba(68, 123, 218, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(255, 206, 86, 1)',
          'rgba(153, 102, 255, 1)',
          'rgba(255, 159, 64, 1)',
          'rgba(255, 99, 132, 1)',
          'rgba(54, 162, 235, 1)',
          'rgba(255, 159, 64, 1)',
        ],
      },
    ],
  };
});

const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('pt-BR');
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
  analysisStore.setLoading(true);
  try {
    const { result, error } = await analysisService.getAnalyses();
    if (error.value) {
      console.error('Error loading analyses:', error.value);
      analysisStore.setError('Erro ao carregar análises');
    } else if (result.value) {
      analyses.value = result.value.data || [];
      analysisStore.setAnalyses(analyses.value);
    }
  } catch (err) {
    console.error('Error loading analyses:', err);
    analysisStore.setError('Erro ao carregar análises');
  } finally {
    analysisStore.setLoading(false);
  }
};

const loadStatistics = async () => {
  try {
    const { result, error } = await analysisService.getAnalysisStatistics();
    
    if (error.value) {
      console.error('Erro ao carregar estatísticas:', error.value);
    } else if (result.value) {
      commitCount.value = result.value.total || 0;
      averageNote.value = result.value.averageNote || 0;
      uniqueFilesCount.value = result.value.uniqueFilesCount || 0;
      totalSuggestions.value = result.value.totalSuggestions || 0;
      languageFrequency.value = result.value.languageFrequency || [];
      commitsEvolution.value = result.value.commitsEvolution || [];
    }
  } catch (err) {
    console.error('Erro ao carregar estatísticas:', err);
  }
};

onMounted(() => {
  initTheme();
  loadAnalyses();
  loadStatistics();
});
</script>

<style scoped lang="scss">
.dashboard {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
  min-height: 100vh;
}

.dashboard-header {
  margin-bottom: 2rem;

  .header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  h1 {
    font-size: 2.5rem;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
    transition: color 0.3s ease;
  }

  p {
    font-size: 1.1rem;
    color: var(--text-secondary);
    transition: color 0.3s ease;
  }
}

.theme-toggle {
  background: var(--card-background);
  border: 2px solid var(--border-color);
  border-radius: 50%;
  width: 50px;
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1.5rem;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px var(--shadow-color);

  &:hover {
    transform: scale(1.1);
    border-color: var(--primary-color);
  }

  &:active {
    transform: scale(0.95);
  }
}

.dashboard-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  transition: all 0.3s ease;

  h3 {
    font-size: 0.9rem;
    color: var(--text-secondary);
    margin-bottom: 0.5rem;
    text-transform: uppercase;
    transition: color 0.3s ease;
  }

  .stat-value {
    font-size: 2.5rem;
    font-weight: bold;
    color: var(--primary-color);
    transition: color 0.3s ease;
  }

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
}

.dashboard-charts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.chart-card {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  height: 450px;
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;

  h3 {
    margin-bottom: 1rem;
    color: var(--text-primary);
    flex-shrink: 0;
    transition: color 0.3s ease;
  }

  .chart-wrapper {
    flex: 1;
    min-height: 0;
    position: relative;
    width: 100%;
    height: 100%;
  }

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
}

.no-data {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  color: var(--text-secondary);
  font-size: 1.1rem;
}

.recent-analyses {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);

  h3 {
    margin-bottom: 1rem;
    color: #2c3e50;
  }
}

.analysis-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.analysis-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s ease;

  &:hover {
    background: #f8f9fa;
    border-color: #447bda;
  }

  .analysis-info {
    h4 {
      font-size: 1rem;
      color: #2c3e50;
      margin-bottom: 0.25rem;
    }

    p {
      font-size: 0.85rem;
      color: #7f8c8d;
    }
  }

  .analysis-score {
    span {
      font-size: 1.5rem;
      font-weight: bold;
      padding: 0.5rem 1rem;
      border-radius: 6px;
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
