"use client";

import { useCallback, useEffect } from "react";
import { InterviewExperience, InterviewQuestion } from "@/components/interview-experience";

interface InterviewQuestionData {
  id: string;
  questionText: string;
  questionOrder: number;
  isRequired: boolean;
  followUpPrompt?: string;
  maxFollowUps: number;
}

interface Interview {
  id: string;
  token: string;
  status: string;
  agent?: {
    id: string;
    displayName: string;
    profileImageUrl?: string;
    systemPrompt?: string;
    interviewGuidelines?: string;
  };
  job?: {
    id: string;
    title: string;
    description?: string;
  };
  applicant?: {
    firstName?: string;
    lastName?: string;
  };
  questions: InterviewQuestionData[];
  responses: any[];
}

interface InterviewClientProps {
  token: string;
  interview: Interview;
}

export function InterviewClient({ token, interview }: InterviewClientProps) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
  
  // Transform questions to the format expected by InterviewExperience
  const questions: InterviewQuestion[] = interview.questions.map((q) => ({
    id: q.id,
    text: q.questionText,
  }));

  // Warmup audio cache on mount
  useEffect(() => {
    const warmupAudio = async () => {
      try {
        const response = await fetch(`/api/interview/${token}/warmup`, {
          method: "POST",
        });
        if (response.ok) {
          const result = await response.json();
          console.log("Audio warmup complete:", result);
        }
      } catch (err) {
        console.warn("Audio warmup failed:", err);
      }
    };
    
    warmupAudio();
  }, [token]);

  const handleBegin = useCallback(async () => {
    try {
      await fetch(`${apiUrl}/api/v1/interviews/by-token/${token}/start`, {
        method: "POST",
      });
    } catch (err) {
      console.error("Failed to start interview:", err);
    }
  }, [apiUrl, token]);

  const handleSaveResponse = useCallback(async (
    questionId: string,
    questionText: string,
    transcript: string,
    order: number
  ) => {
    await fetch(`${apiUrl}/api/v1/interviews/by-token/${token}/responses`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        questionId,
        questionText,
        transcript,
        responseOrder: order,
      }),
    });
  }, [apiUrl, token]);

  const handleComplete = useCallback(async () => {
    try {
      await fetch(`${apiUrl}/api/v1/interviews/by-token/${token}/complete`, {
        method: "POST",
      });
    } catch (err) {
      console.error("Failed to complete interview:", err);
    }
  }, [apiUrl, token]);

  return (
    <InterviewExperience
      questions={questions}
      agentId={interview.agent?.id}
      agentName={interview.agent?.displayName || "AI Interviewer"}
      agentImageUrl={interview.agent?.profileImageUrl}
      applicantName={interview.applicant?.firstName || "there"}
      onBegin={handleBegin}
      onSaveResponse={handleSaveResponse}
      onComplete={handleComplete}
    />
  );
}
