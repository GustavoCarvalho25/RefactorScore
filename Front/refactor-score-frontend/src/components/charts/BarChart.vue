<template>
  <BaseChart
    :chart-id="chartId"
    type="bar"
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
    backgroundColor?: string | string[];
    borderColor?: string | string[];
  }[];
  title?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Bar Chart',
});

const { isDark } = useTheme();

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map((dataset) => ({
    label: dataset.label,
    data: dataset.data,
    backgroundColor: dataset.backgroundColor || 'rgba(68, 123, 218, 0.6)',
    borderColor: dataset.borderColor || 'rgba(68, 123, 218, 1)',
    borderWidth: 1,
  })),
}));

const chartOptions = computed(() => {
  // Usa isDark.value para reagir às mudanças de tema
  const textColor = isDark.value ? '#ffffff' : '#2c3e50';
  
  return {
    responsive: true,
    maintainAspectRatio: true,
    aspectRatio: 2,
    plugins: {
      legend: {
        display: false, // Remove a legenda
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
      y: {
        beginAtZero: true,
        max: 100,
        ticks: {
          color: textColor,
          callback: function(value) {
            if (typeof value === 'number') {
              return value.toFixed(0) + '%';
            }
            return value;
          }
        },
        grid: {
          color: isDark.value ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)',
          drawBorder: false
        },
      },
      x: {
        ticks: {
          color: textColor,
        },
        grid: {
          color: isDark.value ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)',
          drawBorder: false
        },
      },
    },
  };
});
</script>
