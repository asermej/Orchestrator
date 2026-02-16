import { Auth0Client } from '@auth0/nextjs-auth0/server';

let _auth0Client: Auth0Client | null = null;

function getAuth0Client(): Auth0Client {
  if (!_auth0Client) {
    _auth0Client = new Auth0Client({
      secret: process.env.AUTH0_SECRET!,
      appBaseUrl: process.env.APP_BASE_URL!,
      domain: process.env.AUTH0_DOMAIN!,
      clientId: process.env.AUTH0_CLIENT_ID!,
      clientSecret: process.env.AUTH0_CLIENT_SECRET!,
      authorizationParameters: {
        audience: process.env.AUTH0_AUDIENCE || 'https://api.surrova.com', // Required for API access tokens
        scope: 'openid profile email offline_access', // offline_access is needed for API tokens and refresh tokens
      },
      session: {
        rolling: true, // Extend session on activity
        rollingDuration: 60 * 60 * 24, // 24 hours - session extends by this amount on each request
        cookie: {
          name: '__orchestrator_session', // Unique cookie name to avoid collisions with other apps on localhost
        },
      },
      routes: {
        login: '/api/auth/login',
        logout: '/api/auth/logout',
        callback: '/api/auth/callback',
      },
    });
  }
  return _auth0Client;
}

export const auth0 = {
  get middleware() {
    return getAuth0Client().middleware.bind(getAuth0Client());
  },
  get getSession() {
    return getAuth0Client().getSession.bind(getAuth0Client());
  },
  get getAccessToken() {
    return getAuth0Client().getAccessToken.bind(getAuth0Client());
  },
  get startInteractiveLogin() {
    return getAuth0Client().startInteractiveLogin.bind(getAuth0Client());
  },
  get updateSession() {
    return getAuth0Client().updateSession.bind(getAuth0Client());
  },
  get withPageAuthRequired() {
    return getAuth0Client().withPageAuthRequired.bind(getAuth0Client());
  },
  get withApiAuthRequired() {
    return getAuth0Client().withApiAuthRequired.bind(getAuth0Client());
  },
};

/**
 * Helper function to get the access token from the Auth0 SDK
 * Uses the official getAccessToken() method which handles token refresh automatically
 * Returns null if no session exists or token has expired (allows anonymous access)
 */
export async function getAccessToken(): Promise<string | null> {
  try {
    const session = await auth0.getSession();
    
    if (!session) {
      console.warn('[Auth0] No session found when getting access token');
      return null;
    }
    
    const tokenData = await auth0.getAccessToken();
    return tokenData.token;
  } catch (error: any) {
    // Token expired or refresh token not available - user needs to re-authenticate
    // This is expected behavior for expired sessions, so we return null to allow anonymous access
    if (error?.code === 'missing_refresh_token' || error?.message?.includes('expired')) {
      console.warn('[Auth0] Token expired or refresh token missing:', error.code || error.message);
      return null;
    }
    
    // Log other unexpected errors
    console.error('[Auth0] Error getting access token:', error);
    return null;
  }
}

