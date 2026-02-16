import { getAccessToken } from '@/lib/auth0';
import { ApiError, ApiClientError } from '@/lib/api-types';

/**
 * Reads the orchestrator_group_id cookie from document.cookie (client-side).
 */
function getGroupIdFromCookie(): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/(?:^|;\s*)orchestrator_group_id=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

/**
 * Reads the orchestrator_selected_org cookie from document.cookie (client-side).
 */
function getSelectedOrgIdFromCookie(): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/(?:^|;\s*)orchestrator_selected_org=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

/**
 * API client with automatic authentication and error handling.
 * Sends X-Group-Id header from the stored group context cookie.
 */
export class ApiClient {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';
  }

  /**
   * Fetch with automatic authentication and error handling
   */
  async fetch(url: string, options: RequestInit = {}): Promise<Response> {
    const accessToken = await getAccessToken();
    const groupId = getGroupIdFromCookie();
    const selectedOrgId = getSelectedOrgIdFromCookie();
    
    const fullUrl = url.startsWith('http') ? url : `${this.baseUrl}${url}`;
    
    const response = await fetch(fullUrl, {
      ...options,
      headers: {
        ...options.headers,
        'Content-Type': 'application/json',
        ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
        ...(groupId && { 'X-Group-Id': groupId }),
        ...(selectedOrgId && { 'X-Organization-Id': selectedOrgId }),
      },
    });
    
    // Handle 401 - Session expired
    if (response.status === 401) {
      // Token expired and Auth0 SDK couldn't refresh it
      // Redirect to login
      if (typeof window !== 'undefined') {
        window.location.href = '/api/auth/login';
      }
      throw new Error('Session expired');
    }
    
    // Handle error responses
    if (!response.ok) {
      try {
        const error: ApiError = await response.json();
        throw new ApiClientError(error);
      } catch (e) {
        // If response is not JSON or parsing failed
        if (e instanceof ApiClientError) {
          throw e;
        }
        // Create a generic error
        throw new ApiClientError({
          statusCode: response.status,
          message: response.statusText || 'An error occurred',
          exceptionType: 'UnknownError',
          isBusinessException: false,
          isTechnicalException: false,
          timestamp: new Date().toISOString(),
        });
      }
    }
    
    return response;
  }

  /**
   * GET request with automatic authentication
   */
  async get<T>(url: string): Promise<T> {
    const response = await this.fetch(url, { method: 'GET' });
    return response.json();
  }

  /**
   * POST request with automatic authentication
   */
  async post<T>(url: string, body?: any): Promise<T> {
    const response = await this.fetch(url, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });
    return response.json();
  }

  /**
   * PUT request with automatic authentication
   */
  async put<T>(url: string, body?: any): Promise<T> {
    const response = await this.fetch(url, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });
    return response.json();
  }

  /**
   * DELETE request with automatic authentication
   */
  async delete(url: string): Promise<void> {
    await this.fetch(url, { method: 'DELETE' });
  }
}

/**
 * Default API client instance
 */
export const apiClient = new ApiClient();
