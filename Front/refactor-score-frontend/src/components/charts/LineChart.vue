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
  labels: string[];
  datasets: {
    label: string;
    data: number[];
    borderColor?: string;
    backgroundColor?: string;
  }[];
  title?: string;
  yAxisStepSize?: number;
  yAxisMax?: number;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Line Chart',
  yAxisStepSize: undefined,
  yAxisMax: undefined,
});

const { isDark } = useTheme();

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map((dataset) => ({
    label: dataset.label,
    data: dataset.data,
    borderColor: dataset.borderColor || 'rgba(68, 123, 218, 1)',
    backgroundColor: dataset.backgroundColor || 'rgba(68, 123, 218, 0.1)',
    tension: 0.4,
    fill: true,
  })),
}));

const chartOptions = computed(() => {
  // Usa isDark.value para reagir às mudanças de tema
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
