"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";
import { getGroupId, getSelectedOrgId } from "@/lib/group-context";

export interface InterviewGuideQuestion {
  id: string;
  interviewGuideId: string;
  question: string;
  displayOrder: number;
  scoringWeight: number;
  scoringGuidance?: string | null;
  followUpsEnabled: boolean;
  maxFollowUps: number;
  createdAt: string;
  updatedAt?: string | null;
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
  questions: InterviewGuideQuestion[];
  questionCount: number;
  createdAt: string;
  updatedAt?: string | null;
  createdBy?: string | null;
  updatedBy?: string | null;
  isInherited?: boolean;
  ownerOrganizationName?: string | null;
}

interface PaginatedResponse {
  items: InterviewGuideItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Fetch interview guides with pagination, filters, and optional source filtering.
 */
export async function fetchInterviewGuides(
  pageNumber: number = 1,
  pageSize: number = 12,
  searchTerm?: string,
  isActive?: boolean,
  source?: "local" | "inherited"
): Promise<PaginatedResponse> {
  const params = new URLSearchParams({
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  if (searchTerm && searchTerm.trim() !== "") {
    params.append("Name", searchTerm.trim());
  }

  if (isActive !== undefined) {
    params.append("IsActive", isActive.toString());
  }

  if (source) {
    params.append("Source", source);
  }

  const data = await apiGet<PaginatedResponse>(`/InterviewGuide?${params.toString()}`);
  return data;
}

/**
 * Fetch a single interview guide by ID
 */
export async function fetchInterviewGuideById(id: string): Promise<InterviewGuideItem> {
  const data = await apiGet<InterviewGuideItem>(`/InterviewGuide/${id}?includeQuestions=true`);
  return data;
}

/**
 * Create a new interview guide.
 * groupId and organizationId are automatically resolved from context cookies.
 */
export async function createInterviewGuide(data: {
  groupId?: string;
  organizationId?: string | null;
  visibilityScope?: string;
  name: string;
  description?: string | null;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
  scoringRubric?: string | null;
  isActive?: boolean;
  questions?: Array<{
    question: string;
    displayOrder: number;
    scoringWeight: number;
    scoringGuidance?: string | null;
    followUpsEnabled?: boolean;
    maxFollowUps?: number;
  }>;
  createdBy?: string;
}): Promise<InterviewGuideItem> {
  const groupId = data.groupId || (await getGroupId()) || "";
  const selectedOrgId = data.organizationId || (await getSelectedOrgId());

  if (!selectedOrgId) {
    throw new Error("Please select an organization before creating an interview guide.");
  }

  const createdGuide = await apiPost<InterviewGuideItem>("/InterviewGuide", {
    ...data,
    groupId,
    organizationId: selectedOrgId,
    visibilityScope: data.visibilityScope || "organization_only",
  });
  return createdGuide as InterviewGuideItem;
}

/**
 * Update an interview guide
 */
export async function updateInterviewGuide(
  id: string,
  data: {
    name?: string;
    description?: string | null;
    openingTemplate?: string | null;
    closingTemplate?: string | null;
    scoringRubric?: string | null;
    isActive?: boolean;
    visibilityScope?: string;
    questions?: Array<{
      question: string;
      displayOrder: number;
      scoringWeight: number;
      scoringGuidance?: string | null;
      followUpsEnabled?: boolean;
      maxFollowUps?: number;
    }>;
    updatedBy?: string;
  }
): Promise<InterviewGuideItem> {
  const updatedGuide = await apiPut<InterviewGuideItem>(`/InterviewGuide/${id}`, data);
  return updatedGuide as InterviewGuideItem;
}

/**
 * Delete an interview guide
 */
export async function deleteInterviewGuide(id: string): Promise<void> {
  await apiDelete(`/InterviewGuide/${id}`);
}

/**
 * Clone an inherited interview guide into the currently selected organization.
 */
export async function cloneInterviewGuide(id: string): Promise<InterviewGuideItem> {
  const clonedGuide = await apiPost<InterviewGuideItem>(`/InterviewGuide/${id}/clone`);
  return clonedGuide as InterviewGuideItem;
}
