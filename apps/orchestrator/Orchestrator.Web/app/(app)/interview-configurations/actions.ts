"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";
import { getGroupId } from "@/lib/group-context";

export interface AgentItem {
  id: string;
  groupId: string;
  organizationId?: string | null;
  visibilityScope?: string;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface InterviewGuideQuestion {
  id: string;
  interviewGuideId: string;
  question: string;
  displayOrder: number;
  scoringWeight: number;
  scoringGuidance?: string | null;
  followUpsEnabled: boolean;
  maxFollowUps: number;
}

export interface InterviewGuideItem {
  id: string;
  groupId: string;
  organizationId?: string | null;
  visibilityScope?: string;
  name: string;
  description?: string | null;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
  scoringRubric?: string | null;
  isActive: boolean;
  questionCount: number;
  questions?: InterviewGuideQuestion[];
}

export interface InterviewConfigurationItem {
  id: string;
  groupId: string;
  organizationId?: string | null;
  interviewGuideId: string;
  agentId: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  interviewGuide?: InterviewGuideItem | null;
  agent?: AgentItem | null;
  createdAt: string;
  updatedAt?: string | null;
  createdBy?: string | null;
  updatedBy?: string | null;
}

interface PaginatedResponse {
  items: InterviewConfigurationItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Fetch interview configurations with pagination and filters
 */
export async function fetchInterviewConfigurations(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string,
  agentId?: string,
  isActive?: boolean
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  if (searchTerm && searchTerm.trim() !== "") {
    params.append("Name", searchTerm.trim());
  }

  if (agentId) {
    params.append("AgentId", agentId);
  }

  if (isActive !== undefined) {
    params.append("IsActive", isActive.toString());
  }

  const data = await apiGet<PaginatedResponse>(`/InterviewConfiguration?${params.toString()}`);
  return data;
}

/**
 * Fetch a single interview configuration by ID
 */
export async function fetchInterviewConfigurationById(id: string): Promise<InterviewConfigurationItem> {
  const data = await apiGet<InterviewConfigurationItem>(`/InterviewConfiguration/${id}`);
  return data;
}

/**
 * Create a new interview configuration.
 * groupId is automatically resolved from the group context cookie.
 */
export async function createInterviewConfiguration(data: {
  groupId?: string;
  organizationId?: string | null;
  interviewGuideId: string;
  agentId: string;
  name: string;
  description?: string | null;
  isActive?: boolean;
  createdBy?: string;
}): Promise<InterviewConfigurationItem> {
  const groupId = data.groupId || (await getGroupId()) || "";
  const createdConfig = await apiPost<InterviewConfigurationItem>("/InterviewConfiguration", {
    ...data,
    groupId,
  });
  return createdConfig as InterviewConfigurationItem;
}

/**
 * Update an interview configuration
 */
export async function updateInterviewConfiguration(
  id: string,
  data: {
    interviewGuideId?: string;
    agentId?: string;
    name?: string;
    description?: string | null;
    isActive?: boolean;
    updatedBy?: string;
  }
): Promise<InterviewConfigurationItem> {
  const updatedConfig = await apiPut<InterviewConfigurationItem>(`/InterviewConfiguration/${id}`, data);
  return updatedConfig as InterviewConfigurationItem;
}

/**
 * Delete an interview configuration
 */
export async function deleteInterviewConfiguration(id: string): Promise<void> {
  await apiDelete(`/InterviewConfiguration/${id}`);
}

/**
 * Fetch available agents for the configuration dropdown
 */
export async function fetchAgentsForConfiguration(): Promise<AgentItem[]> {
  const data = await apiGet<{ items: AgentItem[] }>("/Agent?PageSize=100");
  return data.items || [];
}

/**
 * Fetch available interview guides for the configuration dropdown
 */
export async function fetchGuidesForConfiguration(): Promise<InterviewGuideItem[]> {
  const data = await apiGet<{ items: InterviewGuideItem[] }>("/InterviewGuide?PageSize=100&IsActive=true");
  return data.items || [];
}
