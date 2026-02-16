import { NextRequest, NextResponse } from "next/server";
import { auth0 } from "@/lib/auth0";
import { cookies } from "next/headers";
import { setSelectedOrgId } from "@/lib/group-context";

const GROUP_CONTEXT_COOKIE = "orchestrator_group_id";
const RETURN_URL_COOKIE = "orchestrator_return_url";

const UUID_REGEX = /^[0-9a-f-]{36}$/i;

/**
 * SSO entry point for cross-app navigation from the ATS.
 *
 * GET /sso?groupId=<uuid>&returnUrl=<url>&organizationId=<uuid>
 *
 * Flow:
 * 1. ATS links here with the user's current group ID in the query string,
 *    optional returnUrl, and optional organizationId (current location selection).
 * 2. On first arrival (no "fresh" flag), we always redirect through Auth0's
 *    login flow so the Orchestrator session is rebuilt from the current Auth0
 *    SSO cookie. This prevents a stale session from a previous user from
 *    being reused. The login redirect includes a "fresh=1" flag in returnTo.
 * 3. Auth0 sees the shared SSO cookie and silently issues new tokens (no
 *    login prompt). The callback creates a fresh Orchestrator session.
 * 4. On the return visit (with "fresh=1"), we verify the session is valid,
 *    store the groupId cookie, returnUrl cookie, and organizationId cookie,
 *    then redirect to the Orchestrator home page.
 */
export async function GET(request: NextRequest) {
  const groupId = request.nextUrl.searchParams.get("groupId");
  const returnUrl = request.nextUrl.searchParams.get("returnUrl");
  const organizationId = request.nextUrl.searchParams.get("organizationId");
  const fresh = request.nextUrl.searchParams.get("fresh");

  // Validate groupId
  if (!groupId || !UUID_REGEX.test(groupId)) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  // Build the returnTo URL with a "fresh" flag so we know we've already
  // been through the login flow and can skip the forced re-auth.
  const returnToParams = new URLSearchParams({ groupId, fresh: "1" });
  if (returnUrl) returnToParams.set("returnUrl", returnUrl);
  if (organizationId && UUID_REGEX.test(organizationId)) returnToParams.set("organizationId", organizationId);
  const returnTo = `/sso?${returnToParams.toString()}`;
  const loginUrl = new URL(`/api/auth/login?returnTo=${encodeURIComponent(returnTo)}`, request.url);

  // When arriving from the ATS (no "fresh" flag), always force re-authentication
  // through Auth0's SSO flow. This ensures the Orchestrator session is rebuilt
  // from the current Auth0 SSO cookie, which reflects the ATS user — not a
  // stale session from a previous login by a different user.
  if (!fresh) {
    return NextResponse.redirect(loginUrl);
  }

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

  // Store return URL so the Orchestrator sidebar can show "Back to Hireology".
  // Validate that it's a real HTTP(S) URL to prevent open-redirect abuse.
  const atsBaseUrl = process.env.NEXT_PUBLIC_ATS_URL || "http://localhost:3001";
  let safeReturnUrl = atsBaseUrl;
  if (returnUrl) {
    try {
      const parsed = new URL(returnUrl);
      if (parsed.protocol === "http:" || parsed.protocol === "https:") {
        safeReturnUrl = returnUrl;
      }
    } catch {
      // Invalid URL — fall back to ATS base URL
    }
  }
  cookieStore.set(RETURN_URL_COOKIE, safeReturnUrl, {
    path: "/",
    httpOnly: false,
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 30, // 30 days
  });

  // If ATS passed the current location selection, set the selected-org cookie so
  // the same organization is selected in Orchestrator.
  if (organizationId && UUID_REGEX.test(organizationId)) {
    await setSelectedOrgId(organizationId);
  }

  // Redirect to the default app page (My Agents)
  return NextResponse.redirect(new URL("/my-agents", request.url));
}
