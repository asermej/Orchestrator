"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";
import { PersonaItem } from "../../actions";

/**
 * Converts an Auth0 sub claim to a deterministic Guid
 * This creates a consistent Guid for the same Auth0 user every time
 */
function auth0SubToGuid(auth0Sub: string): string {
  // Create a deterministic hash of the Auth0 sub
  // We'll use a simple approach: take the numeric part if it exists, or hash the string
  
  // Try to extract numeric ID from Auth0 sub (e.g., "google-oauth2|111706907437700312262")
  const numericMatch = auth0Sub.match(/\|(\d+)$/);
  
  if (numericMatch) {
    // Convert the numeric ID to a Guid-like format
    // Pad to 32 hex characters and format as a Guid
    const numericId = numericMatch[1];
    const hex = BigInt(numericId).toString(16).padStart(32, '0');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20, 32)}`;
  }
  
  // Fallback: Create a hash-based Guid for non-numeric Auth0 subs
  // This uses a simple string-to-hex conversion
  let hash = 0;
  for (let i = 0; i < auth0Sub.length; i++) {
    hash = ((hash << 5) - hash) + auth0Sub.charCodeAt(i);
    hash = hash & hash; // Convert to 32bit integer
  }
  
  // Create a deterministic Guid from the hash
  const hex = Math.abs(hash).toString(16).padStart(32, '0');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20, 32)}`;
}

// Types based on the API ResourceModels
export interface Chat {
  id: string;
  personaId: string;
  userId: string;
  title: string | null;
  lastMessageAt: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface Message {
  id: string;
  chatId: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
}

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function fetchPersonaById(id: string): Promise<PersonaItem> {
  const data = await apiGet<PersonaItem>(`/persona/${id}`);
  return data;
}

/**
 * Creates a new chat between a user and a persona
 */
export async function createChat(
  personaId: string,
  userId: string,
  title?: string
): Promise<Chat> {
  // Convert Auth0 sub to Guid format
  const userGuid = auth0SubToGuid(userId);

  const payload = {
    personaId,
    userId: userGuid,
    title,
    lastMessageAt: new Date().toISOString(),
  };

  return await apiPost<Chat>('/chat', payload);
}

/**
 * Fetches all chats for a specific user and persona
 */
export async function fetchChats(
  personaId: string,
  userId: string,
  pageNumber: number = 1,
  pageSize: number = 50
): Promise<PaginatedResponse<Chat>> {
  // Convert Auth0 sub to Guid format
  const userGuid = auth0SubToGuid(userId);

  const params = new URLSearchParams({
    PersonaId: personaId,
    UserId: userGuid,
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  try {
    return await apiGet<PaginatedResponse<Chat>>(`/chat?${params.toString()}`);
  } catch (error) {
    // If 401/404, user might not have any chats yet - return empty result
    console.log(`No chats found for persona ${personaId}, returning empty result`);
    return {
      items: [],
      totalCount: 0,
      pageNumber,
      pageSize,
    };
  }
}

/**
 * Fetches all messages for a specific chat
 */
export async function fetchChatMessages(
  chatId: string,
  pageNumber: number = 1,
  pageSize: number = 100
): Promise<PaginatedResponse<Message>> {
  const params = new URLSearchParams({
    ChatId: chatId,
    PageNumber: pageNumber.toString(),
    PageSize: pageSize.toString(),
  });

  return await apiGet<PaginatedResponse<Message>>(`/message?${params.toString()}`);
}

/**
 * Sends a user message and receives an AI response
 */
export async function sendMessage(
  chatId: string,
  content: string
): Promise<Message> {
  const payload = {
    chatId,
    content,
  };

  return await apiPost<Message>('/message/send', payload);
}

/**
 * Updates a chat's title
 */
export async function updateChatTitle(
  chatId: string,
  title: string
): Promise<Chat> {
  const payload = { title };
  return await apiPut<Chat>(`/chat/${chatId}`, payload);
}

/**
 * Deletes a chat
 */
export async function deleteChat(chatId: string): Promise<void> {
  await apiDelete(`/chat/${chatId}`);
}

// Category Types and Actions

export interface Category {
  id: string;
  name: string;
  description?: string;
  categoryType: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface PersonaCategory {
  id: string;
  personaId: string;
  categoryId: string;
  createdAt: string;
}

// Chat Topic Types and Actions

export interface ChatTopic {
  id: string;
  chatId: string;
  topicId: string;
  addedAt: string;
}

export interface TagItem {
  id: string;
  name: string;
}

export interface TopicItem {
  id: string;
  name: string;
  description?: string;
  personaId: string;
  contentUrl: string;
  contributionNotes?: string;
  categoryId: string;
  tags?: TagItem[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Fetches all topics loaded in a chat
 * Note: API returns TopicResource[] (full topic objects), not ChatTopic[] (join table records)
 */
export async function fetchChatTopics(chatId: string): Promise<TopicItem[]> {
  return await apiGet<TopicItem[]>(`/chat/${chatId}/topics`);
}

/**
 * Adds a topic to a chat
 * Note: API returns TopicResource (full topic object), not ChatTopic (join table record)
 */
export async function addTopicToChat(chatId: string, topicId: string): Promise<TopicItem> {
  const payload = { topicId };
  return await apiPost<TopicItem>(`/chat/${chatId}/topics`, payload);
}

/**
 * Removes a topic from a chat
 */
export async function removeTopicFromChat(chatId: string, topicId: string): Promise<void> {
  await apiDelete(`/chat/${chatId}/topics/${topicId}`);
}

/**
 * Fetches all topics for a persona
 */
export async function fetchPersonaTopics(personaId: string): Promise<TopicItem[]> {
  const data = await apiGet<{items: TopicItem[]}>(`/topic?personaId=${personaId}&pageSize=100`);
  return data.items || [];
}

/**
 * Fetches all available topics
 */
export async function fetchAllTopics(): Promise<TopicItem[]> {
  const data = await apiGet<{items: TopicItem[]}>('/topic?pageSize=100');
  return data.items || [];
}

/**
 * Fetches all available categories
 */
export async function fetchAllCategories(): Promise<Category[]> {
  const data = await apiGet<{items: Category[]}>('/category?pageSize=100');
  return data.items || [];
}

/**
 * Fetches all available tags
 */
export async function fetchAllTags(): Promise<TagItem[]> {
  const data = await apiGet<{items: TagItem[]}>('/tag?pageSize=100');
  return data.items || [];
}

/**
 * Fetches categories associated with a persona
 */
export async function fetchPersonaCategories(personaId: string): Promise<PersonaCategory[]> {
  return await apiGet<PersonaCategory[]>(`/persona/${personaId}/categories`);
}

/**
 * Adds a category to a persona
 */
export async function addCategoryToPersona(personaId: string, categoryId: string): Promise<PersonaCategory> {
  const payload = { categoryId };
  return await apiPost<PersonaCategory>(`/persona/${personaId}/categories`, payload);
}

/**
 * Removes a category from a persona
 */
export async function removeCategoryFromPersona(personaId: string, categoryId: string): Promise<void> {
  await apiDelete(`/persona/${personaId}/categories/${categoryId}`);
}

