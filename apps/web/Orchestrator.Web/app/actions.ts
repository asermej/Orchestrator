"use server";

import { apiGet } from '@/lib/api-client-server';

interface TopicFeedItem {
  id: string;
  name: string;
  description?: string;
  isPublic: boolean;
  personaId: string;
  author?: {
    id: string;
    firstName: string;
    lastName: string;
    profileImageUrl?: string;
    topicCount?: number;
    messageCount?: number;
  };
  chatCount: number;
  category: {
    id: string;
    name: string;
  };
  tags: Array<{
    id: string;
    name: string;
  }>;
  createdAt: string;
  updatedAt?: string;
}

interface PersonaFeedItem {
  id: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  profileImageUrl?: string;
  topicCount: number;
  chatCount: number;
  messageCount: number;
  categories: Array<{
    id: string;
    name: string;
  }>;
  createdAt: string;
}

interface Category {
  id: string;
  name: string;
  categoryType: string;
  isActive: boolean;
  createdAt: string;
}

interface Tag {
  id: string;
  name: string;
  createdAt: string;
}

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchTopicFeed(
  page: number = 1,
  pageSize: number = 10,
  categoryId?: string,
  tagIds?: string[],
  searchTerm?: string,
  sortBy?: string,
  isPublic?: boolean
): Promise<PaginatedResponse<TopicFeedItem>> {
  const params = new URLSearchParams({
    pageNumber: page.toString(),
    pageSize: pageSize.toString(),
  });

  if (categoryId) {
    params.append("categoryId", categoryId);
  }

  if (tagIds && tagIds.length > 0) {
    tagIds.forEach((tagId) => params.append("tagIds", tagId));
  }

  if (searchTerm) {
    params.append("searchTerm", searchTerm);
  }

  if (sortBy) {
    params.append("sortBy", sortBy);
  }

  if (isPublic !== undefined) {
    params.append("isPublic", isPublic.toString());
  }

  const data = await apiGet<PaginatedResponse<any>>(`/topic/feed?${params}`);
  
  // Add default values for author metrics (personaId now comes directly from API)
  return {
    ...data,
    items: data.items.map((topic: any) => ({
      ...topic,
      author: topic.author ? {
        ...topic.author,
        topicCount: topic.author.topicCount ?? 0,
        messageCount: topic.author.messageCount ?? 0,
      } : undefined,
    })),
  };
}

export async function fetchPersonaFeed(
  page: number = 1,
  pageSize: number = 10,
  searchTerm?: string,
  categoryId?: string,
  sortBy?: string
): Promise<PaginatedResponse<PersonaFeedItem>> {
  const params = new URLSearchParams({
    pageNumber: page.toString(),
    pageSize: pageSize.toString(),
  });

  if (searchTerm) {
    params.append("displayName", searchTerm);
  }

  if (categoryId) {
    params.append("categoryId", categoryId);
  }

  if (sortBy) {
    params.append("sortBy", sortBy);
  }

  const data = await apiGet<PaginatedResponse<any>>(`/persona?${params}`);
  
  // The API returns personas but we need to add engagement metrics
  // For now, we'll return mock data for engagement metrics since the API doesn't have them yet
  return {
    ...data,
    items: data.items.map((persona: any) => ({
      ...persona,
      topicCount: 0, // TODO: Add endpoint to get persona topic count
      chatCount: 0, // TODO: Add endpoint to get persona chat count
      messageCount: 0, // TODO: Add endpoint to get persona message count
      categories: [], // TODO: Add endpoint to get persona categories
    })),
  };
}

export async function fetchCategories(): Promise<Category[]> {
  const data = await apiGet<PaginatedResponse<Category>>('/category?pageNumber=1&pageSize=100');
  return data.items;
}

export async function fetchPopularTags(limit: number = 50): Promise<Tag[]> {
  const data = await apiGet<PaginatedResponse<Tag>>(`/tag?pageNumber=1&pageSize=${limit}`);
  return data.items;
}

