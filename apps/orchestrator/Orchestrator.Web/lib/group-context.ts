import { cookies } from "next/headers";

const GROUP_CONTEXT_COOKIE = "orchestrator_group_id";
const RETURN_URL_COOKIE = "orchestrator_return_url";
const SELECTED_ORG_COOKIE = "orchestrator_selected_org";

/**
 * Gets the current group context from the cookie (server-side).
 */
export async function getGroupId(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get(GROUP_CONTEXT_COOKIE)?.value ?? null;
}

/**
 * Sets the group context cookie (server-side).
 */
export async function setGroupId(groupId: string): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.set(GROUP_CONTEXT_COOKIE, groupId, {
    path: "/",
    httpOnly: false, // Readable by client-side JS for X-Group-Id header
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 30, // 30 days
  });
}

/**
 * Clears the group context cookie (server-side).
 */
export async function clearGroupId(): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.delete(GROUP_CONTEXT_COOKIE);
}

/**
 * Gets the ATS return URL from the cookie (server-side).
 * This is set during the SSO flow so the Orchestrator can show a
 * "Back to Hireology" link that returns the user to where they were in the ATS.
 */
export async function getReturnUrl(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get(RETURN_URL_COOKIE)?.value ?? null;
}

/**
 * Sets the ATS return URL cookie (server-side).
 */
export async function setReturnUrl(url: string): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.set(RETURN_URL_COOKIE, url, {
    path: "/",
    httpOnly: false,
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 30, // 30 days
  });
}

/**
 * Clears the ATS return URL cookie (server-side).
 */
export async function clearReturnUrl(): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.delete(RETURN_URL_COOKIE);
}

/**
 * Gets the selected organization ID from the cookie (server-side).
 * When set, the Orchestrator header shows this org as selected and
 * the API client sends it as the X-Organization-Id header.
 */
export async function getSelectedOrgId(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get(SELECTED_ORG_COOKIE)?.value ?? null;
}

/**
 * Sets the selected organization ID cookie (server-side).
 */
export async function setSelectedOrgId(orgId: string): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.set(SELECTED_ORG_COOKIE, orgId, {
    path: "/",
    httpOnly: false, // Readable by client-side JS for X-Organization-Id header
    sameSite: "lax",
    maxAge: 60 * 60 * 24 * 30, // 30 days
  });
}

/**
 * Clears the selected organization ID cookie (server-side).
 */
export async function clearSelectedOrgId(): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.delete(SELECTED_ORG_COOKIE);
}
