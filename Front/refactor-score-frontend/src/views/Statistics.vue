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
        <div class="stats-notes">
          <div class="stat-card">
            <h3>Melhor Nota</h3>
            <p class="stat-value">{{bestNote}}</p>
          </div>
          <div class="stat-card">
            <h3>Pior Nota</h3>
            <p class="stat-value">{{ worstNote }}</p>
          </div>
        </div>
        <div class="stat-card chart-card">
          <h3>Distribuição de Qualidade</h3>
          <BarChart
            v-if="commitsEvolution.length > 0"
            chart-id="quality-distribution"
            :labels="qualityDistribution.labels"
            :datasets="qualityDistribution.datasets"
            title="Commits por Qualidade"
          />
        </div>
      </div>

      <div class="charts-grid">
        <div class="chart-card">
          <h3>Top Autores</h3>
          <DoughnutChart
            v-if="topAuthors.labels.length > 0"
            chart-id="top-authors"
            :labels="topAuthors.labels"
            :datasets="topAuthors.datasets"
            title="Autores com Mais Commits"
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
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useFetch, Service } from '../composables/useFetch';
import type { CommitAnalysis } from '../interfaces/CommitAnalysis';
import type { CleanCodeRating } from '../interfaces/CleanCodeRating';
import { translateQuality } from '../utils/translations';
import BarChart from '../components/charts/BarChart.vue';
import LineChart from '../components/charts/LineChart.vue';
import RadarChart from '../components/charts/RadarChart.vue';
import DoughnutChart from '../components/charts/DoughnutChart.vue';

const { fetchData, loading, error: apiError, result } = useFetch(Service.Statistics);

interface Metrics {
  variableNaming: number;
  functionSizes: number;
  noNeedsComments: number;
  methodCohesion: number;
  deadCode: number;
}

interface Commit {
  id: string;
  author: string;
  language: string;
  note: number;
  quality: string;
  metrics: Metrics;
}

const commitsEvolution = ref<Commit[]>([]);
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
  // Define as qualidades em ordem específica
  const qualities = [
    'Excellent',
    'Very Good',
    'Good',
    'Acceptable',
    'Needs Improvement',
    'Problematic'
  ];

  const totalCommits = commitsEvolution.value.length;
  
  // Inicializa contagem com zeros
  const counts = new Array(qualities.length).fill(0);

  // Conta os commits por qualidade
  commitsEvolution.value.forEach((commit: Commit) => {
    const index = qualities.indexOf(commit.quality);
    if (index !== -1) {
      counts[index]++;
    }
  });

  // Calcula as porcentagens
  const percentages = counts.map(count => 
    totalCommits > 0 ? (count / totalCommits) * 100 : 0
  );

  // Log para debug
  console.log('Estatísticas:', {
    totalCommits,
    contagens: Object.fromEntries(qualities.map((q, i) => [q, counts[i]])),
    porcentagens: Object.fromEntries(qualities.map((q, i) => [q, (percentages[i] ?? 0).toFixed(1) + '%']))
  });

  return {
    labels: qualities.map(q => translateQuality(q)),
    datasets: [
      {
        label: 'Porcentagem de Commits',
        data: percentages,
        backgroundColor: [
          'rgb(39, 174, 96)',   // Excellent - Verde escuro
          'rgb(46, 204, 113)',  // Very Good - Verde claro
          'rgb(243, 156, 18)',  // Good - Amarelo
          'rgb(230, 126, 34)',  // Acceptable - Laranja
          'rgb(231, 76, 60)',   // Needs Improvement - Vermelho claro
          'rgb(192, 57, 43)',   // Problematic - Vermelho escuro
        ],
        borderColor: [
          'rgb(39, 174, 96)',   // Excellent - Verde escuro
          'rgb(46, 204, 113)',  // Very Good - Verde claro
          'rgb(243, 156, 18)',  // Good - Amarelo
          'rgb(230, 126, 34)',  // Acceptable - Laranja
          'rgb(231, 76, 60)',   // Needs Improvement - Vermelho claro
          'rgb(192, 57, 43)',   // Problematic - Vermelho escuro
        ],
        borderWidth: 1
      }
    ]
  };
});

// Timeline removed as commit dates are not available in the current API response

const averageMetrics = computed((): CleanCodeRating | null => {
  if (commitsEvolution.value.length === 0) return null;

  // Calcula a média de cada métrica
  const totalCommits = commitsEvolution.value.length;
  const sumMetrics = commitsEvolution.value.reduce((acc, commit) => {
    if (!commit.metrics) return acc;
    
    return {
      variableNaming: acc.variableNaming + commit.metrics.variableNaming,
      functionSizes: acc.functionSizes + commit.metrics.functionSizes,
      noNeedsComments: acc.noNeedsComments + commit.metrics.noNeedsComments,
      methodCohesion: acc.methodCohesion + commit.metrics.methodCohesion,
      deadCode: acc.deadCode + commit.metrics.deadCode,
    };
  }, {
    variableNaming: 0,
    functionSizes: 0,
    noNeedsComments: 0,
    methodCohesion: 0,
    deadCode: 0,
  });

  // Calcula a nota média geral
  const avgNote = commitsEvolution.value.reduce((sum, c) => sum + c.note, 0) / totalCommits;

  return {
    variableNaming: sumMetrics.variableNaming / totalCommits,
    functionSizes: sumMetrics.functionSizes / totalCommits,
    noNeedsComments: sumMetrics.noNeedsComments / totalCommits,
    methodCohesion: sumMetrics.methodCohesion / totalCommits,
    deadCode: sumMetrics.deadCode / totalCommits,
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

  const colors = [
    'rgba(68, 123, 218, 0.8)',
    'rgba(75, 192, 192, 0.8)',
    'rgba(255, 206, 86, 0.8)',
    'rgba(153, 102, 255, 0.8)',
    'rgba(255, 159, 64, 0.8)',
    'rgba(235, 99, 132, 0.8)',
    'rgba(102, 187, 106, 0.8)',
    'rgba(121, 134, 203, 0.8)',
    'rgba(255, 138, 101, 0.8)',
    'rgba(149, 117, 205, 0.8)',
  ];

  const borderColors = colors.map(color => color.replace('0.8)', '1)'));

  return {
    labels: sorted.map(([author]) => author),
    datasets: [
      {
        label: 'Commits',
        data: sorted.map(([, count]) => count),
        backgroundColor: colors,
        borderColor: borderColors,
      },
    ],
  };
});

const loadStatistics = async () => {
  error.value = null;

  try {
    await fetchData('get', '/');
    
    if (apiError.value) {
      error.value = 'Erro ao carregar estatísticas';
      console.error('Error loading statistics:', apiError.value);
    } else if (result.value) {
      commitsEvolution.value = result.value.commits || [];
      bestNote.value = result.value.bestNote || 0;
      worstNote.value = result.value.worstNote || 0;
      
      console.log('Dados carregados:', {
        commits: commitsEvolution.value.length,
        autores: [...new Set(commitsEvolution.value.map(c => c.author))],
        amostraCommit: commitsEvolution.value[0], // Mostra o primeiro commit como exemplo
        qualidades: [...new Set(commitsEvolution.value.map(c => c.quality))], // Lista todas as qualidades únicas
        dadosCompletos: result.value // Mostra todos os dados recebidos
      });
    }
  } catch (err) {
    error.value = 'Erro ao carregar estatísticas';
    console.error('Error loading statistics:', err);
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
  grid-template-columns: minmax(500px, 1fr) minmax(500px, 1fr);
  justify-content: center;
  gap: 1.5rem;
  margin-bottom: 2rem;
  max-width: 100%;
}

.stats-notes {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  height: 100%;
  
  .stat-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    justify-content: center;
    padding: 2.6rem 2rem; // Aumentando o padding vertical em 30% (2rem -> 2.6rem)

    h3 {
      margin-bottom: 1rem; // Aumentando o espaço entre o título e o valor
    }

    .stat-value {
      font-size: 3rem; // Aumentando um pouco o tamanho do valor também
    }
  }
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

  &.chart-card {
    height: 100%;
    min-height: 100px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 1rem;

    h3 {
      text-align: center;
      margin-bottom: 1.5rem;
    }
  }
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1rem;
}

.chart-card {
  background: var(--card-background);
  padding: 1.5rem 1rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  height: 400px;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  overflow: hidden;

  h3 {
    margin-bottom: 1rem;
    color: var(--text-primary);
    transition: color 0.3s ease;
    flex-shrink: 0;
  }

  :deep(.chartjs-render-monitor) {
    height: 100% !important;
    width: 100% !important;
  }
}
</style>
