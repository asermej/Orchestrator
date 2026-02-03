"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";

export interface InterviewQuestion {
  id?: string;
  questionText: string;
  questionOrder: number;
  isRequired: boolean;
  followUpPrompt?: string;
  maxFollowUps: number;
}

export interface JobTypeItem {
  id: string;
  organizationId: string;
  name: string;
  description?: string;
  defaultAgentId?: string;
  interviewDurationMinutes: number;
  questionCount: number;
  questions: InterviewQuestion[];
  createdAt: string;
  updatedAt?: string;
}

interface PaginatedResponse {
  items: JobTypeItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchJobTypes(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  if (searchTerm && searchTerm.trim() !== "") {
    params.append("Name", searchTerm.trim());
  }

  const data = await apiGet<PaginatedResponse>(`/job-types?${params.toString()}`);
  return data;
}

export async function getJobType(id: string): Promise<JobTypeItem> {
  return await apiGet<JobTypeItem>(`/job-types/${id}`);
}

export async function createJobType(data: {
  organizationId?: string;
  name: string;
  description?: string;
  defaultAgentId?: string;
  interviewDurationMinutes?: number;
  questions?: InterviewQuestion[];
}): Promise<JobTypeItem> {
  return await apiPost<JobTypeItem>("/job-types", data);
}

export async function updateJobType(
  id: string,
  data: {
    name?: string;
    description?: string;
    defaultAgentId?: string;
    interviewDurationMinutes?: number;
    questions?: InterviewQuestion[];
  }
): Promise<JobTypeItem> {
  return await apiPut<JobTypeItem>(`/job-types/${id}`, data);
}

export async function deleteJobType(id: string): Promise<void> {
  await apiDelete(`/job-types/${id}`);
}
