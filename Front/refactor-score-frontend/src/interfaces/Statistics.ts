export interface Statistics {
  success: boolean;
  total: number;
  averageNote: number;
  bestNote: number;
  worstNote: number;
  uniqueFilesCount: number;
  totalSuggestions: number;
  languageFrequency: Array<{
    language: string;
    count: number;
  }>;
  commitsEvolution: Array<{
    id: string;
    commitId: string;
    author: string;
    commitDate: string;
    analysisDate: string;
    language: string;
    note: number;
    quality: string;
  }>;
}