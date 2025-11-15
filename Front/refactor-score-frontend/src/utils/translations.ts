// Traduções para exibição no frontend
export const qualityTranslations: Record<string, string> = {
  'Excellent': 'Excelente',
  'Very Good': 'Muito Bom',
  'VeryGood': 'Muito Bom',
  'Good': 'Bom',
  'Acceptable': 'Aceitável',
  'Needs Improvement': 'Precisa Melhorar',
  'NeedsImprovement': 'Precisa Melhorar',
  'Problematic': 'Problemático',
  'Unknown': 'Desconhecido'
};

export const metricTranslations: Record<string, string> = {
  'Variable Naming': 'Nomenclatura de Variáveis',
  'Function Sizes': 'Tamanho de Funções',
  'No Needs Comments': 'Código Autoexplicativo',
  'Method Cohesion': 'Coesão de Métodos',
  'Dead Code': 'Código Morto'
};

export function translateQuality(quality: string): string {
  return qualityTranslations[quality] || quality;
}

export function translateMetric(metric: string): string {
  return metricTranslations[metric] || metric;
}
