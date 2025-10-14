<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <h1>RefactorScore Dashboard</h1>
      <p>Análise de Commits com Clean Code</p>
    </header>

    <div class="dashboard-stats">
      <div class="stat-card">
        <h3>Total de Análises</h3>
        <p class="stat-value">{{ totalAnalyses }}</p>
      </div>
      <div class="stat-card">
        <h3>Nota Média</h3>
        <p class="stat-value">{{ averageNote.toFixed(2) }}</p>
      </div>
      <div class="stat-card">
        <h3>Arquivos Analisados</h3>
        <p class="stat-value">{{ totalFiles }}</p>
      </div>
      <div class="stat-card">
        <h3>Sugestões Geradas</h3>
        <p class="stat-value">{{ totalSuggestions }}</p>
      </div>
    </div>

    <div class="dashboard-charts">
      <div class="chart-card">
        <h3>Evolução da Qualidade</h3>
        <LineChart
          v-if="lineChartData.labels.length > 0"
          chart-id="quality-evolution"
          :labels="lineChartData.labels"
          :datasets="lineChartData.datasets"
          title="Nota ao Longo do Tempo"
        />
      </div>

      <div class="chart-card">
        <h3>Distribuição por Linguagem</h3>
        <BarChart
          v-if="languageChartData.labels.length > 0"
          chart-id="language-distribution"
          :labels="languageChartData.labels"
          :datasets="languageChartData.datasets"
          title="Análises por Linguagem"
        />
      </div>
    </div>

    <div class="recent-analyses">
      <h3>Análises Recentes</h3>
      <div class="analysis-list">
        <div
          v-for="analysis in recentAnalyses"
          :key="analysis.id"
          class="analysis-item"
          @click="goToAnalysis(analysis.id)"
        >
          <div class="analysis-info">
            <h4>{{ analysis.commitId.substring(0, 8) }}</h4>
            <p>{{ analysis.author }} - {{ formatDate(analysis.commitDate) }}</p>
          </div>
          <div class="analysis-score">
            <span :class="getScoreClass(analysis.overallNote)">
              {{ analysis.overallNote.toFixed(2) }}
            </span>
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
import LineChart from '../components/charts/LineChart.vue';
import BarChart from '../components/charts/BarChart.vue';

const router = useRouter();
const analysisStore = useAnalysisStore();
const analysisService = useAnalysisService();

const analyses = ref<any[]>([]);

const totalAnalyses = computed(() => analyses.value.length);
const averageNote = computed(() => {
  if (analyses.value.length === 0) return 0;
  const sum = analyses.value.reduce((acc, a) => acc + a.overallNote, 0);
  return sum / analyses.value.length;
});
const totalFiles = computed(() =>
  analyses.value.reduce((acc, a) => acc + a.files.length, 0)
);
const totalSuggestions = computed(() =>
  analyses.value.reduce((acc, a) => acc + a.suggestions.length, 0)
);

const recentAnalyses = computed(() =>
  analyses.value.slice(0, 5).sort((a, b) =>
    new Date(b.analysisDate).getTime() - new Date(a.analysisDate).getTime()
  )
);

const lineChartData = computed(() => {
  const sortedAnalyses = [...analyses.value].sort((a, b) =>
    new Date(a.commitDate).getTime() - new Date(b.commitDate).getTime()
  );

  return {
    labels: sortedAnalyses.map((a) => formatDate(a.commitDate)),
    datasets: [
      {
        label: 'Nota Geral',
        data: sortedAnalyses.map((a) => a.overallNote),
        borderColor: 'rgba(68, 123, 218, 1)',
        backgroundColor: 'rgba(68, 123, 218, 0.1)',
      },
    ],
  };
});

const languageChartData = computed(() => {
  const languageCount: Record<string, number> = {};
  analyses.value.forEach((a) => {
    languageCount[a.language] = (languageCount[a.language] || 0) + 1;
  });

  return {
    labels: Object.keys(languageCount),
    datasets: [
      {
        label: 'Quantidade',
        data: Object.values(languageCount),
        backgroundColor: [
          'rgba(68, 123, 218, 0.6)',
          'rgba(75, 192, 192, 0.6)',
          'rgba(255, 206, 86, 0.6)',
          'rgba(153, 102, 255, 0.6)',
          'rgba(255, 159, 64, 0.6)',
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

onMounted(() => {
  loadAnalyses();
});
</script>

<style scoped lang="scss">
.dashboard {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.dashboard-header {
  margin-bottom: 2rem;

  h1 {
    font-size: 2.5rem;
    color: #2c3e50;
    margin-bottom: 0.5rem;
  }

  p {
    font-size: 1.1rem;
    color: #7f8c8d;
  }
}

.dashboard-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);

  h3 {
    font-size: 0.9rem;
    color: #7f8c8d;
    margin-bottom: 0.5rem;
    text-transform: uppercase;
  }

  .stat-value {
    font-size: 2.5rem;
    font-weight: bold;
    color: #447bda;
  }
}

.dashboard-charts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.chart-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  min-height: 400px;

  h3 {
    margin-bottom: 1rem;
    color: #2c3e50;
  }
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
