/**
 * Structured error response from the API
 */
export interface ApiError {
  statusCode: number;
  message: string;
  exceptionType: string;
  isBusinessException: boolean;
  isTechnicalException: boolean;
  timestamp: string;
}

/**
 * Custom error class for API client errors
 */
export class ApiClientError extends Error {
  constructor(public error: ApiError) {
    super(error.message);
    this.name = 'ApiClientError';
  }
}

