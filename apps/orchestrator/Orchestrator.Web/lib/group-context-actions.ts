"use server";

import { getGroupId } from "@/lib/group-context";

/**
 * Server action that returns the current group ID from the cookie.
 * Callable from client components to get the active group context.
 */
export async function getActiveGroupId(): Promise<string | null> {
  return await getGroupId();
}
