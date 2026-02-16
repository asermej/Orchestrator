"use server";

import { apiGet, apiPut, apiDelete, apiPost } from "@/lib/api-client-server";

export interface AgentItem {
  id: string;
  groupId: string;
  organizationId?: string | null;
  visibilityScope?: string;
  displayName: string;
  profileImageUrl?: string | null;
  systemPrompt?: string | null;
  interviewGuidelines?: string | null;
  elevenlabsVoiceId?: string | null;
  voiceStability?: number;
  voiceSimilarityBoost?: number;
  voiceProvider?: string | null;
  voiceType?: string | null;
  voiceName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  isInherited?: boolean;
  ownerOrganizationName?: string | null;
}

interface PaginatedResponse {
  items: AgentItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Fetch agents with optional source filtering (local/inherited).
 */
export async function fetchMyAgents(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string,
  source?: "local" | "inherited"
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  if (searchTerm && searchTerm.trim() !== "") {
    params.append("DisplayName", searchTerm.trim());
  }

  if (source) {
    params.append("Source", source);
  }

  const data = await apiGet<PaginatedResponse>(`/Agent?${params.toString()}`);
  return data;
}

/**
 * Fetch a single agent by ID
 */
export async function fetchAgentById(id: string): Promise<AgentItem> {
  const data = await apiGet<AgentItem>(`/Agent/${id}`);
  return data;
}

/**
 * Update an agent
 */
export async function updateAgent(
  id: string,
  data: {
    displayName?: string;
    profileImageUrl?: string | null;
    systemPrompt?: string | null;
    interviewGuidelines?: string | null;
    visibilityScope?: string;
  }
): Promise<AgentItem> {
  const updatedAgent = await apiPut<AgentItem>(`/Agent/${id}`, data);
  return updatedAgent as AgentItem;
}

/**
 * Delete an agent
 */
export async function deleteAgent(id: string): Promise<void> {
  await apiDelete(`/Agent/${id}`);
}

/**
 * Clone an inherited agent into the currently selected organization.
 */
export async function cloneAgent(id: string): Promise<AgentItem> {
  const clonedAgent = await apiPost<AgentItem>(`/Agent/${id}/clone`);
  return clonedAgent as AgentItem;
}
