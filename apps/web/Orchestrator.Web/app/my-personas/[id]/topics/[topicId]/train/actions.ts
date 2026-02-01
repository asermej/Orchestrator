"use server";

import { revalidatePath } from "next/cache";
import { apiGet, apiPost, apiPut } from "@/lib/api-client-server";

export interface TopicDetails {
  id: string;
  name: string;
  description?: string;
  personaId: string;
  contentUrl: string;
  contributionNotes?: string;
  categoryId: string;
  createdAt: string;
  updatedAt: string;
}

export interface TagItem {
  id: string;
  name: string;
}

export interface TopicTrainingContent {
  trainingContent: string;
  contributionNotes?: string;
}

export async function fetchTopicDetails(topicId: string): Promise<TopicDetails> {
  return await apiGet<TopicDetails>(`/Topic/${topicId}`);
}

export async function fetchTopicTags(topicId: string): Promise<TagItem[]> {
  return await apiGet<TagItem[]>(`/Topic/${topicId}/tags`);
}

export async function fetchPersonaTopicTraining(
  personaId: string,
  topicId: string
): Promise<TopicTrainingContent> {
  // Only fetch training content - contributionNotes will be merged from topic details
  // to avoid duplicate topic fetch when called in Promise.all
  let trainingContent = "";
  try {
    const trainingResponse = await apiGet<{content: string}>(`/Topic/${topicId}/training`);
    trainingContent = trainingResponse.content || "";
  } catch (error) {
    // Training content doesn't exist yet - that's okay, return empty
    console.log(`No training content found for topic ${topicId}, returning empty`);
  }
  
  return {
    trainingContent,
    contributionNotes: "" // Will be populated from topicDetails after Promise.all completes
  };
}

export async function updateTopicDetails(
  topicId: string,
  name: string,
  categoryId: string,
  description?: string,
  contributionNotes?: string
): Promise<void> {
  // Get current topic to preserve contentUrl
  const topic = await apiGet<TopicDetails>(`/Topic/${topicId}`);
  
  await apiPut(`/Topic/${topicId}`, {
    name,
    description: description || "",
    personaId: topic.personaId,
    contentUrl: topic.contentUrl, // Preserve existing URL
    contributionNotes: contributionNotes || "",
    categoryId,
  });
  
  // Revalidate the training page to ensure fresh data
  revalidatePath(`/my-personas/${topic.personaId}/topics/${topicId}/train`);
}

export async function updateTopicTags(
  topicId: string,
  tagNames: string[],
  personaId?: string
): Promise<TagItem[]> {
  const result = await apiPut<TagItem[]>(`/Topic/${topicId}/tags`, { tagNames });
  
  // Revalidate if personaId is provided
  if (personaId) {
    revalidatePath(`/my-personas/${personaId}/topics/${topicId}/train`);
  }
  
  return result;
}

export async function savePersonaTopicTraining(
  personaId: string,
  topicId: string,
  trainingContent: string
): Promise<void> {
  // Save training content to storage (returns a file:// URL)
  await apiPost(`/Topic/${topicId}/training`, {
    content: trainingContent
  });

  // Note: Contribution notes are saved separately via updateTopicDetails
  // to avoid race condition with contentUrl being overwritten
  
  // Revalidate the training page to ensure fresh data
  revalidatePath(`/my-personas/${personaId}/topics/${topicId}/train`);
}

export async function fetchAllTags(): Promise<TagItem[]> {
  const data = await apiGet<{items: TagItem[]}>('/Tag?pageSize=500');
  return data.items || [];
}

export interface CategoryItem {
  id: string;
  name: string;
  description?: string;
}

export async function fetchCategories(): Promise<CategoryItem[]> {
  const data = await apiGet<{items: CategoryItem[]}>('/Category?pageSize=100');
  return data.items || [];
}

export async function createTopicAndAddToPersona(
  personaId: string,
  name: string,
  categoryId: string,
  description: string,
  tagNames: string[],
  trainingContent: string,
  contributionNotes: string
): Promise<string> {
  // Create the topic WITH training content (required field)
  const newTopic = await apiPost<{id: string}>('/Topic', {
    name,
    description: description || "",
    personaId,
    content: trainingContent,  // Training content is now required during creation
    contributionNotes: contributionNotes || "",
    categoryId,
  });

  const topicId = newTopic.id;

  // Add tags if provided
  if (tagNames.length > 0) {
    await updateTopicTags(topicId, tagNames);
  }

  return topicId;
}

