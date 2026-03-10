"use client";

import { useCallback, useEffect, useState } from "react";
import {
  InterviewExperience,
  InterviewQuestion,
  CompetencyData,
  CompetencyCompleteResult,
  ClassifyAndEvaluateResult,
} from "@/components/interview-experience";

interface CandidateInterview {
  id: string;
  status: string;
  interviewType: string;
  currentQuestionIndex: number;
  startedAt?: string;
  completedAt?: string;
}

interface CandidateAgent {
  id: string;
  displayName: string;
  profileImageUrl?: string;
}

interface CandidateJob {
  id: string;
  title: string;
}

interface CandidateApplicant {
  firstName: string;
}

interface CandidateQuestionResponse {
  id: string;
  text: string;
  displayOrder: number;
  followUpsEnabled: boolean;
  maxFollowUps: number;
}

interface RuntimeContextResponse {
  interviewId: string;
  agentName: string;
  applicantName: string;
  jobTitle: string;
  roleName: string;
  industry: string;
  openingText: string;
  closingText: string;
  competencies: {
    competencyId: string;
    name: string;
    description?: string;
    scoringWeight: number;
    displayOrder: number;
    primaryQuestion: string;
  }[];
}

interface CandidateInterviewClientProps {
  interview: CandidateInterview;
  agent?: CandidateAgent;
  job?: CandidateJob;
  applicant?: CandidateApplicant;
  questions?: CandidateQuestionResponse[];
  token: string;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
}

export function CandidateInterviewClient({
  interview,
  agent,
  job,
  applicant,
  questions: apiQuestions,
  token,
  openingTemplate,
  closingTemplate,
}: CandidateInterviewClientProps) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const [runtimeContext, setRuntimeContext] = useState<RuntimeContextResponse | null>(null);
  const [isLoadingRuntime, setIsLoadingRuntime] = useState(true);

  const questions: InterviewQuestion[] = (apiQuestions || [])
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .map((q) => ({
      id: q.id,
      text: q.text,
      maxFollowUps: q.followUpsEnabled ? q.maxFollowUps : 0,
    }));

  useEffect(() => {
    const init = async () => {
      try {
        await fetch("/api/candidate/set-session", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token }),
        });
      } catch (err) {
        console.warn("Failed to set session cookie:", err);
      }

      // Try to load runtime context for competency-based flow
      try {
        const response = await fetch(
          `${apiUrl}/api/v1/interview/${interview.id}/runtime`,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );
        if (response.ok) {
          const ctx: RuntimeContextResponse = await response.json();
          if (ctx.competencies && ctx.competencies.length > 0) {
            setRuntimeContext(ctx);
          }
        }
      } catch {
        // No runtime context — fall back to question mode
      }

      setIsLoadingRuntime(false);
    };

    init();
  }, [apiUrl, token, interview.id]);

  const handleBegin = useCallback(async () => {
    try {
      await fetch(`${apiUrl}/api/v1/candidate/interview/start`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
      });
    } catch (err) {
      console.error("Failed to start interview:", err);
    }
  }, [apiUrl, token]);

  const handleUploadAudio = useCallback(
    async (blob: Blob): Promise<string | null> => {
      const formData = new FormData();
      formData.append("file", blob, "recording.webm");

      const response = await fetch(
        `${apiUrl}/api/v1/candidate/interview/audio/upload`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
          body: formData,
        }
      );

      if (response.ok) {
        const data = await response.json();
        return data.url;
      }
      return null;
    },
    [apiUrl, token]
  );

  const handleSaveResponse = useCallback(
    async (
      questionId: string,
      questionText: string,
      transcript: string,
      order: number,
      isFollowUp?: boolean,
      followUpTemplateId?: string,
      audioUrl?: string,
      durationSeconds?: number
    ) => {
      const response = await fetch(
        `${apiUrl}/api/v1/candidate/interview/responses`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            questionId,
            questionText,
            transcript,
            responseOrder: order,
            isFollowUp: isFollowUp || false,
            followUpTemplateId,
            audioUrl,
            durationSeconds,
          }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        return data;
      }
    },
    [apiUrl, token]
  );

  // ───── Competency Mode Callbacks ─────

  const handleGenerateQuestion = useCallback(async (competencyId: string, includeTransition?: boolean, previousCompetencyName?: string): Promise<string> => {
    const response = await fetch(
      `${apiUrl}/api/v1/interview/${interview.id}/runtime/generate-question`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ competencyId, includeTransition: includeTransition || false, previousCompetencyName }),
      }
    );
    if (!response.ok) throw new Error("Failed to generate question");
    const data = await response.json();
    return data.question;
  }, [apiUrl, token, interview.id]);

  const handleCompleteCompetency = useCallback(async (
    competencyId: string,
    primaryQuestion: string,
    candidateResponse: string,
    followUpExchanges?: { question: string; response: string }[],
    evaluation?: { competencyScore: number; rationale: string }
  ): Promise<CompetencyCompleteResult> => {
    const response = await fetch(
      `${apiUrl}/api/v1/interview/${interview.id}/runtime/competency`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          competencyId,
          primaryQuestion,
          candidateResponse,
          followUpExchanges,
          competencyScore: evaluation?.competencyScore,
          rationale: evaluation?.rationale,
        }),
      }
    );
    if (!response.ok) throw new Error("Failed to complete competency");
    return response.json();
  }, [apiUrl, token, interview.id]);

  const handleClassifyAndEvaluate = useCallback(async (
    competencyId: string,
    candidateResponse: string,
    currentQuestion: string,
    competencyTranscript: string,
    previousFollowUpTarget?: string
  ): Promise<ClassifyAndEvaluateResult> => {
    const response = await fetch(
      `${apiUrl}/api/v1/interview/${interview.id}/runtime/classify-and-evaluate`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ competencyId, candidateResponse, currentQuestion, competencyTranscript, previousFollowUpTarget }),
      }
    );
    if (!response.ok) throw new Error("Failed to classify and evaluate response");
    return response.json();
  }, [apiUrl, token, interview.id]);

  const handleSkipCompetency = useCallback(async (
    competencyId: string,
    primaryQuestion: string,
    skipReason: string
  ): Promise<void> => {
    const response = await fetch(
      `${apiUrl}/api/v1/interview/${interview.id}/runtime/skip-competency`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ competencyId, primaryQuestion, skipReason }),
      }
    );
    if (!response.ok) throw new Error("Failed to skip competency");
  }, [apiUrl, token, interview.id]);

  const handleComplete = useCallback(async () => {
    try {
      if (runtimeContext) {
        await fetch(
          `${apiUrl}/api/v1/interview/${interview.id}/runtime/finalize`,
          {
            method: "POST",
            headers: { Authorization: `Bearer ${token}` },
          }
        );
      }
      await fetch(`${apiUrl}/api/v1/candidate/interview/complete`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
      });
    } catch (err) {
      console.error("Failed to complete interview:", err);
    }
  }, [apiUrl, token, interview.id, runtimeContext]);

  if (interview.status === "completed") {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center p-8 max-w-md">
          <h1 className="text-3xl font-bold text-white mb-4">
            Interview Completed
          </h1>
          <p className="text-slate-400 mb-6">
            Thank you for completing your interview. The results have been
            submitted.
          </p>
        </div>
      </div>
    );
  }

  if (isLoadingRuntime) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-black">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-white/30 border-t-white rounded-full animate-spin mx-auto mb-4" />
          <p className="text-white/60 text-sm">Loading interview...</p>
        </div>
      </div>
    );
  }

  // Competency mode: use runtime context
  if (runtimeContext && runtimeContext.competencies.length > 0) {
    const competencies: CompetencyData[] = runtimeContext.competencies.map(c => ({
      competencyId: c.competencyId,
      name: c.name,
      description: c.description,
      scoringWeight: c.scoringWeight,
      displayOrder: c.displayOrder,
      primaryQuestion: c.primaryQuestion,
    }));

    return (
      <InterviewExperience
        competencies={competencies}
        interviewId={interview.id}
        agentId={agent?.id}
        agentName={runtimeContext.agentName || agent?.displayName || "AI Interviewer"}
        agentImageUrl={agent?.profileImageUrl}
        applicantName={runtimeContext.applicantName || applicant?.firstName || "there"}
        jobTitle={runtimeContext.jobTitle || job?.title}
        openingTemplate={runtimeContext.openingText || openingTemplate}
        closingTemplate={runtimeContext.closingText || closingTemplate}
        onBegin={handleBegin}
        onUploadAudio={handleUploadAudio}
        onGenerateQuestion={handleGenerateQuestion}
        onCompleteCompetency={handleCompleteCompetency}
        onClassifyAndEvaluate={handleClassifyAndEvaluate}
        onSkipCompetency={handleSkipCompetency}
        onComplete={handleComplete}
      />
    );
  }

  // Question mode: legacy flow
  return (
    <InterviewExperience
      questions={questions}
      agentId={agent?.id}
      agentName={agent?.displayName || "AI Interviewer"}
      agentImageUrl={agent?.profileImageUrl}
      applicantName={applicant?.firstName || "there"}
      jobTitle={job?.title}
      openingTemplate={openingTemplate}
      closingTemplate={closingTemplate}
      onBegin={handleBegin}
      onSaveResponse={handleSaveResponse}
      onUploadAudio={handleUploadAudio}
      onComplete={handleComplete}
    />
  );
}
