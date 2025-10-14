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
}

const props = withDefaults(defineProps<Props>(), {
  title: 'Line Chart',
});

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
