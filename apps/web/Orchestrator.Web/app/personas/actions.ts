"use server";

import { apiGet } from "@/lib/api-client-server";

export interface PersonaItem {
  id: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName: string;
  profileImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface PaginatedResponse {
  items: PersonaItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchPersonas(
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

  const data = await apiGet<PaginatedResponse>(`/Persona?${params.toString()}`);
  return data;
}

