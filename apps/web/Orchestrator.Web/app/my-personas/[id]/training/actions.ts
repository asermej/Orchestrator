"use server";

import { apiGet, apiPost, apiDelete } from "@/lib/api-client-server";

export interface CategoryItem {
  id: string;
  name: string;
  description?: string;
  categoryType: string;
  displayOrder: number;
  isActive: boolean;
}

export interface TagItem {
  id: string;
  name: string;
}

export interface TopicWithDetails {
  id: string;
  name: string;
  description?: string;
  categoryId: string;
  categoryName?: string;
  tags: TagItem[];
  contributionNotes?: string;
  hasTrainingContent: boolean;
  updatedAt: string;
  personaId: string;
  contentUrl?: string;
}

export async function fetchCategories(): Promise<CategoryItem[]> {
  const data = await apiGet<{items: CategoryItem[]}>('/Category?pageSize=100');
  return data.items || [];
}

export async function fetchTags(): Promise<TagItem[]> {
  const data = await apiGet<{items: TagItem[]}>('/Tag?pageSize=500');
  return data.items || [];
}

export async function fetchPersonaTopicsWithDetails(
  personaId: string,
  categories?: CategoryItem[]
): Promise<TopicWithDetails[]> {
  // Fetch topics for this persona
  // Note: The backend now returns tags WITH each topic (no N+1 problem)
  const topicsData = await apiGet<{items: any[]}>(`/Topic?personaId=${personaId}&pageSize=500`);

  const topics = topicsData.items || [];

  // If categories not provided, fetch them (for backwards compatibility)
  let categoryMap: Map<string, string>;
  if (categories) {
    categoryMap = new Map(
      categories.map((cat: CategoryItem) => [cat.id, cat.name])
    );
  } else {
    const categoriesData = await apiGet<{items: CategoryItem[]}>('/Category?pageSize=100');
    categoryMap = new Map(
      categoriesData.items.map((cat: CategoryItem) => [cat.id, cat.name])
    );
  }

  // Map topics with tags (tags are already included in the response)
  const topicsWithDetails: TopicWithDetails[] = topics.map((topic: any) => {
    return {
      id: topic.id,
      name: topic.name,
      description: topic.description,
      categoryId: topic.categoryId,
      categoryName: categoryMap.get(topic.categoryId) || "Unknown",
      tags: topic.tags || [], // Tags are now included in the API response
      contributionNotes: topic.contributionNotes,
      hasTrainingContent: !!topic.contentUrl,
      updatedAt: topic.updatedAt,
      personaId: topic.personaId,
      contentUrl: topic.contentUrl,
    };
  });

  return topicsWithDetails;
}

export async function createTopicAndAddToPersona(
  personaId: string,
  name: string,
  categoryId: string,
  description?: string,
  tagNames: string[] = []
): Promise<TopicWithDetails> {
  // Create the topic with personaId (topic is owned by persona from creation)
  const newTopic = await apiPost<any>('/Topic', {
    name,
    description: description || "",
    categoryId,
    personaId,
    contentUrl: "",  // Empty initially, will be filled when training content is added
    contributionNotes: "",
  });

  // Add tags if provided
  if (tagNames.length > 0) {
    await apiPost(`/Topic/${newTopic.id}/tags`, { tagNames }).catch(() => {});
  }

  // Fetch category name and tags in parallel
  const [category, tags] = await Promise.all([
    apiGet<CategoryItem>(`/Category/${categoryId}`).catch(() => ({ name: "Unknown" } as CategoryItem)),
    apiGet<TagItem[]>(`/Topic/${newTopic.id}/tags`).catch(() => []),
  ]);

  return {
    id: newTopic.id,
    name: newTopic.name,
    description: newTopic.description,
    categoryId: newTopic.categoryId,
    categoryName: category.name,
    tags,
    contributionNotes: newTopic.contributionNotes || "",
    hasTrainingContent: false,
    updatedAt: newTopic.updatedAt,
    personaId: newTopic.personaId,
    contentUrl: newTopic.contentUrl,
  };
}

export async function removeTopicFromPersona(
  personaId: string,
  topicId: string
): Promise<boolean> {
  await apiDelete(`/persona/${personaId}/topics/${topicId}`);
  return true;
}

