"use server";

import { apiGet, apiPut, apiDelete } from "@/lib/api-client-server";

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
  voiceCreatedAt?: string | null;
  voiceCreatedByUserId?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface PaginatedResponse {
  items: AgentItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Fetch agents created by the current user
 */
export async function fetchMyAgents(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string
): Promise<PaginatedResponse> {
  // Build query parameters
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
    CreatedByMe: "true", // Filter by current user
  });

  // Add search filters if provided
  if (searchTerm && searchTerm.trim() !== "") {
    params.append("DisplayName", searchTerm.trim());
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
  }
): Promise<AgentItem> {
  const updatedAgent = await apiPut<AgentItem>(`/Agent/${id}`, data);
  return updatedAgent;
}

/**
 * Delete an agent
 */
export async function deleteAgent(id: string): Promise<void> {
  await apiDelete(`/Agent/${id}`);
}

// Aliases for backward compatibility
export type PersonaItem = AgentItem;
export { fetchMyAgents as fetchMyPersonas };
export { fetchAgentById as fetchPersonaById };
export { updateAgent as updatePersona };
export { deleteAgent as deletePersona };