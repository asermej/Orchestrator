"use server";

import { apiPost } from "@/lib/api-client-server";
import { getGroupId, getSelectedOrgId } from "@/lib/group-context";
import { redirect } from "next/navigation";

interface CreateAgentRequest {
  groupId?: string;
  organizationId?: string | null;
  visibilityScope?: string;
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
  const visibilityScope = formData.get("visibilityScope") as string;

  if (!displayName || displayName.trim() === "") {
    throw new Error("Display name is required");
  }

  const groupId = await getGroupId();
  const selectedOrgId = await getSelectedOrgId();

  if (!selectedOrgId) {
    throw new Error("Please select an organization before creating an agent.");
  }

  const agent: CreateAgentRequest = {
    ...(groupId && { groupId }),
    organizationId: selectedOrgId,
    visibilityScope: visibilityScope || "organization_only",
    displayName: displayName.trim(),
    profileImageUrl: profileImageUrl && profileImageUrl.trim() !== "" ? profileImageUrl : null,
  };

  const createdAgent = await apiPost<AgentResponse>("/Agent", agent);
  
  redirect(`/my-agents/${(createdAgent as AgentResponse).id}/edit`);
}
