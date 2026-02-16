import { cookies } from "next/headers";

const GROUP_CONTEXT_COOKIE = "orchestrator_group_id";

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
