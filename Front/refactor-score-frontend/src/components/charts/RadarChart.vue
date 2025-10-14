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

interface Props {
  chartId: string;
  rating: CleanCodeRating;
  title?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Clean Code Rating',
});

const chartData = computed(() => ({
  labels: [
    'Variable Naming',
    'Function Sizes',
    'No Needs Comments',
    'Method Cohesion',
    'Dead Code',
  ],
  datasets: [
    {
      label: props.title,
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

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    r: {
      beginAtZero: true,
      max: 10,
      ticks: {
        stepSize: 2,
      },
    },
  },
  plugins: {
    legend: {
      display: true,
      position: 'top' as const,
    },
    title: {
      display: true,
      text: props.title,
    },
  },
}));
</script>
