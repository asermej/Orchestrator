"use server";

import { apiGet } from "@/lib/api-client-server";

export interface JobItem {
  id: string;
  organizationId: string;
  externalJobId: string;
  title: string;
  description?: string | null;
  status: string;
  location?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface PaginatedResponse {
  items: JobItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchJobs(
  pageNumber: number = 1,
  pageSize: number = 20,
  title?: string,
  status?: string
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });
  if (title) params.append("Title", title);
  if (status) params.append("Status", status);

  const data = await apiGet<PaginatedResponse>(`/Job?${params.toString()}`);
  return data;
}
