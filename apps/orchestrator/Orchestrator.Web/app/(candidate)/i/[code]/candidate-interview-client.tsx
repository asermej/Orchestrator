"use client";

import { useCallback, useEffect } from "react";
import {
  InterviewExperience,
  InterviewQuestion,
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

interface CandidateInterviewClientProps {
  interview: CandidateInterview;
  agent?: CandidateAgent;
  job?: CandidateJob;
  applicant?: CandidateApplicant;
  questions?: CandidateQuestionResponse[];
  token: string;
}

/**
 * Client component for the candidate interview experience.
 * Uses the candidate session JWT (passed as prop for initial render,
 * then relies on httpOnly cookie for subsequent API calls via Next.js API routes).
 */
export function CandidateInterviewClient({
  interview,
  agent,
  job,
  applicant,
  questions: apiQuestions,
  token,
}: CandidateInterviewClientProps) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  // Map API questions to the InterviewQuestion format used by the experience component
  const questions: InterviewQuestion[] = (apiQuestions || [])
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .map((q) => ({
      id: q.id,
      text: q.text,
      maxFollowUps: q.followUpsEnabled ? q.maxFollowUps : 0,
    }));

  // Set session cookie and warmup audio cache on mount
  useEffect(() => {
    const init = async () => {
      // Set the httpOnly session cookie via Route Handler
      try {
        await fetch("/api/candidate/set-session", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token }),
        });
      } catch (err) {
        console.warn("Failed to set session cookie:", err);
      }

      // Warmup audio cache
      try {
        const response = await fetch(
          `${apiUrl}/api/v1/candidate/interview/audio/warmup`,
          {
            method: "POST",
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (response.ok) {
          const result = await response.json();
          console.log("Audio warmup complete:", result);
        }
      } catch (err) {
        console.warn("Audio warmup failed:", err);
      }
    };

    init();
  }, [apiUrl, token]);

  const handleBegin = useCallback(async () => {
    try {
      await fetch(`${apiUrl}/api/v1/candidate/interview/start`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
        },
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
          headers: {
            Authorization: `Bearer ${token}`,
          },
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

  const handleComplete = useCallback(async () => {
    try {
      await fetch(`${apiUrl}/api/v1/candidate/interview/complete`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
    } catch (err) {
      console.error("Failed to complete interview:", err);
    }
  }, [apiUrl, token]);

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

  return (
    <InterviewExperience
      questions={questions}
      agentId={agent?.id}
      agentName={agent?.displayName || "AI Interviewer"}
      agentImageUrl={agent?.profileImageUrl}
      applicantName={applicant?.firstName || "there"}
      jobTitle={job?.title}
      onBegin={handleBegin}
      onSaveResponse={handleSaveResponse}
      onUploadAudio={handleUploadAudio}
      onComplete={handleComplete}
    />
  );
}
