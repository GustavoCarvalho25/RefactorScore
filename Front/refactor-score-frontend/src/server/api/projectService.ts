import { useFetch, Service, HttpResponse } from '../../composables/useFetch';
import type { Project } from '../../interfaces/Project';

export function useProjectService() {
  const { fetchData, error, result, loading } = useFetch(Service.Projects);

  const getProjects = async (): Promise<HttpResponse<Project[]>> => {
    await fetchData('get', '');
    return {
      error,
      result,
    };
  };

  return {
    getProjects,
    loading,
    error,
    result,
  };
}
