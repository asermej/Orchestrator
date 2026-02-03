import { useState, useCallback } from 'react';
import { toast } from 'sonner';

/**
 * Generic state for server action execution
 */
export interface ServerActionState<T = any> {
  data: T | null;
  error: string | null;
  isLoading: boolean;
  isSuccess: boolean;
  isError: boolean;
}

/**
 * Options for the useServerAction hook
 */
export interface UseServerActionOptions<TResult = any> {
  /**
   * Success message to show in a toast (can be a string or function that receives the result)
   */
  successMessage?: string | ((result: TResult) => string);
  
  /**
   * Whether to show error toasts automatically (default: true)
   */
  showErrorToast?: boolean;
  
  /**
   * Callback to run on success (receives the result)
   */
  onSuccess?: (result: TResult) => void;
  
  /**
   * Callback to run on error
   */
  onError?: (error: string) => void;
}

/**
 * Hook for managing server action state and error handling
 * 
 * @example
 * ```tsx
 * const { execute, isLoading, error } = useServerAction(createAgent, {
 *   successMessage: 'Agent created successfully!',
 *   onSuccess: () => router.push('/agents')
 * });
 * 
 * const handleSubmit = async (formData: FormData) => {
 *   await execute(formData);
 * };
 * ```
 */
export function useServerAction<TArgs extends any[], TResult>(
  action: (...args: TArgs) => Promise<TResult>,
  options: UseServerActionOptions<TResult> = {}
) {
  const [state, setState] = useState<ServerActionState<TResult>>({
    data: null,
    error: null,
    isLoading: false,
    isSuccess: false,
    isError: false,
  });

  const execute = useCallback(
    async (...args: TArgs): Promise<TResult | null> => {
      setState({
        data: null,
        error: null,
        isLoading: true,
        isSuccess: false,
        isError: false,
      });

      try {
        const result = await action(...args);
        
        setState({
          data: result,
          error: null,
          isLoading: false,
          isSuccess: true,
          isError: false,
        });

        // Show success message if provided
        if (options.successMessage) {
          const message = typeof options.successMessage === 'function' 
            ? options.successMessage(result)
            : options.successMessage;
          toast.success(message);
        }

        // Call success callback if provided
        if (options.onSuccess) {
          options.onSuccess(result);
        }

        return result;
      } catch (err) {
        // Re-throw Next.js redirect errors to allow navigation to work
        if (err instanceof Error && err.message.includes('NEXT_REDIRECT')) {
          throw err;
        }
        
        const errorMessage = err instanceof Error ? err.message : 'An error occurred';
        
        setState({
          data: null,
          error: errorMessage,
          isLoading: false,
          isSuccess: false,
          isError: true,
        });

        // Show error toast by default
        if (options.showErrorToast !== false) {
          toast.error(errorMessage, {
            description: 'Please try again or contact support if the problem persists.',
            duration: 5000,
          });
        }

        // Call error callback if provided
        if (options.onError) {
          options.onError(errorMessage);
        }

        return null;
      }
    },
    [action, options]
  );

  const reset = useCallback(() => {
    setState({
      data: null,
      error: null,
      isLoading: false,
      isSuccess: false,
      isError: false,
    });
  }, []);

  return {
    execute,
    reset,
    ...state,
  };
}

