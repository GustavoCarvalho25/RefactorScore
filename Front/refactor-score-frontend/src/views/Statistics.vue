<template>
  <div class="statistics-page">
    <header class="page-header">
      <h1>Estatísticas Gerais</h1>
      <p>Visão geral das análises de commits</p>
    </header>

    <div v-if="loading" class="loading">Carregando estatísticas...</div>

    <div v-else-if="error" class="error">{{ error }}</div>

    <div v-else class="statistics-content">
      <div class="stats-overview">
        <div class="stat-card">
          <h3>Melhor Nota</h3>
          <p class="stat-value">{{bestNote}}</p>
        </div>
        <div class="stat-card">
          <h3>Pior Nota</h3>
          <p class="stat-value">{{ worstNote }}</p>
        </div>
      </div>

      <div class="charts-grid">
        <div class="chart-card">
          <h3>Distribuição de Qualidade</h3>
          <BarChart
            v-if="qualityDistribution.labels.length > 0"
            chart-id="quality-distribution"
            :labels="qualityDistribution.labels"
            :datasets="qualityDistribution.datasets"
            title="Commits por Qualidade"
          />
        </div>

        <div class="chart-card">
          <h3>Métricas Médias</h3>
          <RadarChart
            v-if="averageMetrics"
            chart-id="average-metrics"
            :rating="averageMetrics"
            title="Média das Métricas Clean Code"
          />
        </div>

        <div class="chart-card">
          <h3>Top Autores</h3>
          <BarChart
            v-if="topAuthors.labels.length > 0"
            chart-id="top-authors"
            :labels="topAuthors.labels"
            :datasets="topAuthors.datasets"
            title="Autores com Mais Commits"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useAnalysisService } from '../server/api/analysisService';
import type { CommitAnalysis } from '../interfaces/CommitAnalysis';
import type { CleanCodeRating } from '../interfaces/CleanCodeRating';
import BarChart from '../components/charts/BarChart.vue';
import LineChart from '../components/charts/LineChart.vue';
import RadarChart from '../components/charts/RadarChart.vue';

const analysisService = useAnalysisService();

const commitsEvolution = ref<any[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const bestNote = ref<number>(0);
const worstNote = ref<number>(0);

const statistics = computed(() => {
  if (commitsEvolution.value.length === 0) {
    return {
      totalCommits: 0,
      averageScore: 0,
      bestScore: 0,
      worstScore: 0,
    };
  }

  const scores = commitsEvolution.value.map((a) => a.note);
  return {
    totalCommits: commitsEvolution.value.length,
    averageScore: scores.reduce((a, b) => a + b, 0) / scores.length,
    bestScore: Math.max(...scores),
    worstScore: Math.min(...scores),
  };
});

const qualityDistribution = computed(() => {
  const distribution = {
    'Excellent': 0,
    'Very Good': 0,
    'Good': 0,
    'Acceptable': 0,
    'Needs Improvement': 0,
    'Problematic': 0,
  };

  commitsEvolution.value.forEach((a) => {
    const score = a.note;
    if (score >= 9) distribution['Excellent']++;
    else if (score >= 7.5) distribution['Very Good']++;
    else if (score >= 6) distribution['Good']++;
    else if (score >= 5) distribution['Acceptable']++;
    else if (score >= 3.5) distribution['Needs Improvement']++;
    else distribution['Problematic']++;
  });

  return {
    labels: Object.keys(distribution),
    datasets: [
      {
        label: 'Quantidade',
        data: Object.values(distribution),
        backgroundColor: [
          'rgba(39, 174, 96, 0.6)',
          'rgba(46, 204, 113, 0.6)',
          'rgba(243, 156, 18, 0.6)',
          'rgba(230, 126, 34, 0.6)',
          'rgba(231, 76, 60, 0.6)',
          'rgba(192, 57, 43, 0.6)',
        ],
      },
    ],
  };
});

const timelineData = computed(() => {
  const sortedAnalyses = [...commitsEvolution.value].sort(
    (a, b) =>
      new Date(a.commitDate).getTime() - new Date(b.commitDate).getTime()
  );

  return {
    labels: sortedAnalyses.map((a) =>
      new Date(a.commitDate).toLocaleDateString('pt-BR')
    ),
    datasets: [
      {
        label: 'Nota',
        data: sortedAnalyses.map((a) => a.note),
        borderColor: 'rgba(68, 123, 218, 1)',
        backgroundColor: 'rgba(68, 123, 218, 0.1)',
      },
    ],
  };
});

const averageMetrics = computed((): CleanCodeRating | null => {
  if (commitsEvolution.value.length === 0) return null;

  // Criar métricas médias baseadas nas notas dos commits
  const avgNote = commitsEvolution.value.reduce((sum, c) => sum + c.note, 0) / commitsEvolution.value.length;
  
  // Simular métricas baseadas na nota média (valores proporcionais)
  const baseValue = avgNote;

  return {
    variableNaming: baseValue,
    functionSizes: baseValue,
    noNeedsComments: baseValue,
    methodCohesion: baseValue,
    deadCode: baseValue,
    note: avgNote,
    quality: avgNote >= 7 ? 'Good' : 'Acceptable',
    justifies: {},
  };
});

const topAuthors = computed(() => {
  if (commitsEvolution.value.length === 0) {
    return { labels: [], datasets: [] };
  }

  const authorCount: Record<string, number> = {};
  commitsEvolution.value.forEach((commit) => {
    const author = commit.author || 'Desconhecido';
    authorCount[author] = (authorCount[author] || 0) + 1;
  });

  const sorted = Object.entries(authorCount)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10);

  return {
    labels: sorted.map(([author]) => author),
    datasets: [
      {
        label: 'Commits',
        data: sorted.map(([, count]) => count),
        backgroundColor: 'rgba(68, 123, 218, 0.6)',
        borderColor: 'rgba(68, 123, 218, 1)',
      },
    ],
  };
});

const loadStatistics = async () => {
  loading.value = true;
  error.value = null;

  try {
    const { result, error: apiError } = await analysisService.getAnalysisStatistics();
    if (apiError.value) {
      error.value = 'Erro ao carregar estatísticas';
      console.error('Error loading statistics:', apiError.value);
    } else if (result.value) {
      commitsEvolution.value = result.value.commitsEvolution || [];
      bestNote.value = result.value.bestNote || 0;
      worstNote.value = result.value.worstNote || 0;
      
      console.log('Dados carregados:', {
        total: result.value.total,
        commitsEvolution: commitsEvolution.value.length,
        autores: [...new Set(commitsEvolution.value.map(c => c.author))]
      });
    }
  } catch (err) {
    error.value = 'Erro ao carregar estatísticas';
    console.error('Error loading statistics:', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadStatistics();
});
</script>

<style scoped lang="scss">
.statistics-page {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 2rem;

  h1 {
    font-size: 2rem;
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

.loading,
.error {
  text-align: center;
  padding: 3rem;
  font-size: 1.1rem;
}

.loading {
  color: var(--text-primary);
  transition: color 0.3s ease;
}

.error {
  color: var(--danger-color);
}

.stats-overview {
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
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
}

.chart-card {
  background: var(--card-background);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  min-height: 400px;
  transition: all 0.3s ease;

  h3 {
    margin-bottom: 1rem;
    color: var(--text-primary);
    transition: color 0.3s ease;
  }
}
</style>
