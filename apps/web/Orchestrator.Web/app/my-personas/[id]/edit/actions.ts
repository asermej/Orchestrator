"use server";

import { auth0, getAccessToken } from "@/lib/auth0";
import { redirect } from "next/navigation";

export interface CategoryItem {
  id: string;
  name: string;
  description?: string | null;
  categoryType: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/**
 * Fetch all available categories
 */
export async function fetchAllCategories(): Promise<CategoryItem[]> {
  const session = await auth0.getSession();

  if (!session) {
    redirect("/api/auth/login");
  }

  const accessToken = await getAccessToken();
  
  if (!accessToken) {
    throw new Error('Authentication failed - no access token');
  }

  try {
    // Fetch all categories with a large page size
    const response = await fetch(
      `http://localhost:5000/api/v1/Category?PageNumber=1&PageSize=1000&IsActive=true`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        cache: "no-store",
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error("API Error:", errorText);
      throw new Error(
        `Failed to fetch categories: ${response.status} - ${errorText}`
      );
    }

    const data = await response.json();
    return data.items || [];
  } catch (error) {
    console.error("Error fetching categories:", error);
    throw error;
  }
}

/**
 * Fetch categories assigned to a persona
 */
export async function fetchPersonaCategories(personaId: string): Promise<CategoryItem[]> {
  const session = await auth0.getSession();

  if (!session) {
    redirect("/api/auth/login");
  }

  const accessToken = await getAccessToken();
  
  if (!accessToken) {
    throw new Error('Authentication failed - no access token');
  }

  try {
    const response = await fetch(
      `http://localhost:5000/api/v1/Persona/${personaId}/categories`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        cache: "no-store",
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error("API Error:", errorText);
      throw new Error(
        `Failed to fetch persona categories: ${response.status} - ${errorText}`
      );
    }

    const data: CategoryItem[] = await response.json();
    return data;
  } catch (error) {
    console.error("Error fetching persona categories:", error);
    throw error;
  }
}

/**
 * Add a category to a persona
 */
export async function addCategoryToPersona(
  personaId: string,
  categoryId: string
): Promise<void> {
  const session = await auth0.getSession();

  if (!session) {
    redirect("/api/auth/login");
  }

  const accessToken = await getAccessToken();
  
  if (!accessToken) {
    throw new Error('Authentication failed - no access token');
  }

  try {
    const response = await fetch(
      `http://localhost:5000/api/v1/Persona/${personaId}/categories`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          categoryId: categoryId,
        }),
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error("API Error:", errorText);
      throw new Error(
        `Failed to add category to persona: ${response.status} - ${errorText}`
      );
    }
  } catch (error) {
    console.error("Error adding category to persona:", error);
    throw error;
  }
}

/**
 * Remove a category from a persona
 */
export async function removeCategoryFromPersona(
  personaId: string,
  categoryId: string
): Promise<void> {
  const session = await auth0.getSession();

  if (!session) {
    redirect("/api/auth/login");
  }

  const accessToken = await getAccessToken();
  
  if (!accessToken) {
    throw new Error('Authentication failed - no access token');
  }

  try {
    const response = await fetch(
      `http://localhost:5000/api/v1/Persona/${personaId}/categories/${categoryId}`,
      {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error("API Error:", errorText);
      throw new Error(
        `Failed to remove category from persona: ${response.status} - ${errorText}`
      );
    }
  } catch (error) {
    console.error("Error removing category from persona:", error);
    throw error;
  }
}

/**
 * Create a new category
 */
export async function createCategory(
  name: string,
  description: string | null,
  categoryType: string,
  displayOrder: number
): Promise<CategoryItem> {
  const session = await auth0.getSession();

  if (!session) {
    redirect("/api/auth/login");
  }

  const accessToken = await getAccessToken();
  
  if (!accessToken) {
    throw new Error('Authentication failed - no access token');
  }

  try {
    const response = await fetch(
      `http://localhost:5000/api/v1/Category`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          name,
          description,
          categoryType,
          displayOrder,
        }),
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error("API Error:", errorText);
      throw new Error(
        `Failed to create category: ${response.status} - ${errorText}`
      );
    }

    const data: CategoryItem = await response.json();
    return data;
  } catch (error) {
    console.error("Error creating category:", error);
    throw error;
  }
}

