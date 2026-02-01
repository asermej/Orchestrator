"use server";

import { apiGet, apiPost, apiPut, apiDelete } from "@/lib/api-client-server";

export interface PersonaTrainingData {
  trainingContent: string;
}

export interface TopicItem {
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

export async function fetchPersonaTraining(id: string): Promise<PersonaTrainingData> {
  const data = await apiGet<PersonaTrainingData>(`/Persona/${id}/training`);
  return data;
}

export async function updatePersonaTraining(id: string, trainingContent: string) {
  const payload = {
    trainingContent: trainingContent || "",
  };

  await apiPut(`/Persona/${id}/training`, payload);
  return true;
}

// Topic Training Actions

export async function fetchPersonaTopics(personaId: string): Promise<TopicItem[]> {
  const data = await apiGet<{items: TopicItem[]}>(`/Topic?personaId=${personaId}&pageSize=100`);
  return data.items || [];
}

export async function fetchAllTopics(): Promise<TopicItem[]> {
  const data = await apiGet<{items: TopicItem[]}>('/Topic?pageSize=100');
  return data.items || [];
}

export async function fetchPersonaTopicContent(
  personaId: string,
  topicId: string
): Promise<{trainingContent: string}> {
  const topic = await apiGet<TopicItem>(`/Topic/${topicId}`);
  
  // For now, return contentUrl as trainingContent to maintain compatibility
  // In reality, you'd fetch the actual content from the URL
  return {
    trainingContent: topic.contentUrl || ""
  };
}

/**
 * @deprecated This function is no longer supported. Topics are now owned by personas from creation.
 * To create a topic for a persona, use createTopic() with personaId parameter.
 * To link an existing topic's knowledge to a persona, you need to create a new topic with the same metadata.
 */
export async function addTopicToPersona(
  personaId: string,
  topicId: string,
  trainingContent: string,
  contributionNotes?: string
) {
  // Since topics are now owned by personas, we can't "add" an existing topic to a persona
  // Instead, callers should use createTopic() to create a new topic with the personaId
  throw new Error(
    "addTopicToPersona is deprecated. Topics are now owned by personas from creation. " +
    "Use createTopic() with personaId, or update the existing topic if you own it."
  );
}

export async function updatePersonaTopicContent(
  personaId: string,
  topicId: string,
  trainingContent: string,
  contributionNotes?: string
) {
  // First fetch the topic to get its current data
  const topic = await apiGet<TopicItem>(`/Topic/${topicId}`);

  // Update the topic with new content URL and contribution notes
  const payload = {
    name: topic.name,
    description: topic.description || "",
    personaId: topic.personaId,
    contentUrl: trainingContent,  // contentUrl now stores the training content
    contributionNotes: contributionNotes || "",
    categoryId: topic.categoryId,
  };

  return await apiPut(`/Topic/${topicId}`, payload);
}

export async function removeTopicFromPersona(personaId: string, topicId: string) {
  // Since topics are now owned by personas, removing a topic means deleting it
  await apiDelete(`/Topic/${topicId}`);
  return true;
}

// Topic Management Actions

export async function createTopic(
  name: string,
  categoryId: string,
  personaId: string,
  content: string,
  description?: string,
  contributionNotes?: string
): Promise<TopicItem> {
  const payload = {
    name,
    description: description || "",
    personaId,
    content,  // Training content is now required and sent in the create request
    contributionNotes: contributionNotes || "",
    categoryId,
  };

  return await apiPost<TopicItem>('/Topic', payload);
}

export async function updateTopic(
  topicId: string,
  name: string,
  categoryId: string,
  personaId: string,
  content?: string,
  description?: string,
  contributionNotes?: string
): Promise<TopicItem> {
  const payload = {
    name,
    description: description || "",
    content,  // Optional training content - only sent if provided
    contributionNotes: contributionNotes || "",
    categoryId,
  };

  return await apiPut<TopicItem>(`/Topic/${topicId}`, payload);
}

export async function deleteTopic(topicId: string): Promise<boolean> {
  await apiDelete(`/Topic/${topicId}`);
  return true;
}

