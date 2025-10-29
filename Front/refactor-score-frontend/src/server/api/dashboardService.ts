import { useFetch, Service, HttpResponse } from '../../composables/useFetch';
import type { Statistics } from '../../interfaces/Statistics';

export function useDashboardService() {
  const { fetchData, error, result, loading } = useFetch(Service.Dashboard);

  const getDashboardStatistics = async (startDate?: string, endDate?: string): Promise<HttpResponse<Statistics>> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    
    const queryString = params.toString();
    await fetchData('get', queryString ? `?${queryString}` : '');
    return {
      error,
      result,
    };
  };

  return {
    getDashboardStatistics,
    loading,
    error,
    result,
  };
}
