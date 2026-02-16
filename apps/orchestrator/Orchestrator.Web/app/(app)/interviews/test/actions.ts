"use server";

import { apiGet, apiPost } from "@/lib/api-client-server";

export interface TestInterviewResult {
  id: string;
  interviewId: string;
  summary?: string | null;
  score?: number | null;
  recommendation?: string | null;
  strengths?: string | null;
  areasForImprovement?: string | null;
  createdAt: string;
}

export interface TestInterview {
  id: string;
  jobId: string;
  applicantId: string;
  agentId: string;
  token: string;
  status: string;
  interviewType: string;
  scheduledAt?: string | null;
  startedAt?: string | null;
  completedAt?: string | null;
  currentQuestionIndex: number;
  createdAt: string;
  updatedAt?: string | null;
}

/**
 * Create a test interview from an interview configuration
 */
export async function createTestInterview(
  interviewConfigurationId: string,
  testUserName?: string
): Promise<TestInterview> {
  const data = await apiPost<TestInterview>("/Interview/test", {
    interviewConfigurationId,
    testUserName
  });
  return data as TestInterview;
}

/**
 * Score a completed test interview
 */
export async function scoreTestInterview(
  interviewId: string,
  interviewConfigurationId: string
): Promise<TestInterviewResult> {
  const data = await apiPost<TestInterviewResult>(`/Interview/test/${interviewId}/score`, {
    interviewConfigurationId
  });
  return data as TestInterviewResult;
}

/**
 * Get interview result
 */
export async function getInterviewResult(interviewId: string): Promise<TestInterviewResult | null> {
  try {
    const data = await apiGet<TestInterviewResult>(`/Interview/${interviewId}/result`);
    return data;
  } catch {
    return null;
  }
}

/**
 * Get interview by ID
 */
export async function getInterview(interviewId: string): Promise<TestInterview | null> {
  try {
    const data = await apiGet<TestInterview>(`/Interview/${interviewId}`);
    return data;
  } catch {
    return null;
  }
}

/**
 * Save a response for a test interview
 */
export async function saveTestInterviewResponse(
  interviewId: string,
  questionId: string,
  questionText: string,
  transcript: string,
  responseOrder: number
): Promise<void> {
  await apiPost(`/Interview/${interviewId}/responses`, {
    questionId,
    questionText,
    transcript,
    responseOrder
  });
}

/**
 * Warmup audio cache for test interview questions
 */
export async function warmupTestInterviewAudio(interviewId: string): Promise<void> {
  try {
    await apiPost(`/Interview/${interviewId}/audio/warmup`, {});
  } catch (err) {
    // Non-blocking - log but don't throw
    console.warn("Audio warmup failed (non-blocking):", err);
  }
}
