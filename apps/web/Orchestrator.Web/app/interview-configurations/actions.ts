"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";

export interface InterviewConfigurationQuestion {
  id: string;
  interviewConfigurationId: string;
  question: string;
  displayOrder: number;
  scoringWeight: number;
  scoringGuidance?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface AgentItem {
  id: string;
  organizationId: string;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface InterviewConfigurationItem {
  id: string;
  organizationId: string;
  agentId: string;
  name: string;
  description?: string | null;
  scoringRubric?: string | null;
  isActive: boolean;
  questions: InterviewConfigurationQuestion[];
  questionCount: number;
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
  const data = await apiGet<InterviewConfigurationItem>(`/InterviewConfiguration/${id}?includeQuestions=true`);
  return data;
}

/**
 * Create a new interview configuration
 */
export async function createInterviewConfiguration(data: {
  organizationId: string;
  agentId: string;
  name: string;
  description?: string | null;
  scoringRubric?: string | null;
  isActive?: boolean;
  questions?: Array<{
    question: string;
    displayOrder: number;
    scoringWeight: number;
    scoringGuidance?: string | null;
  }>;
  createdBy?: string;
}): Promise<InterviewConfigurationItem> {
  const createdConfig = await apiPost<InterviewConfigurationItem>("/InterviewConfiguration", data);
  return createdConfig;
}

/**
 * Update an interview configuration
 */
export async function updateInterviewConfiguration(
  id: string,
  data: {
    name?: string;
    description?: string | null;
    scoringRubric?: string | null;
    isActive?: boolean;
    questions?: Array<{
      question: string;
      displayOrder: number;
      scoringWeight: number;
      scoringGuidance?: string | null;
    }>;
    updatedBy?: string;
  }
): Promise<InterviewConfigurationItem> {
  const updatedConfig = await apiPut<InterviewConfigurationItem>(`/InterviewConfiguration/${id}`, data);
  return updatedConfig;
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
