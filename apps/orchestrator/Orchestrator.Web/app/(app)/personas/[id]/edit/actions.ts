"use server";

import { apiGet, apiPut } from "@/lib/api-client-server";
import { AgentItem } from "@/app/(app)/my-personas/actions";

export async function fetchAgentById(id: string): Promise<AgentItem> {
  const data = await apiGet<AgentItem>(`/Agent/${id}`);
  return data;
}

export async function updateAgent(id: string, formData: FormData) {
  const displayName = formData.get("displayName") as string;
  const profileImageUrl = formData.get("profileImageUrl") as string;

  const payload = {
    displayName: displayName || undefined,
    profileImageUrl: profileImageUrl || null,
  };

  return await apiPut(`/Agent/${id}`, payload);
}
