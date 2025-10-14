import { defineStore } from 'pinia';
import { CommitAnalysis } from '../interfaces/CommitAnalysis';

interface State {
  analyses: CommitAnalysis[];
  currentAnalysis: CommitAnalysis | null;
  loading: boolean;
  error: string | null;
}

export const useAnalysisStore = defineStore('analysis', {
  state: (): State => ({
    analyses: [],
    currentAnalysis: null,
    loading: false,
    error: null,
  }),
  
  getters: {
    getAnalysisById: (state) => (id: string) => {
      return state.analyses.find((analysis) => analysis.id === id);
    },
    
    getAnalysesByAuthor: (state) => (author: string) => {
      return state.analyses.filter((analysis) => analysis.author === author);
    },
    
    averageOverallNote: (state) => {
      if (state.analyses.length === 0) return 0;
      const sum = state.analyses.reduce((acc, analysis) => acc + analysis.overallNote, 0);
      return sum / state.analyses.length;
    },
  },
  
  actions: {
    setAnalyses(analyses: CommitAnalysis[]) {
      this.analyses = analyses;
    },
    
    setCurrentAnalysis(analysis: CommitAnalysis | null) {
      this.currentAnalysis = analysis;
    },
    
    addAnalysis(analysis: CommitAnalysis) {
      this.analyses.push(analysis);
    },
    
    setLoading(loading: boolean) {
      this.loading = loading;
    },
    
    setError(error: string | null) {
      this.error = error;
    },
  },
});
