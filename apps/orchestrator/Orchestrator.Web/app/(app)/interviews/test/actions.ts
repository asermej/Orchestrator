"use server";

import { apiGet, apiPost } from "@/lib/api-client-server";

export interface TestInterviewResult {
  id: string;
  interviewId: string;
  summary?: string | null;
  score?: number | null;
  overallScoreDisplay?: number | null;
  recommendation?: string | null;
  recommendationTier?: string | null;
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

// ───── Runtime (Competency-Based) Server Actions ─────

export interface RuntimeCompetency {
  competencyId: string;
  name: string;
  description?: string;
  scoringWeight: number;
  displayOrder: number;
  primaryQuestion: string;
}

export interface RuntimeContext {
  interviewId: string;
  agentName: string;
  applicantName: string;
  jobTitle: string;
  roleName: string;
  industry: string;
  openingText: string;
  closingText: string;
  competencies: RuntimeCompetency[];
}

export interface EvaluationResult {
  competencyScore: number;
  rationale: string;
  followUpNeeded: boolean;
  followUpTarget?: string | null;
  followUpQuestion?: string | null;
}

export interface CompetencyResult {
  id: string;
  interviewId: string;
  competencyId: string;
  competencyScore: number;
  competencyRationale?: string;
  followUpCount?: number;
  scoringWeight?: number;
  generatedQuestionText?: string;
  questionsAsked?: string;
  responseText?: string;
}

export interface FollowUpExchangeData {
  question: string;
  response: string;
}

export interface PriorExchangeData {
  question: string;
  response: string;
}

export async function fetchRuntimeContext(interviewId: string): Promise<RuntimeContext> {
  const data = await apiGet<RuntimeContext>(`/Interview/${interviewId}/runtime`);
  return data;
}

export async function generateCompetencyQuestion(
  interviewId: string,
  competencyId: string,
  includeTransition?: boolean,
  previousCompetencyName?: string
): Promise<string> {
  const data = await apiPost<{ competencyId: string; question: string }>(
    `/Interview/${interviewId}/runtime/generate-question`,
    { competencyId, includeTransition: includeTransition || false, previousCompetencyName }
  );
  return (data as { question: string }).question;
}

export async function evaluateCompetencyResponse(
  interviewId: string,
  competencyId: string,
  candidateResponse: string,
  priorExchanges?: PriorExchangeData[],
  previousFollowUpTarget?: string
): Promise<EvaluationResult> {
  const data = await apiPost<EvaluationResult>(
    `/Interview/${interviewId}/runtime/evaluate`,
    { competencyId, candidateResponse, priorExchanges, previousFollowUpTarget }
  );
  return data as EvaluationResult;
}

export async function completeCompetency(
  interviewId: string,
  competencyId: string,
  primaryQuestion: string,
  candidateResponse: string,
  followUpExchanges?: FollowUpExchangeData[],
  evaluation?: { competencyScore: number; rationale: string }
): Promise<CompetencyResult> {
  const data = await apiPost<CompetencyResult>(
    `/Interview/${interviewId}/runtime/competency`,
    { competencyId, primaryQuestion, candidateResponse, followUpExchanges, competencyScore: evaluation?.competencyScore, rationale: evaluation?.rationale }
  );
  return data as CompetencyResult;
}

export interface ClassificationResult {
  classification: string;
  requiresResponse: boolean;
  responseText?: string | null;
  consumesRedirect: boolean;
  abandonCompetency: boolean;
  storeNote?: string | null;
}

export async function classifyCompetencyResponse(
  interviewId: string,
  competencyId: string,
  candidateResponse: string,
  currentQuestion: string
): Promise<ClassificationResult> {
  const data = await apiPost<ClassificationResult>(
    `/Interview/${interviewId}/runtime/classify`,
    { competencyId, candidateResponse, currentQuestion }
  );
  return data as ClassificationResult;
}

export interface ClassifyAndEvaluateResult {
  classification: string;
  requiresResponse: boolean;
  responseText?: string | null;
  consumesRedirect: boolean;
  abandonCompetency: boolean;
  storeNote?: string | null;
  competencyScore?: number | null;
  rationale?: string | null;
  followUpNeeded?: boolean | null;
  followUpTarget?: string | null;
  followUpQuestion?: string | null;
}

export async function classifyAndEvaluateCompetencyResponse(
  interviewId: string,
  competencyId: string,
  candidateResponse: string,
  currentQuestion: string,
  competencyTranscript: string,
  previousFollowUpTarget?: string
): Promise<ClassifyAndEvaluateResult> {
  const data = await apiPost<ClassifyAndEvaluateResult>(
    `/Interview/${interviewId}/runtime/classify-and-evaluate`,
    { competencyId, candidateResponse, currentQuestion, competencyTranscript, previousFollowUpTarget }
  );
  return data as ClassifyAndEvaluateResult;
}

export async function skipCompetency(
  interviewId: string,
  competencyId: string,
  primaryQuestion: string,
  skipReason: string
): Promise<void> {
  await apiPost(
    `/Interview/${interviewId}/runtime/skip-competency`,
    { competencyId, primaryQuestion, skipReason }
  );
}

export async function finalizeInterview(interviewId: string): Promise<TestInterviewResult> {
  const data = await apiPost<TestInterviewResult>(
    `/Interview/${interviewId}/runtime/finalize`,
    {}
  );
  return data as TestInterviewResult;
}

export async function createTestInterviewFromTemplate(
  interviewTemplateId: string,
  testUserName?: string
): Promise<TestInterview> {
  const data = await apiPost<TestInterview>("/Interview/test", {
    interviewTemplateId,
    testUserName
  });
  return data as TestInterview;
}
