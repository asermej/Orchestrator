"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";
import { getGroupId } from "@/lib/group-context";

export interface InterviewTemplateItem {
  id: string;
  groupId: string;
  organizationId?: string | null;
  name: string;
  description?: string | null;
  isActive: boolean;
  roleTemplateId?: string | null;
  agentId?: string | null;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
  agent?: { id: string; displayName: string; profileImageUrl?: string | null } | null;
  createdAt: string;
  updatedAt?: string | null;
  createdBy?: string | null;
  updatedBy?: string | null;
}

interface PaginatedResponse {
  items: InterviewTemplateItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchInterviewTemplates(
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

  const data = await apiGet<PaginatedResponse>(`/InterviewTemplate?${params.toString()}`);
  return data;
}

export async function fetchInterviewTemplateById(id: string): Promise<InterviewTemplateItem> {
  const data = await apiGet<InterviewTemplateItem>(`/InterviewTemplate/${id}`);
  return data;
}

export async function createInterviewTemplate(data: {
  groupId?: string;
  organizationId?: string | null;
  name: string;
  description?: string | null;
  isActive?: boolean;
  roleTemplateId?: string | null;
  agentId?: string | null;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
  createdBy?: string;
}): Promise<InterviewTemplateItem> {
  const groupId = data.groupId || (await getGroupId()) || "";
  const created = await apiPost<InterviewTemplateItem>("/InterviewTemplate", {
    ...data,
    groupId,
  });
  return created as InterviewTemplateItem;
}

export async function updateInterviewTemplate(
  id: string,
  data: {
    name?: string;
    description?: string | null;
    isActive?: boolean;
    roleTemplateId?: string | null;
    agentId?: string | null;
    openingTemplate?: string | null;
    closingTemplate?: string | null;
    updatedBy?: string;
  }
): Promise<InterviewTemplateItem> {
  const updated = await apiPut<InterviewTemplateItem>(`/InterviewTemplate/${id}`, data);
  return updated as InterviewTemplateItem;
}

export async function deleteInterviewTemplate(id: string): Promise<void> {
  await apiDelete(`/InterviewTemplate/${id}`);
}

export async function fetchAgentsForTemplate(): Promise<
  { id: string; displayName: string; profileImageUrl?: string | null }[]
> {
  const data = await apiGet<{ items: { id: string; displayName: string; profileImageUrl?: string | null }[] }>(
    "/Agent?PageSize=100"
  );
  return data.items || [];
}

export async function fetchRolesForTemplate(): Promise<
  { id: string; roleKey: string; roleName: string; industry?: string | null }[]
> {
  return await apiGet<
    { id: string; roleKey: string; roleName: string; industry?: string | null }[]
  >("/QuestionPackageLibrary");
}
