"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";

/** Read-only level of the universal 1-5 behavioral rubric (not stored per competency). */
export interface UniversalRubricLevel {
  level: number;
  label: string;
  description: string;
}

export interface Competency {
  id: string;
  competencyKey: string;
  name: string;
  description?: string | null;
  canonicalExample?: string | null;
  defaultWeight: number;
  isRequired: boolean;
  displayOrder: number;
}

export interface RoleTemplate {
  id: string;
  roleKey: string;
  roleName: string;
  industry: string;
  source: string;
  groupId?: string | null;
  organizationId?: string | null;
  visibilityScope: string;
  isInherited: boolean;
  ownerOrganizationName?: string | null;
  maxFollowUpsPerQuestion: number;
  scoringScaleMin: number;
  scoringScaleMax: number;
  flagThreshold: number;
  competencyCount: number;
}

export interface RoleTemplateDetail {
  id: string;
  roleKey: string;
  roleName: string;
  industry: string;
  source: string;
  groupId?: string | null;
  organizationId?: string | null;
  visibilityScope: string;
  isInherited: boolean;
  ownerOrganizationName?: string | null;
  maxFollowUpsPerQuestion: number;
  scoringScaleMin: number;
  scoringScaleMax: number;
  flagThreshold: number;
  competencies: Competency[];
}

export async function fetchRoleTemplates(
  source?: "local" | "inherited" | "system"
): Promise<RoleTemplate[]> {
  const params = new URLSearchParams();
  if (source) {
    params.append("Source", source);
  }
  const qs = params.toString();
  return await apiGet<RoleTemplate[]>(
    `/QuestionPackageLibrary${qs ? `?${qs}` : ""}`
  );
}

export async function fetchRoleTemplateDetail(roleKey: string): Promise<RoleTemplateDetail> {
  return await apiGet<RoleTemplateDetail>(`/QuestionPackageLibrary/${roleKey}`);
}

export async function createRoleTemplate(data: {
  roleName: string;
  industry: string;
  organizationId?: string;
  visibilityScope?: string;
}): Promise<RoleTemplate> {
  const result = await apiPost<RoleTemplate>("/QuestionPackageLibrary", data);
  return result as RoleTemplate;
}

export async function cloneRoleTemplate(id: string): Promise<RoleTemplateDetail> {
  const result = await apiPost<RoleTemplateDetail>(
    `/QuestionPackageLibrary/${id}/clone`,
    {}
  );
  return result as RoleTemplateDetail;
}

export async function updateRoleTemplate(id: string, data: {
  roleName: string;
  industry: string;
  visibilityScope?: string;
}): Promise<RoleTemplate> {
  const result = await apiPut<RoleTemplate>(`/QuestionPackageLibrary/${id}`, data);
  return result as RoleTemplate;
}

export async function deleteRoleTemplate(id: string): Promise<void> {
  await apiDelete(`/QuestionPackageLibrary/${id}`);
}

export async function createCompetency(roleTemplateId: string, data: {
  name: string;
  description?: string;
  canonicalExample: string;
  defaultWeight: number;
  isRequired: boolean;
  displayOrder: number;
}): Promise<Competency> {
  const result = await apiPost<Competency>(`/QuestionPackageLibrary/${roleTemplateId}/competencies`, data);
  return result as Competency;
}

export async function updateCompetency(competencyId: string, roleTemplateId: string, data: {
  name: string;
  description?: string;
  canonicalExample: string;
  defaultWeight: number;
  isRequired: boolean;
  displayOrder: number;
}): Promise<Competency> {
  const result = await apiPut<Competency>(`/QuestionPackageLibrary/competencies/${competencyId}?roleTemplateId=${roleTemplateId}`, data);
  return result as Competency;
}

export async function deleteCompetency(competencyId: string, roleTemplateId: string): Promise<void> {
  await apiDelete(`/QuestionPackageLibrary/competencies/${competencyId}?roleTemplateId=${roleTemplateId}`);
}

export async function fetchUniversalRubric(): Promise<UniversalRubricLevel[]> {
  return await apiGet<UniversalRubricLevel[]>("/QuestionPackageLibrary/universal-rubric");
}

export interface AISuggestedCompetency {
  name: string;
  defaultWeight: number;
  description: string;
}

export async function aiSuggestCompetencies(roleName: string, industry: string): Promise<AISuggestedCompetency[]> {
  const result = await apiPost<AISuggestedCompetency[]>("/QuestionPackageLibrary/ai/suggest-competencies", { roleName, industry });
  return result as AISuggestedCompetency[];
}

export async function aiSuggestCanonicalExample(competencyName: string, roleContext: string, description?: string | null): Promise<string> {
  const result = await apiPost<{ suggestedExample: string }>("/QuestionPackageLibrary/ai/suggest-canonical-example", {
    competencyName,
    roleContext,
    description: description ?? undefined,
  });
  return result.suggestedExample ?? "";
}
