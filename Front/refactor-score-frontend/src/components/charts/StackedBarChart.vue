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
		count?: number;
		backgroundColor?: string | string[];
		borderColor?: string | string[];
	}[];
	title?: string;
}

const props = withDefaults(defineProps<Props>(), {
	title: 'Stacked Bar Chart',
});

const { isDark } = useTheme();

const chartData = computed(() => ({
	labels: props.labels,
	datasets: props.datasets.map((dataset) => ({
		label: dataset.label,
		data: dataset.data,
		count: dataset.count, // passa a contagem para o Chart.js
		backgroundColor: dataset.backgroundColor || 'rgba(68, 123, 218, 0.8)',
		borderColor: dataset.borderColor || 'rgba(68, 123, 218, 1)',
		borderWidth: 1,
	})),
}));

const chartOptions = computed(() => {
	const textColor = isDark.value ? '#ffffff' : '#2c3e50';
	const gridColor = isDark.value ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0,0,0,0.1)';

	return {
		indexAxis: 'y' as const,
		responsive: true,
		maintainAspectRatio: false,
		barThickness: 11, // Aumentada a espessura global das barras
		categorySpacing: 4, // Aumentado o espaçamento entre categorias
		barSpacing: 2, // Aumentado o espaçamento entre barras
		plugins: {
			legend: {
				display: true,
				position: 'top' as const,
				labels: {
					color: textColor,
				},
			},
			title: {
				display: true,
				text: props.title,
				color: textColor,
				font: { size: 16 },
			},
			tooltip: {
        callbacks: {
          label: (context: any) => {
            const dataset = context.dataset;
            const label = dataset.label;
            const percent = context.raw;
            const count = dataset.count;
            const arquivos = count === 1 ? 'arquivo' : 'arquivos';
            return `${label}: ${percent}% (${count} ${arquivos})`;
          },
        },
      },
		},
		scales: {
			x: {
				stacked: true,
				max: 100,
				display: false,
				ticks: { color: textColor },
				grid: { color: gridColor },
			},
			y: {
				stacked: true,
				ticks: { 
					color: textColor,
					font: {
						size: 11 // Tamanho da fonte ajustado
					}
				},
				grid: { 
					color: gridColor,
					lineWidth: 0.5 // Linha da grade mais fina
				},
				maxBarThickness: 11 // Aumentada a espessura máxima
			},
		},
	};
});
</script>

<style scoped>
.chart-container {
	width: 100%;
}
</style>
