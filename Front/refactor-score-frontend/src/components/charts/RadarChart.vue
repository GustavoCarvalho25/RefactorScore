<template>
  <BaseChart
    :chart-id="chartId"
    type="radar"
    :data="chartData"
    :options="chartOptions"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import BaseChart from './BaseChart.vue';
import { CleanCodeRating } from '../../interfaces/CleanCodeRating';
import { useTheme } from '../../composables/useTheme';
import { translateMetric } from '../../utils/translations';

const props = defineProps<{
  chartId: string;
  rating: CleanCodeRating;
  title?: string;
}>();

const title = props.title ?? 'Clean Code Rating';

const { isDark } = useTheme();

const chartData = computed(() => ({
  labels: [
    translateMetric('Variable Naming'),
    translateMetric('Function Sizes'),
    translateMetric('No Needs Comments'),
    translateMetric('Method Cohesion'),
    translateMetric('Dead Code'),
  ],
  datasets: [
    {
      label: title,
      data: [
        props.rating.variableNaming,
        props.rating.functionSizes,
        props.rating.noNeedsComments,
        props.rating.methodCohesion,
        props.rating.deadCode,
      ],
      backgroundColor: 'rgba(68, 123, 218, 0.2)',
      borderColor: 'rgba(68, 123, 218, 1)',
      borderWidth: 2,
      pointBackgroundColor: 'rgba(68, 123, 218, 1)',
      pointBorderColor: '#fff',
      pointHoverBackgroundColor: '#fff',
      pointHoverBorderColor: 'rgba(68, 123, 218, 1)',
    },
  ],
}));

const chartOptions = computed(() => {
  const textColor = isDark.value ? '#ffffff' : '#2c3e50';
  const gridColor = isDark.value ? 'rgba(255, 255, 255, 0.2)' : 'rgba(0, 0, 0, 0.1)';
  
  return {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      r: {
        beginAtZero: true,
        max: 10,
        ticks: {
          stepSize: 2,
          color: textColor,
          backdropColor: isDark.value ? 'rgba(22, 33, 62, 0.95)' : '#ffffff',
          font: {
            weight: 'bold' as const
          }
        },
        grid: {
          color: gridColor,
        },
        pointLabels: {
          color: textColor,
          font: {
            size: 12,
          },
        },
        angleLines: {
          color: gridColor
        },
      },
    },
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
        text: title,
        color: textColor,
        font: {
          size: 16,
        },
      },
    },
  };
});
</script>
