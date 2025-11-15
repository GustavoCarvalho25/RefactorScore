export interface Project {
  name: string;
  totalCommits: number;
  averageNote: number;
  lastAnalysisDate: string | null;
  mainLanguage: string;
  totalFiles: number;
}
