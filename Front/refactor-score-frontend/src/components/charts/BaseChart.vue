<template>
  <div class="chart-container">
    <canvas :id="chartId"></canvas>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, watch, ref, nextTick } from 'vue';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  registerables,
} from 'chart.js';
import { useTheme } from '../../composables/useTheme';

Chart.register(...registerables);

const { isDark } = useTheme();

interface Props {
  chartId: string;
  type: ChartType;
  data: ChartConfiguration['data'];
  options?: ChartConfiguration['options'];
}

const props = withDefaults(defineProps<Props>(), {
  options: () => ({}),
});

const chartInstance = ref<Chart | null>(null);

const createChart = () => {
  const canvas = document.getElementById(props.chartId) as HTMLCanvasElement;
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const defaultOptions = {
    plugins: {
      legend: {
        labels: {
          color: isDark.value ? '#ffffff' : '#2c3e50'
        }
      }
    },
    scales: {
      x: {
        ticks: {
          color: isDark.value ? '#a0a0a0' : '#7f8c8d'
        },
        grid: {
          color: isDark.value ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)'
        }
      },
      y: {
        ticks: {
          color: isDark.value ? '#a0a0a0' : '#7f8c8d'
        },
        grid: {
          color: isDark.value ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)'
        }
      }
    }
  };

  chartInstance.value = new Chart(ctx, {
    type: props.type,
    data: props.data,
    options: {
      ...defaultOptions,
      ...props.options,
    },
  });
};

const destroyChart = () => {
  if (chartInstance.value) {
    chartInstance.value.destroy();
    chartInstance.value = null;
  }
};

const updateChart = () => {
  if (chartInstance.value) {
    chartInstance.value.data = props.data;
    chartInstance.value.update();
  }
};

const recreateChart = async () => {
  destroyChart();
  await nextTick();
  createChart();
};

onMounted(() => {
  createChart();
});

onBeforeUnmount(() => {
  destroyChart();
});

watch(
  () => props.data,
  () => {
    updateChart();
  },
  { deep: true }
);

watch(
  () => props.options,
  () => {
    recreateChart();
  },
  { deep: true }
);

watch(isDark, () => {
  recreateChart();
});
</script>

<style scoped lang="scss">
.chart-container {
  position: relative;
  width: 100%;
  height: 100%;
  max-width: 100%;
  padding: 10px;
  box-sizing: border-box;
  
  canvas {
    max-width: 100% !important;
    max-height: 100% !important;
  }
}
</style>
