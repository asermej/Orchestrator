"use server";

import { apiGet, apiPut } from "@/lib/api-client-server";

export interface RecommendationThresholds {
  id: string;
  stronglyRecommendMin: number;
  recommendMin: number;
  considerMin: number;
  doNotRecommendMin: number;
}

export async function getRecommendationThresholds(): Promise<RecommendationThresholds> {
  return (await apiGet<RecommendationThresholds>("/RecommendationThreshold")) as RecommendationThresholds;
}

export async function updateRecommendationThresholds(
  data: Omit<RecommendationThresholds, "id">
): Promise<RecommendationThresholds> {
  return (await apiPut<RecommendationThresholds>("/RecommendationThreshold", data)) as RecommendationThresholds;
}
