import { useFetch, Service, HttpResponse } from '../../composables/useFetch';
import type { CommitAnalysis } from '../../interfaces/CommitAnalysis';
import type { Statistics } from '../../interfaces/Statistics';
import type { ApiResponse } from '../../interfaces/ApiResponse';

export function useAnalysisService() {
  const { fetchData, error, result, loading } = useFetch(Service.Analysis);

  const getAnalyses = async (params = ''): Promise<HttpResponse<CommitAnalysis[]>> => {
    await fetchData('get', `?${params}`);
    return {
      error,
      result,
    };
  };

  const getAnalysisById = async (id: string): Promise<HttpResponse<ApiResponse<CommitAnalysis[]>>> => {
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

  const getAnalysisStatistics = async (startDate?: string, endDate?: string): Promise<HttpResponse<Statistics>> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    
    const queryString = params.toString();
    await fetchData('get', queryString ? `/?${queryString}` : '/');
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
