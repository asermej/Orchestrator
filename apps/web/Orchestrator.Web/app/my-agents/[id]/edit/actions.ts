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
 * Fetch categories assigned to an agent
 */
export async function fetchAgentCategories(agentId: string): Promise<CategoryItem[]> {
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
      `http://localhost:5000/api/v1/Agent/${agentId}/categories`,
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
        `Failed to fetch agent categories: ${response.status} - ${errorText}`
      );
    }

    const data: CategoryItem[] = await response.json();
    return data;
  } catch (error) {
    console.error("Error fetching agent categories:", error);
    throw error;
  }
}

/**
 * Add a category to an agent
 */
export async function addCategoryToAgent(
  agentId: string,
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
      `http://localhost:5000/api/v1/Agent/${agentId}/categories`,
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
        `Failed to add category to agent: ${response.status} - ${errorText}`
      );
    }
  } catch (error) {
    console.error("Error adding category to agent:", error);
    throw error;
  }
}

/**
 * Remove a category from an agent
 */
export async function removeCategoryFromAgent(
  agentId: string,
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
      `http://localhost:5000/api/v1/Agent/${agentId}/categories/${categoryId}`,
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
        `Failed to remove category from agent: ${response.status} - ${errorText}`
      );
    }
  } catch (error) {
    console.error("Error removing category from agent:", error);
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

