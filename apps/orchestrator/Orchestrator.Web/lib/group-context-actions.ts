"use server";

import { revalidatePath } from "next/cache";
import { getGroupId, setSelectedOrgId, clearSelectedOrgId } from "@/lib/group-context";

/**
 * Server action that returns the current group ID from the cookie.
 * Callable from client components to get the active group context.
 */
export async function getActiveGroupId(): Promise<string | null> {
  return await getGroupId();
}

/**
 * Server action to select an organization. Sets the cookie and revalidates
 * the layout so the header re-renders with the new selection.
 */
export async function selectOrganization(orgId: string): Promise<void> {
  await setSelectedOrgId(orgId);
  revalidatePath("/", "layout");
}

/**
 * Server action to clear the selected organization (show all orgs).
 */
export async function clearOrganization(): Promise<void> {
  await clearSelectedOrgId();
  revalidatePath("/", "layout");
}
