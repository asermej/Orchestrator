import { toast } from 'sonner';
import { ApiClientError } from './api-types';

/**
 * Custom error handler function type
 * Return true if the error was handled, false to use default handling
 */
export type CustomErrorHandler = (error: ApiClientError) => boolean;

/**
 * Centralized error handling for API errors
 * Displays appropriate toast notifications based on exception type
 * 
 * @param error - The error to handle (typically from API calls)
 * @param customHandler - Optional custom handler for specific error cases
 */
export function handleApiError(error: unknown, customHandler?: CustomErrorHandler) {
  if (error instanceof ApiClientError) {
    // Allow component to override handling
    if (customHandler && customHandler(error)) {
      return;
    }
    
    // Centralized handling based on exception type
    if (error.error.isBusinessException) {
      // Business exceptions are user-facing (validation, not found, etc.)
      // Show the actual message from the API
      toast.error(error.error.message, {
        description: 'Please check your input and try again.',
        duration: 5000,
      });
    } else if (error.error.isTechnicalException) {
      // Technical exceptions are internal errors
      // Show generic message (don't expose internals to users)
      toast.error('Something went wrong', {
        description: 'We\'re working on fixing this. Please try again later.',
        duration: 5000,
      });
      
      // Log to console for debugging
      console.error('Technical error:', error.error);
    } else {
      // Unknown error type
      toast.error('An unexpected error occurred', {
        description: error.error.message,
        duration: 5000,
      });
      
      console.error('Unknown error type:', error.error);
    }
  } else {
    // Non-API error (network issue, etc.)
    toast.error('Connection error', {
      description: 'Please check your internet connection and try again.',
      duration: 5000,
    });
    
    console.error('Non-API error:', error);
  }
}

/**
 * Success toast helper for consistent success messages
 */
export function showSuccess(message: string, description?: string) {
  toast.success(message, {
    description,
    duration: 4000,
  });
}

/**
 * Warning toast helper for non-error important messages
 */
export function showWarning(message: string, description?: string) {
  toast.warning(message, {
    description,
    duration: 5000,
  });
}

/**
 * Info toast helper for informational messages
 */
export function showInfo(message: string, description?: string) {
  toast.info(message, {
    description,
    duration: 4000,
  });
}

