"use server";

import { apiPost } from "@/lib/api-client-server";
import { redirect } from "next/navigation";

interface CreatePersonaRequest {
  firstName?: string | null;
  lastName?: string | null;
  displayName: string;
  profileImageUrl?: string | null;
}

interface PersonaResponse {
  id: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName: string;
  profileImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export async function createPersona(formData: FormData) {
  // Extract form data
  const firstName = formData.get("firstName") as string;
  const lastName = formData.get("lastName") as string;
  const displayName = formData.get("displayName") as string;
  const profileImageUrl = formData.get("profileImageUrl") as string;

  // Validate required field
  if (!displayName || displayName.trim() === "") {
    throw new Error("Display name is required");
  }

  // Create the persona object to send to API
  const persona: CreatePersonaRequest = {
    firstName: firstName && firstName.trim() !== "" ? firstName : null,
    lastName: lastName && lastName.trim() !== "" ? lastName : null,
    displayName: displayName.trim(),
    profileImageUrl: profileImageUrl && profileImageUrl.trim() !== "" ? profileImageUrl : null,
  };

  // Call the new PersonaController Create endpoint
  const createdPersona = await apiPost<PersonaResponse>("/Persona", persona);
  
  // Redirect directly to general training page with onboarding flag
  // Skip the edit page since user already filled out profile info
  redirect(`/my-personas/${createdPersona.id}/general-training?onboarding=true`);
}
