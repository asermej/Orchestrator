import { Auth0Client } from "@auth0/nextjs-auth0/server";

let _auth0Client: Auth0Client | null = null;

/** Auth0 requires a secret of at least 32 chars for cookie encryption. Ensure it's always a string. */
function getSecret(): string {
  const raw = process.env.AUTH0_SECRET;
  if (typeof raw === "string" && raw.length >= 32) return raw;
  if (process.env.NODE_ENV === "development") {
    console.warn(
      "[Auth0] AUTH0_SECRET is missing or too short. Using a dev-only default. Set AUTH0_SECRET in .env.local (min 32 chars) for real login."
    );
    return "dev-only-secret-min-32-chars-for-test-ats";
  }
  throw new Error("AUTH0_SECRET must be set in .env.local and be at least 32 characters.");
}

function getAuth0Client(): Auth0Client {
  if (!_auth0Client) {
    _auth0Client = new Auth0Client({
      secret: getSecret(),
      appBaseUrl: process.env.APP_BASE_URL ?? "http://localhost:3001",
      domain: process.env.AUTH0_DOMAIN!,
      clientId: process.env.AUTH0_CLIENT_ID!,
      clientSecret: process.env.AUTH0_CLIENT_SECRET!,
      authorizationParameters: {
        audience: process.env.AUTH0_AUDIENCE ?? "https://test-ats-api",
        scope: "openid profile email offline_access",
      },
      session: {
        rolling: true,
        cookie: {
          name: '__test_ats_session', // Unique cookie name to avoid collisions with other apps on localhost
        },
      },
      routes: {
        login: "/api/auth/login",
        logout: "/api/auth/logout",
        callback: "/api/auth/callback",
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
  get withPageAuthRequired() {
    return getAuth0Client().withPageAuthRequired.bind(getAuth0Client());
  },
};

export async function getAccessToken(): Promise<string | null> {
  try {
    const session = await auth0.getSession();
    if (!session) return null;
    const tokenData = await auth0.getAccessToken();
    return tokenData.token;
  } catch {
    return null;
  }
}
