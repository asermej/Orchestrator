"use server";

import { apiGet, apiPost } from '@/lib/api-client-server';
import { ApiClientError } from '@/lib/api-types';

interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  auth0Sub: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Syncs a user from Auth0 to the local database
 * Creates the user if they don't exist, or ensures data is up-to-date if they do
 * 
 * @param auth0Sub - The Auth0 'sub' claim (e.g., "google-oauth2|123456")
 * @param email - User's email from Auth0
 * @param firstName - User's first name from Auth0
 * @param lastName - User's last name from Auth0 (optional)
 */
export async function syncUserFromAuth0(
  auth0Sub: string,
  email: string,
  firstName: string,
  lastName: string
): Promise<void> {
  try {
    // Try to get existing user by Auth0 sub
    const existingUser = await apiGet<User>(`/User/by-auth0-sub/${encodeURIComponent(auth0Sub)}`);
    
    // User exists - sync is complete
    // Note: We could add update logic here if we want to sync profile changes
    // For now, we just ensure the user exists
    return;
  } catch (error: unknown) {
    // If user doesn't exist (404), create them
    if (error instanceof ApiClientError && error.error.statusCode === 404) {
      try {
        await apiPost('/User', {
          firstName,
          lastName: lastName || '',
          email,
          auth0Sub,
        });
        return;
      } catch (createError: unknown) {
        // If user was created by another request (race condition), that's ok
        if (createError instanceof ApiClientError && 
            createError.error.statusCode === 400 && 
            createError.error.message.includes('already exists')) {
          return;
        }
        // Re-throw other creation errors
        throw createError;
      }
    }
    
    // For other errors, re-throw
    throw error;
  }
}

