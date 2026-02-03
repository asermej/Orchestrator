"use server";

import { apiGet, apiDelete } from "@/lib/api-client-server";

export interface InterviewResponse {
  id: string;
  questionId: string;
  questionText: string;
  transcript: string;
  audioUrl?: string;
  responseOrder: number;
  createdAt: string;
}

export interface InterviewResult {
  id: string;
  interviewId: string;
  summary?: string;
  strengths?: string;
  weaknesses?: string;
  overallScore?: number;
  recommendation?: string;
  aiAnalysis?: string;
  createdAt: string;
}

export interface InterviewItem {
  id: string;
  token: string;
  status: string;
  scheduledAt?: string;
  startedAt?: string;
  completedAt?: string;
  job?: {
    id: string;
    title: string;
    externalJobId?: string;
  };
  applicant?: {
    id: string;
    firstName?: string;
    lastName?: string;
    email?: string;
    phone?: string;
  };
  agent?: {
    id: string;
    displayName: string;
    profileImageUrl?: string;
  };
  responses: InterviewResponse[];
  result?: InterviewResult;
  createdAt: string;
  updatedAt?: string;
}

interface PaginatedResponse {
  items: InterviewItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchInterviews(
  pageNumber: number = 1,
  pageSize: number = 20,
  status?: string
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  if (status && status !== "all") {
    params.append("Status", status);
  }

  const data = await apiGet<PaginatedResponse>(`/Interview?${params.toString()}`);
  return data;
}

export async function getInterview(id: string): Promise<InterviewItem> {
  return await apiGet<InterviewItem>(`/Interview/${id}`);
}

export async function deleteInterview(id: string): Promise<void> {
  await apiDelete(`/Interview/${id}`);
}
