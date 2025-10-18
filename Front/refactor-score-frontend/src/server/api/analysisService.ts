import { useFetch, Service, HttpResponse } from '../../composables/useFetch';
import type { CommitAnalysis } from '../../interfaces/CommitAnalysis';

export function useAnalysisService() {
  const { fetchData, error, result, loading } = useFetch(Service.Analysis);

  const getAnalyses = async (params = ''): Promise<HttpResponse<CommitAnalysis[]>> => {
    await fetchData('get', `?${params}`);
    return {
      error,
      result,
    };
  };

  const getAnalysisById = async (id: string): Promise<HttpResponse<CommitAnalysis>> => {
    await fetchData('get', `/${id}`);
    return {
      error,
      result,
    };
  };

  const getAnalysisByCommitId = async (commitId: string): Promise<HttpResponse<CommitAnalysis>> => {
    await fetchData('get', `/commit/${commitId}`);
    return {
      error,
      result,
    };
  };

  const getAnalysisStatistics = async (): Promise<HttpResponse<any>> => {
    await fetchData('get', '');
    return {
      error,
      result,
    };
  };

  const getAnalysisByDateRange = async (startDate: string, endDate: string): Promise<HttpResponse<CommitAnalysis[]>> => {
    await fetchData('get', `/date-range?start=${startDate}&end=${endDate}`);
    return {
      error,
      result,
    };
  };

  const getAnalysisByAuthor = async (author: string): Promise<HttpResponse<CommitAnalysis[]>> => {
    await fetchData('get', `/author/${author}`);
    return {
      error,
      result,
    };
  };

  const getCommitAnalysisCount = async (): Promise<HttpResponse<any>> => {
    await fetchData('get', '/count');
    return {
      error,
      result,
    };
  };

  return {
    getAnalyses,
    getAnalysisById,
    getAnalysisByCommitId,
    getAnalysisStatistics,
    getAnalysisByDateRange,
    getAnalysisByAuthor,
    getCommitAnalysisCount,
    loading,
    error,
    result,
  };
}
