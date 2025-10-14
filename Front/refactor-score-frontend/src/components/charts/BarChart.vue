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

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
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
  scales: {
    y: {
      beginAtZero: true,
    },
  },
}));
</script>
