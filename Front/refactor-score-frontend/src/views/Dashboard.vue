<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div class="header-content">
        <div>
          <h1>RefactorScore Dashboard</h1>
          <p>Análise de Commits com Clean Code</p>
        </div>
      </div>
    </header>

    <div class="dashboard-content">
      <div class="stats-section">
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
        
        <div class="language-distribution-card">
          <div class="chart-wrapper" style="height: 100%; width: 100%; display: flex; align-items: center; justify-content: center;">
            <StackedBarChart
              v-if="stackedLanguageChartData.labels.length > 0"
              chart-id="language-distribution-stacked"
              :labels="stackedLanguageChartData.labels"
              :datasets="stackedLanguageChartData.datasets"
              title="Distribuição de Arquivos por Linguagem"
            />
            <div v-else class="no-data">
              <p>Sem dados para exibir</p>
            </div>
          </div>
        </div>
      </div>

      <div class="chart-card full-width">
        <div class="filter-controls">
          <label for="startDate">Data Início:</label>
          <input id="startDate" type="date" v-model="filterStartDate" class="date-input" />
          
          <label for="endDate">Data Fim:</label>
          <input id="endDate" type="date" v-model="filterEndDate" class="date-input" />
        </div>
        <div class="chart-wrapper">
          <div class="chart-container" style="position: relative; height: 100%; width: 100%;">
            <template v-if="commitsEvolution && commitsEvolution.length > 0">
              <LineChart
                :key="chartKey"
                :chart-id="`quality-evolution-${chartKey}`"
                :labels="lineChartData.labels"
                :datasets="lineChartData.datasets"
                :y-axis-step-size="1"
                :y-axis-max="10"
                :on-point-click="handlePointClick"
                title="Evolução das Notas dos Commits ao Longo do Tempo"
              />
            </template>
            <template v-else>
              <div class="no-data">
                <p>Sem dados para exibir</p>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'; 
import { useRouter } from 'vue-router';
import { useAnalysisStore } from '../stores/analysisStore';
import { useDashboardService } from '../server/api/dashboardService';
import { useAnalysisService } from '../server/api/analysisService';
import LineChart from '../components/charts/LineChart.vue';
import StackedBarChart from '../components/charts/StackedBarChart.vue';

const router = useRouter();
const analysisStore = useAnalysisStore();
const dashboardService = useDashboardService();
const analysisService = useAnalysisService();

interface CommitEvolution {
  id: string;
  commitDate: string;
  note: number;
}

interface LanguageFrequency {
  language: string;
  count: number;
}

const analyses = ref<any[]>([]);
const commitCount = ref<number>(0);
const averageNote = ref<number>(0);
const uniqueFilesCount = ref<number>(0);
const totalSuggestions = ref<number>(0);
const bestNote = ref<number>(0);
const worstNote = ref<number>(0);
const languageFrequency = ref<LanguageFrequency[]>([]);
const commitsEvolution = ref<CommitEvolution[]>([]);

// REFS PARA O FILTRO DE DATA
const filterStartDate = ref<string>('');
const filterEndDate = ref<string>('');

// LÓGICA DO GRÁFICO DE LINHA (INCLUINDO FILTRO)
// Referência para forçar recriação do gráfico
const chartKey = ref(0);
// Armazena os commits filtrados para acessar pelo índice
const filteredCommits = ref<CommitEvolution[]>([]);

// Computed para processar os dados do gráfico
const lineChartData = computed(() => {
  if (!commitsEvolution.value || commitsEvolution.value.length === 0) {
    return { labels: [], datasets: [] };
  }

  try {
    // 1. Preparar dados
    let commits = [...commitsEvolution.value];
    
    // 2. Aplicar filtros de data
    if (filterStartDate.value || filterEndDate.value) {
      // Função para converter data para o formato YYYY-MM-DD
      const formatDate = (dateString: string) => {
        try {
          const date = new Date(dateString);
          return date.toISOString().split('T')[0];
        } catch (error) {
          console.error('Erro ao formatar data:', dateString, error);
          return '';
        }
      };

      // Converter datas do filtro para YYYY-MM-DD
      const startDate = filterStartDate.value ? formatDate(filterStartDate.value) : '';
      const endDate = filterEndDate.value ? formatDate(filterEndDate.value) : '';

      commits = commits.filter(commit => {
        // Converter a data do commit para YYYY-MM-DD
        const commitDate = formatDate(commit.commitDate);
        if (!commitDate) return false; // Ignora commits com data inválida

        // Verificar se está no intervalo
        const isAfterStart = !startDate || commitDate >= startDate;
        const isBeforeEnd = !endDate || commitDate <= endDate;
        const isInRange = isAfterStart && isBeforeEnd;
        
        return isInRange;
      });

      // Ordenar commits após a filtragem
      commits.sort((a, b) => {
        const dateA = new Date(a.commitDate).getTime();
        const dateB = new Date(b.commitDate).getTime();
        return dateA - dateB;
      });


    }

    // 3. Ordenar por data
    commits.sort((a, b) => new Date(a.commitDate).getTime() - new Date(b.commitDate).getTime());

    // Armazenar commits filtrados para uso no click
    filteredCommits.value = commits;

    // 4. Preparar dados para o gráfico
    const dates = commits.map(commit => formatDate(commit.commitDate));
    const notes = commits.map(commit => commit.note);

    return {
      labels: dates,
      datasets: [
        {
          label: 'Nota do Commit',
          data: notes,
          borderColor: 'rgba(68, 123, 218, 1)',
          backgroundColor: 'rgba(68, 123, 218, 0.2)',
          fill: true,
          tension: 0.4,
        }
      ]
    };
  } catch (error) {
    console.error('Erro ao processar dados:', error);
    return { labels: [], datasets: [] };
  }
});

// Transforma a distribuição por linguagem em um único gráfico de barras empilhadas
const stackedLanguageChartData = computed(() => {
  if (!languageFrequency.value || languageFrequency.value.length === 0) {
    return { labels: [], datasets: [] };
  }

  const total = languageFrequency.value.reduce((acc, cur) => acc + (cur.count || 0), 0) || 1;

  const colors = [
    'rgba(68, 123, 218, 0.8)',
    'rgba(75, 192, 192, 0.8)',
    'rgba(255, 206, 86, 0.8)',
    'rgba(153, 102, 255, 0.8)',
    'rgba(255, 159, 64, 0.8)',
    'rgba(255, 99, 132, 0.8)',
    'rgba(54, 162, 235, 0.8)',
    'rgba(201, 203, 207, 0.8)'
  ];

  // labels: apenas uma categoria para a barra empilhada
  const labels = [''];

  const datasets = languageFrequency.value.map((item, idx) => ({
    label: item.language,
    // cada dataset precisa ter um array com o mesmo tamanho de labels (1)
    data: [Math.round(((item.count || 0) / total) * 100)],
    count: item.count || 0, // contagem original para o tooltip
    backgroundColor: colors[idx % colors.length],
    borderColor: (colors[idx % colors.length] as string).replace('0.8', '1'),
    borderWidth: 1,
  }));

  return { labels, datasets };
});


const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  return `${day}/${month}/${year}`;
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

// Função para lidar com clique em ponto do gráfico
const handlePointClick = (datasetIndex: number, pointIndex: number) => {
  const commit = filteredCommits.value[pointIndex];
  if (commit && commit.id) {
    router.push({ name: 'Analysis', query: { commitId: commit.id } });
  }
};

const loadAnalyses = async () => {
  analysisStore.setLoading(true);
  try {
    const { result, error } = await analysisService.getAnalyses();
    if (error.value) {
      console.error('Error loading analyses:', error.value);
      analysisStore.setError('Erro ao carregar análises');
    } else if (result.value) {
      analyses.value = result.value || [];
      analysisStore.setAnalyses(analyses.value);
    }
  } catch (err) {
    console.error('Error loading analyses:', err);
    analysisStore.setError('Erro ao carregar análises');
  } finally {
    analysisStore.setLoading(false);
  }
};

// FUNÇÃO ATUALIZADA PARA ACEITAR ARGUMENTOS DE FILTRO
const loadStatistics = async () => {
  try {
    const { result, error } = await dashboardService.getDashboardStatistics();
    
    if (error.value) {
      console.error('Erro ao carregar estatísticas:', error.value);
      return;
    }

    if (result.value) {
      commitCount.value = result.value.total || 0;
      averageNote.value = Number(result.value.averageNote?.toFixed(2)) || 0;
      uniqueFilesCount.value = result.value.uniqueFilesCount || 0;
      totalSuggestions.value = result.value.totalSuggestions || 0;
      languageFrequency.value = result.value.languageFrequency || [];
      commitsEvolution.value = result.value.commitsEvolution || [];
    }
  } catch (err) {
    console.error('Erro ao carregar estatísticas:', err);
  }
};

// Observar mudanças nas datas do filtro
watch([filterStartDate, filterEndDate], () => {
  chartKey.value++;
});

onMounted(() => {
  loadAnalyses();
  // Carrega todas as estatísticas sem filtros
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

.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.stats-section {
  display: flex;
  gap: 1.5rem;
}

.dashboard-stats {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}

.language-distribution-card {
  flex: 0.9;
  background: var(--card-background);
  padding: 1rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px var(--shadow-color);
  min-height: auto;
  height: 300px; 
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
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

  &.full-width {
    height: 550px;
  }

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px var(--shadow-color);
  }
  
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
}

// ESTILOS DO FILTRO DE DATA
.filter-controls {
  display: flex;
  gap: 1.5rem;
  align-items: center;
  margin-bottom: 1rem;
  padding: 0 0.5rem;
  flex-shrink: 0;
  
  label {
    color: var(--text-secondary);
    font-size: 0.9rem;
    font-weight: 500;
  }
  
  .date-input {
    padding: 0.5rem;
    border: 1px solid var(--border-color);
    border-radius: 4px;
    background: var(--background-color);
    color: var(--text-primary);
    transition: all 0.3s ease;
    
    &:focus {
      border-color: var(--primary-color);
      box-shadow: 0 0 0 2px rgba(68, 123, 218, 0.2);
      outline: none;
    }
  }
}
// FIM ESTILOS DO FILTRO

.no-data {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  color: var(--text-secondary);
  font-size: 1.1rem;
}

// ... (Restante dos estilos, incluindo .score-excellent, etc.)
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