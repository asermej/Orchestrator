"use server";

import { apiGet, apiPut } from "@/lib/api-client-server";

export interface AgentTrainingData {
  trainingContent: string;
}

interface AgentResponse {
  id: string;
  displayName: string;
  systemPrompt?: string;
  interviewGuidelines?: string;
  // ... other fields
}

/**
 * Fetches agent training data (SystemPrompt) from the agent endpoint
 */
export async function fetchAgentTraining(id: string): Promise<AgentTrainingData> {
  const agent = await apiGet<AgentResponse>(`/Agent/${id}`);
  return {
    trainingContent: agent.systemPrompt || "",
  };
}

/**
 * Updates agent training data (SystemPrompt) via the agent endpoint
 */
export async function updateAgentTraining(id: string, trainingContent: string) {
  await apiPut(`/Agent/${id}`, {
    systemPrompt: trainingContent || "",
  });
  return true;
}
