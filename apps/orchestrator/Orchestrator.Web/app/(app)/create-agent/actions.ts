"use server";

import { apiPost } from "@/lib/api-client-server";
import { getGroupId } from "@/lib/group-context";
import { redirect } from "next/navigation";

interface CreateAgentRequest {
  groupId?: string;
  organizationId?: string | null;
  displayName: string;
  profileImageUrl?: string | null;
}

interface AgentResponse {
  id: string;
  displayName: string;
  profileImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export async function createAgent(formData: FormData) {
  const displayName = formData.get("displayName") as string;
  const profileImageUrl = formData.get("profileImageUrl") as string;
  const organizationId = formData.get("organizationId") as string;

  if (!displayName || displayName.trim() === "") {
    throw new Error("Display name is required");
  }

  const groupId = await getGroupId();

  const agent: CreateAgentRequest = {
    ...(groupId && { groupId }),
    ...(organizationId && { organizationId }),
    displayName: displayName.trim(),
    profileImageUrl: profileImageUrl && profileImageUrl.trim() !== "" ? profileImageUrl : null,
  };

  const createdAgent = await apiPost<AgentResponse>("/Agent", agent);
  
  redirect(`/my-agents/${createdAgent.id}/edit`);
}
