<template>
  <BaseChart
    :chart-id="chartId"
    type="line"
    :data="chartData"
    :options="chartOptions"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import BaseChart from './BaseChart.vue';
import { useTheme } from '../../composables/useTheme';

interface Props {
  chartId: string;
  labels: string[]; // Estas devem ser strings de data válidas (ex: 'YYYY-MM-DD')
  datasets: {
    label: string;
    data: number[];
    borderColor?: string;
    backgroundColor?: string;
  }[];
  title?: string;
  yAxisStepSize?: number;
  yAxisMax?: number;
  // NOVAS PROPS PARA O FILTRO DE DATA
  startDate?: string; // Data de início do filtro (ex: 'YYYY-MM-DD')
  endDate?: string;   // Data de fim do filtro (ex: 'YYYY-MM-DD')
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Line Chart',
  yAxisStepSize: undefined,
  yAxisMax: undefined,
  startDate: undefined,
  endDate: undefined,
});

const { isDark } = useTheme();

// --- LÓGICA DE FILTRAGEM ---
const filteredData = computed(() => {
  const allLabels = props.labels;
  const allDatasets = props.datasets;

  let startIndex = 0;
  let endIndex = allLabels.length - 1;

  // 1. Encontrar o intervalo de índices com base nas datas de início e fim
  if (props.startDate || props.endDate) {
    // Para comparação, convertemos as strings de data para o formato numérico (timestamp)
    const startTimestamp = props.startDate ? new Date(props.startDate).getTime() : -Infinity;
    const endTimestamp = props.endDate ? new Date(props.endDate).getTime() : Infinity;

    // Encontra o índice inicial
    startIndex = allLabels.findIndex(label => {
      if (!label) return false;
      const labelTimestamp = new Date(label).getTime();
      return labelTimestamp >= startTimestamp;
    });
    
    // Se não encontrou, ou a data inicial é maior que a última data, não há dados para mostrar.
    if (startIndex === -1 && startTimestamp !== -Infinity) {
      startIndex = allLabels.length; // Garante que o slice retorne vazio
    } else if (startIndex === -1) {
      startIndex = 0; // Se não houver startDate, começa do 0
    }
    
    // Encontra o índice final (procura do início até o fim)
    let tempEndIndex = -1;
    for (let i = startIndex; i < allLabels.length; i++) {
        const label = allLabels[i];
        if (!label) continue;
        const labelTimestamp = new Date(label).getTime();
        if (labelTimestamp <= endTimestamp) {
            tempEndIndex = i;
        } else {
            // Se a data do rótulo for maior que a data final, podemos parar de buscar
            break;
        }
    }
    endIndex = tempEndIndex;
    
    // Se startIndex for após endIndex (por exemplo, filtro fora do intervalo de dados),
    // definimos um intervalo vazio.
    if (startIndex > endIndex) {
        return {
            labels: [],
            datasets: allDatasets.map(d => ({ ...d, data: [] })),
        };
    }
  }

  // Se não houver filtro, ou se o filtro for válido, fatia os dados.
  const labels = allLabels.slice(startIndex, endIndex + 1);
  const datasets = allDatasets.map(dataset => ({
    ...dataset,
    data: dataset.data.slice(startIndex, endIndex + 1),
  }));

  return { labels, datasets };
});
// --- FIM DA LÓGICA DE FILTRAGEM ---


const chartData = computed(() => ({
  labels: filteredData.value.labels, // Usa os labels filtrados
  datasets: filteredData.value.datasets.map((dataset) => ({
    label: dataset.label,
    data: dataset.data,
    borderColor: dataset.borderColor || 'rgba(68, 123, 218, 1)',
    backgroundColor: dataset.backgroundColor || 'rgba(68, 123, 218, 0.1)',
    tension: 0.4,
    fill: true,
  })),
}));

const chartOptions = computed(() => {
  // ... (RESTO DO CÓDIGO DO chartOptions PERMANECE O MESMO) ...
  const textColor = isDark.value ? '#ffffff' : '#2c3e50';
  const gridColor = isDark.value ? 'rgba(255, 255, 255, 0.2)' : 'rgba(0, 0, 0, 0.1)';
  
  const yAxisConfig: any = {
    beginAtZero: true,
    ticks: {
      color: textColor,
    },
    grid: {
      color: gridColor,
    },
  };

  // Adiciona stepSize se fornecido
  if (props.yAxisStepSize) {
    yAxisConfig.ticks.stepSize = props.yAxisStepSize;
  }

  // Adiciona max se fornecido
  if (props.yAxisMax !== undefined) {
    yAxisConfig.max = props.yAxisMax;
  }
  
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top' as const,
        labels: {
          color: textColor,
          font: {
            size: 12,
          },
        },
      },
      title: {
        display: true,
        text: props.title,
        color: textColor,
        font: {
          size: 16,
        },
      },
    },
    scales: {
      y: yAxisConfig,
      x: {
        ticks: {
          color: textColor,
        },
        grid: {
          color: gridColor,
        },
      },
    },
  };
});
</script>