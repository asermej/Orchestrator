"use server";

import { apiPost } from "@/lib/api-client-server";
import { redirect } from "next/navigation";

interface CreateAgentRequest {
  displayName: string;
  profileImageUrl?: string | null;
}

interface AgentResponse {
  id: string;
  displayName: string;
  profileImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export async function createAgent(formData: FormData) {
  // Extract form data
  const displayName = formData.get("displayName") as string;
  const profileImageUrl = formData.get("profileImageUrl") as string;

  // Validate required field
  if (!displayName || displayName.trim() === "") {
    throw new Error("Display name is required");
  }

  // Create the agent object to send to API
  const agent: CreateAgentRequest = {
    displayName: displayName.trim(),
    profileImageUrl: profileImageUrl && profileImageUrl.trim() !== "" ? profileImageUrl : null,
  };

  // Call the AgentController Create endpoint
  const createdAgent = await apiPost<AgentResponse>("/Agent", agent);
  
  // Redirect to my-personas page after creation
  redirect(`/my-personas`);
}
