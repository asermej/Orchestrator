"use server";

import { apiPostFormData } from "@/lib/api-client-server";

/**
 * Server action to upload an image with authentication
 * This allows client components to upload images without directly handling auth tokens
 */
export async function uploadImage(formData: FormData) {
  const data = await apiPostFormData<{ url: string }>(
    '/Image/upload',
    formData
  );
  
  return data.url; // Return the relative URL
}

