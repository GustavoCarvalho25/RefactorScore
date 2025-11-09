import { ref, type Ref } from 'vue';
import axios, { AxiosError, Method } from 'axios';

export interface ErrorResponse {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

export interface HttpResponse<T> {
  result: Ref<T | undefined>;
  error: Ref<AxiosError<ErrorResponse> | null>;
}

export enum Service {
  Dashboard ='/api/v1/main',
  Analysis = '/api/v1/analysis',
  Statistics = '/api/v1/statistics',
  Projects = '/api/v1/projects'
}

export function useFetch(service?: Service) {
  const result = ref();
  const loading = ref(false);
  const error = ref<AxiosError<ErrorResponse, any> | null>(null);

  const baseURL = '';
  
  const api = axios.create({
    baseURL: service ? `${baseURL}${service}` : baseURL,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  const fetchData = async (
    method: Method,
    url: string,
    data?: object | string,
    contentType = 'application/json'
  ) => {
    error.value = null;
    loading.value = true;

    try {
      const response = await api.request({
        method,
        url,
        data,
        headers: {
          'Content-Type': contentType,
          Accept: 'application/json, text/plain, */*',
        },
      });

      // Handle nested response structure
      if (response.data && typeof response.data === 'object') {
        // Check if response has success and analysis fields
        if ('success' in response.data && 'analysis' in response.data) {
          result.value = response.data.analysis;
        } else {
          result.value = response.data;
        }
      } else {
        result.value = response.data;
      }
    } catch (err) {
      const axiosError = err as AxiosError<ErrorResponse, any>;
      error.value = axiosError;
    } finally {
      loading.value = false;
    }
  };

  return {
    result,
    error,
    loading,
    fetchData,
  };
}
