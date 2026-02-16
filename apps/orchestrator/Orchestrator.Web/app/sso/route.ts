import { NextRequest, NextResponse } from "next/server";
import { auth0 } from "@/lib/auth0";
import { cookies } from "next/headers";

const GROUP_CONTEXT_COOKIE = "orchestrator_group_id";

/**
 * SSO entry point for cross-app navigation from the ATS.
 *
 * GET /sso?groupId=<uuid>
 *
 * Flow:
 * 1. ATS links here with the user's current group ID in the query string.
 * 2. Since both apps share the same Auth0 tenant and domain, the browser
 *    already has a valid Auth0 session cookie -- no re-authentication prompt.
 * 3. If there's no session (e.g., cookie expired), we redirect to login
 *    which will use the existing Auth0 session and immediately redirect back.
 * 4. We store the groupId in a cookie so the API client can send it as
 *    the X-Group-Id header on every request.
 * 5. Redirect to the Orchestrator home page.
 */
export async function GET(request: NextRequest) {
  const groupId = request.nextUrl.searchParams.get("groupId");

  // Validate groupId
  if (!groupId || !/^[0-9a-f-]{36}$/i.test(groupId)) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  // Check for existing Auth0 session AND a usable access token.
  // The user may have an old Orchestrator session whose refresh token expired
  // while their Auth0 SSO session (from the ATS) is still valid.
  // In that case we force a re-login — Auth0 will silently issue fresh tokens
  // using the SSO session without prompting for credentials.
  const returnTo = `/sso?groupId=${encodeURIComponent(groupId)}`;
  const loginUrl = new URL(`/api/auth/login?returnTo=${encodeURIComponent(returnTo)}`, request.url);

  const session = await auth0.getSession();
  if (!session?.user) {
    return NextResponse.redirect(loginUrl);
  }

  // Verify we can actually get an access token (refresh token still valid)
  try {
    await auth0.getAccessToken();
  } catch {
    // Refresh token is stale — force re-login to get fresh tokens
    return NextResponse.redirect(loginUrl);
  }

  // Store group context in a cookie (Route Handlers can set cookies)
  const cookieStore = await cookies();
  cookieStore.set(GROUP_CONTEXT_COOKIE, groupId, {
    path: "/",
    httpOnly: false, // Readable by client-side JS for X-Group-Id header
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 30, // 30 days
  });

  // Redirect to the app home page
  return NextResponse.redirect(new URL("/", request.url));
}
