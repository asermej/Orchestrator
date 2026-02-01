"use server";

import { apiGet, apiPut, apiDelete } from "@/lib/api-client-server";

export interface PersonaItem {
  id: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName: string;
  profileImageUrl?: string | null;
  elevenLabsVoiceId?: string | null;
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
  items: PersonaItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Fetch personas created by the current user
 */
export async function fetchMyPersonas(
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

  const data = await apiGet<PaginatedResponse>(`/Persona?${params.toString()}`);
  return data;
}

/**
 * Fetch a single persona by ID
 */
export async function fetchPersonaById(id: string): Promise<PersonaItem> {
  const data = await apiGet<PersonaItem>(`/Persona/${id}`);
  return data;
}

/**
 * Update a persona
 */
export async function updatePersona(
  id: string,
  data: {
    firstName?: string | null;
    lastName?: string | null;
    displayName?: string;
    profileImageUrl?: string | null;
    elevenLabsVoiceId?: string | null;
    voiceStability?: number;
    voiceSimilarityBoost?: number;
  }
): Promise<PersonaItem> {
  const updatedPersona = await apiPut<PersonaItem>(`/Persona/${id}`, data);
  return updatedPersona;
}

/**
 * Delete a persona
 */
export async function deletePersona(id: string): Promise<void> {
  await apiDelete(`/Persona/${id}`);
}

