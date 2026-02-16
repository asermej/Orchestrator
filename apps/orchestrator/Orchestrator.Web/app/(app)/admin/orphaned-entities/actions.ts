"use server";

import { apiGet } from "@/lib/api-client-server";

export interface OrphanedEntitySummary {
  orphanedAgentCount: number;
  orphanedInterviewGuideCount: number;
  orphanedInterviewConfigurationCount: number;
  orphanedJobCount: number;
  orphanedApplicantCount: number;
  totalOrphanedCount: number;
  orphanedOrganizationIds: string[];
}

/**
 * Fetch orphaned entity summary from the admin endpoint.
 * Requires group admin or superadmin privileges.
 */
export async function fetchOrphanedEntities(): Promise<OrphanedEntitySummary> {
  return await apiGet<OrphanedEntitySummary>("/admin/orphaned-entities");
}
