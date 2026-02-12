import { auth0 } from '@/lib/auth0';
import { syncUserFromAuth0 } from '@/lib/sync-user';
import { cache } from 'react';

/**
 * Server component that provisions and syncs users in the local database
 * Uses React cache to prevent redundant syncs during the same request
 * This runs on page loads to:
 * - Ensure Auth0 users exist locally
 * - Sync profile changes from Auth0 (email, name)
 * - Link users via auth0_sub (primary identifier)
 */

// Cache the sync operation for the duration of the request
const getCachedUserSync = cache(async () => {
  try {
    const session = await auth0.getSession();
    
    if (session?.user) {
      const { sub, email, given_name, family_name, name } = session.user;
      
      // Auth0 sub is required - this is the stable identifier
      if (sub && email) {
        // Extract first and last name from Auth0 profile
        const firstName = given_name || name?.split(' ')[0] || 'User';
        const lastName = family_name || name?.split(' ').slice(1).join(' ') || '';
        
        try {
          await syncUserFromAuth0(sub, email, firstName, lastName);
          // User synced successfully - no logging needed
        } catch (error: any) {
          // Log only unexpected errors (not 404s during user lookup)
          if (error?.error?.statusCode && error.error.statusCode !== 404) {
            console.error('[UserProvisioner] Failed to sync user:', {
              email,
              statusCode: error.error.statusCode,
              message: error.message
            });
          }
          // Note: User sync will be retried on next page load
        }
      }
    }
  } catch (error) {
    // Silently fail if session check fails
    // Only log if it's an actual error, not just "no session"
    if (error instanceof Error && error.message !== 'No session') {
      console.error('[UserProvisioner] Session check failed:', error);
    }
  }
});

export async function UserProvisioner() {
  // Use cached version to prevent multiple syncs in the same request
  await getCachedUserSync();
  
  // This component renders nothing
  return null;
}

