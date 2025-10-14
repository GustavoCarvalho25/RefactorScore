import { ref } from 'vue';
import axios, { AxiosError, Method } from 'axios';

export interface ErrorResponse {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

export interface HttpResponse<T> {
  result: any;
  error: any;
}

export enum Service {
  Analysis = '/api/v1/analysis',
}

export function useFetch(service?: Service) {
  const result = ref();
  const loading = ref(false);
  const error = ref<AxiosError<ErrorResponse, any> | null>(null);

  // TODO: Configurar a URL base da API quando estiver disponível
  const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5000';
  
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

      result.value = response.data;
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
