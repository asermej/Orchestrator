/**
 * Application configuration
 * These values are used throughout the application
 */

// API Base URL - defaults to localhost:5000 in development
// Note: Hardcoded for development. In production, set NEXT_PUBLIC_API_URL environment variable.
export const API_BASE_URL = 'http://localhost:5000'

/**
 * Get the full URL for an image
 * @param relativePath - The relative path to the image (e.g., "/uploads/personas/abc.png")
 * @returns The full URL to the image
 */
export function getImageUrl(relativePath: string | null | undefined): string | null {
  if (!relativePath) return null
  
  // If it's already a full URL, return it
  if (relativePath.startsWith('http://') || relativePath.startsWith('https://')) {
    return relativePath
  }
  
  // Otherwise, prepend the API base URL
  return `${API_BASE_URL}${relativePath}`
}

