"use server";

import { apiGet } from "@/lib/api-client-server";

export interface AgentItem {
  id: string;
  displayName: string;
  profileImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface PaginatedResponse {
  items: AgentItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchAgents(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string
): Promise<PaginatedResponse> {
  // Build query parameters
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  // Add search filters if provided
  if (searchTerm && searchTerm.trim() !== "") {
    params.append("DisplayName", searchTerm.trim());
  }

  const data = await apiGet<PaginatedResponse>(`/Agent?${params.toString()}`);
  return data;
}
