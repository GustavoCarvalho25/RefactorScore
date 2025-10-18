<template>
  <BaseChart
    :chart-id="chartId"
    type="pie"
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
    backgroundColor?: string[];
    borderColor?: string[];
  }[];
  title?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Pie Chart',
});

const { themeChangeCounter } = useTheme();

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map((dataset) => ({
    label: dataset.label,
    data: dataset.data,
    backgroundColor: dataset.backgroundColor || [
      'rgba(68, 123, 218, 0.8)',
      'rgba(75, 192, 192, 0.8)',
      'rgba(255, 206, 86, 0.8)',
      'rgba(153, 102, 255, 0.8)',
      'rgba(255, 159, 64, 0.8)',
    ],
    borderColor: dataset.borderColor || [
      'rgba(68, 123, 218, 1)',
      'rgba(75, 192, 192, 1)',
      'rgba(255, 206, 86, 1)',
      'rgba(153, 102, 255, 1)',
      'rgba(255, 159, 64, 1)',
    ],
    borderWidth: 2,
  })),
}));

const chartOptions = computed(() => {
  // Força a recomputação quando o tema muda
  themeChangeCounter.value;
  
  const isDarkMode = document.documentElement.classList.contains('dark-mode');
  const textColor = isDarkMode ? '#eaeaea' : '#2c3e50';
  
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
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
      tooltip: {
        callbacks: {
          label: function(context: any) {
            const label = context.label || '';
            const value = context.parsed || 0;
            const total = context.dataset.data.reduce((acc: number, data: number) => acc + data, 0);
            const percentage = ((value / total) * 100).toFixed(2);
            return `${label}: ${value} (${percentage}%)`;
          }
        }
      },
    },
  };
});
</script>

