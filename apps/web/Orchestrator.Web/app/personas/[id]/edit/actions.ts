"use server";

import { apiGet, apiPut } from "@/lib/api-client-server";
import { PersonaItem } from "../../actions";

export async function fetchPersonaById(id: string): Promise<PersonaItem> {
  const data = await apiGet<PersonaItem>(`/Persona/${id}`);
  return data;
}

export async function updatePersona(id: string, formData: FormData) {
  const displayName = formData.get("displayName") as string;
  const firstName = formData.get("firstName") as string;
  const lastName = formData.get("lastName") as string;
  const profileImageUrl = formData.get("profileImageUrl") as string;

  const payload = {
    displayName: displayName || undefined,
    firstName: firstName || null,
    lastName: lastName || null,
    profileImageUrl: profileImageUrl || null,
  };

  return await apiPut(`/Persona/${id}`, payload);
}

