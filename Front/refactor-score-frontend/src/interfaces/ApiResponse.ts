export interface ApiResponse<T> {
  success: boolean;
  analysis: T;
}