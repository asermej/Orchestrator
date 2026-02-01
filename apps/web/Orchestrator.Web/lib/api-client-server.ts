"use server";

import { getAccessToken } from '@/lib/auth0';
import { ApiError, ApiClientError } from '@/lib/api-types';

// Ensure baseUrl always includes /api/v1
const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const baseUrl = apiUrl.endsWith('/api/v1') ? apiUrl : `${apiUrl}/api/v1`;

/**
 * Internal fetch helper with automatic authentication and error handling
 */
async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const accessToken = await getAccessToken();
  
  // Log token availability for debugging
  if (!accessToken) {
    console.warn(`[API Client] No access token available for ${options.method || 'GET'} ${url}`);
  }
  
  const fullUrl = url.startsWith('http') ? url : `${baseUrl}${url}`;
  
  const response = await fetch(fullUrl, {
    ...options,
    headers: {
      ...options.headers,
      'Content-Type': 'application/json',
      ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
    },
    cache: 'no-store',
  });
  
  // Handle error responses with structured error format
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
export async function apiGet<T>(url: string): Promise<T> {
  const response = await apiFetch(url, { method: 'GET' });
  return response.json();
}

/**
 * POST request with automatic authentication
 * Returns void for 204 No Content responses
 */
export async function apiPost<T>(url: string, body?: any): Promise<T | void> {
  const response = await apiFetch(url, {
    method: 'POST',
    body: body ? JSON.stringify(body) : undefined,
  });
  // Handle 204 No Content responses
  if (response.status === 204) {
    return;
  }
  return response.json();
}

/**
 * PUT request with automatic authentication
 * Returns void for 204 No Content responses
 */
export async function apiPut<T>(url: string, body?: any): Promise<T | void> {
  const response = await apiFetch(url, {
    method: 'PUT',
    body: body ? JSON.stringify(body) : undefined,
  });
  // Handle 204 No Content responses
  if (response.status === 204) {
    return;
  }
  return response.json();
}

/**
 * DELETE request with automatic authentication
 */
export async function apiDelete(url: string): Promise<void> {
  await apiFetch(url, { method: 'DELETE' });
}

/**
 * POST with FormData (for file uploads)
 * Returns void for 204 No Content responses
 */
export async function apiPostFormData<T>(url: string, formData: FormData): Promise<T | void> {
  const accessToken = await getAccessToken();
  
  const fullUrl = url.startsWith('http') ? url : `${baseUrl}${url}`;
  
  const response = await fetch(fullUrl, {
    method: 'POST',
    headers: {
      ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
    },
    body: formData,
    cache: 'no-store',
  });
  
  if (!response.ok) {
    try {
      const error: ApiError = await response.json();
      throw new ApiClientError(error);
    } catch (e) {
      if (e instanceof ApiClientError) {
        throw e;
      }
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
  
  // Handle 204 No Content responses
  if (response.status === 204) {
    return;
  }
  return response.json();
}

