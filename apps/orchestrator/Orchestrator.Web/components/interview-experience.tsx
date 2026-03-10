"use client";

import { useState, useCallback, useRef, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Mic, Play, PhoneOff, Volume2, Loader2, AlertCircle, ArrowLeft, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
// Replaced with Deepgram Nova-3 STT — see /api/deepgram-token
import { useDeepgramStt } from "@/hooks/use-deepgram-stt";
import { useSilenceDetection } from "@/hooks/use-silence-detection";
import { useInterviewStateMachine, InterviewState, formatDuration } from "@/hooks/use-interview-state-machine";
import { AgentAvatar } from "@/components/agent-avatar";
import { AudioVisualizer } from "@/components/audio-visualizer";
import { InterviewControlsBar } from "@/components/interview-controls-bar";
import { InterviewAccommodationModal } from "@/components/interview-accommodation-modal";
import { MicTestCheck } from "@/components/mic-test-check";
import { PracticeRecordingCheck } from "@/components/practice-recording-check";
import { HelpCircle, ExternalLink } from "lucide-react";

// ───── Types ─────

export interface InterviewQuestion {
  id: string;
  text: string;
  isFollowUp?: boolean;
  followUpTemplateId?: string;
  followUpNumber?: number;
  maxFollowUps?: number;
}

export interface FollowUpSelectionResponse {
  selectedTemplateId?: string;
  questionText?: string;
  matchedCompetencyTag?: string;
  rationale?: string;
  nextQuestionType: "followup" | "main" | "complete";
}

export interface CompetencyData {
  competencyId: string;
  name: string;
  description?: string;
  scoringWeight: number;
  displayOrder: number;
  primaryQuestion: string;
}

export interface CompetencyEvaluation {
  competencyScore: number;
  rationale: string;
  followUpNeeded: boolean;
  followUpTarget?: string | null;
  followUpQuestion?: string | null;
}

export interface CompetencyCompleteResult {
  competencyScore: number;
  competencyRationale?: string;
  followUpCount?: number;
  questionsAsked?: string;
  responseText?: string;
}

export interface ResponseClassification {
  classification: string;
  requiresResponse: boolean;
  responseText?: string | null;
  consumesRedirect: boolean;
  abandonCompetency: boolean;
  storeNote?: string | null;
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

export interface CompetencyExchange {
  question: string;
  response: string;
  type: "primary" | "followup";
}

export interface CompetencyTranscriptBlock {
  competencyName: string;
  competencyId: string;
  score: number;
  exchanges: CompetencyExchange[];
}

interface ConversationMessage {
  role: "ai" | "candidate";
  text: string;
  isQuestion?: boolean;
}

export interface InterviewExperienceProps {
  questions?: InterviewQuestion[];
  competencies?: CompetencyData[];
  interviewId?: string;
  agentId?: string;
  agentName: string;
  agentImageUrl?: string;
  applicantName?: string;
  jobTitle?: string;
  openingTemplate?: string | null;
  closingTemplate?: string | null;
  onSaveResponse?: (questionId: string, questionText: string, transcript: string, order: number, isFollowUp?: boolean, followUpTemplateId?: string, audioUrl?: string, durationSeconds?: number) => Promise<FollowUpSelectionResponse | void>;
  onUploadAudio?: (blob: Blob) => Promise<string | null>;
  onComplete: () => Promise<void>;
  onExit?: () => void;
  onBegin?: () => Promise<void>;
  onGenerateQuestion?: (competencyId: string, includeTransition?: boolean, previousCompetencyName?: string) => Promise<string>;
  onCompleteCompetency?: (competencyId: string, primaryQuestion: string, candidateResponse: string, followUpExchanges?: { question: string; response: string }[], evaluation?: { competencyScore: number; rationale: string }) => Promise<CompetencyCompleteResult>;
  onClassifyAndEvaluate?: (competencyId: string, candidateResponse: string, currentQuestion: string, competencyTranscript: string, previousFollowUpTarget?: string) => Promise<ClassifyAndEvaluateResult>;
  onSkipCompetency?: (competencyId: string, primaryQuestion: string, skipReason: string) => Promise<void>;
}

function substituteTemplateVariables(
  template: string,
  vars: { applicantName?: string; agentName?: string; jobTitle?: string }
): string {
  return template
    .replace(/\{\{applicantName\}\}/g, vars.applicantName || "there")
    .replace(/\{\{agentName\}\}/g, vars.agentName || "your interviewer")
    .replace(/\{\{jobTitle\}\}/g, vars.jobTitle || "this position");
}

export function InterviewExperience({
  questions,
  competencies,
  interviewId,
  agentId,
  agentName,
  agentImageUrl,
  applicantName = "there",
  jobTitle,
  openingTemplate,
  closingTemplate,
  onSaveResponse,
  onUploadAudio,
  onComplete,
  onExit,
  onBegin,
  onGenerateQuestion,
  onCompleteCompetency,
  onClassifyAndEvaluate,
  onSkipCompetency,
}: InterviewExperienceProps) {
  const isCompetencyMode = !!(competencies && competencies.length > 0);

  // ───── Question Mode State ─────
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [followUpQuestions, setFollowUpQuestions] = useState<InterviewQuestion[]>([]);
  const [followUpCounts, setFollowUpCounts] = useState<{ [questionId: string]: number }>({});

  // ───── Shared State ─────
  const [transcript, setTranscript] = useState("");
  const [interimText, setInterimText] = useState("");
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentSpokenText, setCurrentSpokenText] = useState<string | null>(null);
  const [processingMessage, setProcessingMessage] = useState<string>("Thinking...");
  const [isTextResponseMode, setIsTextResponseMode] = useState(false);
  const [textResponse, setTextResponse] = useState("");
  const [accommodationModalOpen, setAccommodationModalOpen] = useState(false);
  const [repeatsRemaining, setRepeatsRemaining] = useState(2);
  const [micTestPassed, setMicTestPassed] = useState(false);
  const [accommodationRequested, setAccommodationRequested] = useState(false);
  const [conversationMessages, setConversationMessages] = useState<ConversationMessage[]>([]);
  const [currentQuestionText, setCurrentQuestionText] = useState<string | null>(null);

  // ───── Competency Mode State ─────
  const [currentCompetencyIndex, setCurrentCompetencyIndex] = useState(0);
  type CompetencyPhase = "generating" | "primary" | "evaluating" | "followup" | "scoring" | "done";
  const [competencyPhase, setCompetencyPhase] = useState<CompetencyPhase>("generating");
  const [competencyPrimaryQuestion, setCompetencyPrimaryQuestion] = useState("");
  const [competencyPrimaryResponse, setCompetencyPrimaryResponse] = useState("");
  const [competencyFollowUps, setCompetencyFollowUps] = useState<{ question: string; response: string }[]>([]);
  const [latestEvaluation, setLatestEvaluation] = useState<CompetencyEvaluation | null>(null);
  const [competencyFollowUpCount, setCompetencyFollowUpCount] = useState(0);
  const [completedCompetencies, setCompletedCompetencies] = useState<CompetencyTranscriptBlock[]>([]);
  const redirectCountRef = useRef<Map<string, number>>(new Map());
  const interviewLanguageRef = useRef<string | undefined>(undefined);

  const transcriptRef = useRef("");
  const recordingStartTimeRef = useRef<number>(0);
  const audioRef = useRef<HTMLAudioElement>(null);
  const mediaSourceRef = useRef<MediaSource | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const pregenAudioRef = useRef<{ url: string; text: string } | null>(null);
  const pregenAbortRef = useRef<AbortController | null>(null);
  const ttsGateRef = useRef<Promise<void>>(Promise.resolve());
  const isTextResponseModeRef = useRef(false);
  const conversationEndRef = useRef<HTMLDivElement>(null);
  const silenceHandledRef = useRef(false);

  // When ElevenLabs is down/quota-exceeded, skip all ElevenLabs calls and use browser TTS
  const ttsUnavailableRef = useRef(false);

  // ───── Pipelining: pre-generate first question + TTS during greeting ─────
  const pregenQuestionRef = useRef<{ competencyIndex: number; text: string } | null>(null);
  const pregenQuestionAbortRef = useRef<AbortController | null>(null);

  const stateMachine = useInterviewStateMachine();
  const { state } = stateMachine;

  const supportsMediaSource = typeof window !== "undefined" && "MediaSource" in window;

  const questionsArray = questions || [];
  const allQuestions = [...questionsArray, ...followUpQuestions];
  const currentQuestion = allQuestions[currentQuestionIndex] || questionsArray[currentQuestionIndex];
  const currentCompetency = isCompetencyMode ? competencies![currentCompetencyIndex] : null;
  const totalCompetencies = isCompetencyMode ? competencies!.length : 0;
  const isLastCompetency = isCompetencyMode && currentCompetencyIndex >= totalCompetencies - 1;
  const isLastQuestion = isCompetencyMode
    ? (isLastCompetency && (competencyPhase === "done" || competencyPhase === "scoring"))
    : currentQuestionIndex >= allQuestions.length - 1;
  const totalQuestions = isCompetencyMode ? totalCompetencies : allQuestions.length;
  const progressPercent = isCompetencyMode
    ? (totalCompetencies > 0 ? ((currentCompetencyIndex + 1) / totalCompetencies) * 100 : 0)
    : (totalQuestions > 0 ? ((currentQuestionIndex + 1) / totalQuestions) * 100 : 0);

  const getQuestionIndicator = () => {
    if (isCompetencyMode && currentCompetency) {
      return `Competency ${currentCompetencyIndex + 1} of ${totalCompetencies}`;
    }
    return `Question ${currentQuestionIndex + 1} of ${totalQuestions}`;
  };

  const getGreetingText = useCallback(() => {
    if (openingTemplate) {
      return substituteTemplateVariables(openingTemplate, { applicantName, agentName, jobTitle });
    }
    if (isCompetencyMode) {
      return `Hello ${applicantName}! I'm ${agentName}, and I'll be conducting your interview today. Let's get started.`;
    }
    const question = questionsArray[0];
    return `Hello ${applicantName}! I'm ${agentName}, and I'll be conducting your interview today. Let's get started with our first question. ${question?.text || "Tell me about yourself."}`;
  }, [applicantName, agentName, jobTitle, questionsArray, openingTemplate, isCompetencyMode]);

  // ───── Transcript Handling (Deepgram Nova-3 STT) ─────

  const stopPlaybackRef = useRef<(() => void) | null>(null);

  const handleDeepgramFinal = useCallback((text: string) => {
    if (silenceHandledRef.current) return;
    silenceHandledRef.current = true;

    const message = text.trim();
    if (!message) {
      silenceHandledRef.current = false;
      return;
    }

    transcriptRef.current = message;
    setTranscript(message);
    setInterimText("");
    stateMachine.onSilenceDetected();

    setConversationMessages(prev => [...prev, { role: "candidate", text: message }]);

    if (isCompetencyMode) {
      processCompetencyResponseRef.current?.(message);
    } else {
      processQuestionResponseRef.current?.(message);
    }
  }, [isCompetencyMode, stateMachine]);

  const signalSpeechRef = useRef<(() => void) | null>(null);

  const handleInterimTranscript = useCallback((text: string) => {
    setInterimText(text);
    // Keep transcriptRef in sync with accumulated Deepgram finals so the
    // silence-detection fallback can submit the transcript if speech_final
    // never fires (e.g. in noisy environments).
    if (text) {
      transcriptRef.current = text;
      // Deepgram detected speech — sync with the silence-detection hook so
      // it knows the candidate is speaking even when raw audio dB is below
      // its threshold. Prevents idle-skip from firing mid-answer.
      signalSpeechRef.current?.();
    }
  }, []);

  const handleBargeIn = useCallback(() => {
    stopPlaybackRef.current?.();
    if (stateMachine.state === InterviewState.AI_SPEAKING) {
      setIsPlaying(false);
      setCurrentSpokenText(null);
      stateMachine.onTTSEnd();
    }
  }, [stateMachine]);

  const {
    isListening: isRecording,
    isSupported,
    error: voiceError,
    mediaStream,
    startListening: startRecording,
    stopListening: stopRecording,
    destroyConnection: destroyDeepgramConnection,
  } = useDeepgramStt({
    onFinalTranscript: handleDeepgramFinal,
    onInterimTranscript: handleInterimTranscript,
    onBargeIn: handleBargeIn,
    onError: (err) => {
      if (isTextResponseModeRef.current) return;
      if (err.includes("denied") || err.includes("permission")) {
        stateMachine.handleError("mic_permission", err);
      } else if (err.includes("microphone") || err.includes("audio")) {
        stateMachine.handleError("audio_capture", err);
      } else if (err.includes("token") || err.includes("WebSocket") || err.includes("Deepgram")) {
        stateMachine.handleError("network", err);
      } else {
        stateMachine.handleError("network", err);
      }
    },
  });

  // ───── Silence Detection ─────

  // Fallback turn completion via silence detection — primary path is Deepgram speech_final
  const handleTurnComplete = useCallback(() => {
    if (silenceHandledRef.current) return;
    silenceHandledRef.current = true;

    const message = transcriptRef.current.trim();
    if (!message) {
      silenceHandledRef.current = false;
      return;
    }

    stopRecording();
    setInterimText("");
    stateMachine.onSilenceDetected();

    setConversationMessages(prev => [...prev, { role: "candidate", text: message }]);

    if (isCompetencyMode) {
      processCompetencyResponseRef.current?.(message);
    } else {
      processQuestionResponseRef.current?.(message);
    }
  }, [isCompetencyMode, stopRecording, stateMachine]);

  const [idlePromptVisible, setIdlePromptVisible] = useState(false);

  const handleIdlePrompt = useCallback(async () => {
    if (state !== InterviewState.LISTENING) return;
    setIdlePromptVisible(true);
    setTimeout(() => setIdlePromptVisible(false), 5000);
  }, [state]);

  const handleIdleSkip = useCallback(() => {
    if (silenceHandledRef.current) return;
    silenceHandledRef.current = true;

    stopRecording();
    setInterimText("");
    stateMachine.onSilenceDetected();

    const skipText = "No problem, let's continue.";
    setConversationMessages(prev => [...prev, { role: "ai", text: skipText }]);

    if (isCompetencyMode) {
      skipCompetencyTurn();
    } else {
      advanceToNextQuestion();
    }
  }, [isCompetencyMode, stopRecording, stateMachine]);

  const silenceDetection = useSilenceDetection({
    mediaStream,
    enabled: state === InterviewState.LISTENING && !isTextResponseMode,
    silenceThresholdDb: -45,
    turnCompleteMs: 2000,
    maxSpeechDurationMs: 60000,
    idlePromptMs: 8000,
    idleSkipMs: accommodationRequested ? 30000 : 20000,
    onTurnComplete: handleTurnComplete,
    onIdlePrompt: handleIdlePrompt,
    onIdleSkip: handleIdleSkip,
  });

  useEffect(() => { signalSpeechRef.current = silenceDetection.signalSpeech; }, [silenceDetection.signalSpeech]);

  // ───── Auto-scroll conversation ─────

  useEffect(() => {
    conversationEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [conversationMessages, transcript, interimText]);

  // ───── Cleanup ─────

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
      pregenAbortRef.current?.abort();
      pregenQuestionAbortRef.current?.abort();
      if (mediaSourceRef.current?.readyState === "open") {
        try { mediaSourceRef.current.endOfStream(); } catch (e) {}
      }
      if (pregenAudioRef.current?.url) {
        URL.revokeObjectURL(pregenAudioRef.current.url);
      }
    };
  }, []);

  // ───── Audio ended handler ─────
  //
  // onTTSPlaybackCompleteRef encapsulates post-TTS logic (start listening or
  // start next competency).  It is kept in a ref so that non-<audio> playback
  // paths (browser speechSynthesis, no-agentId timeout) can call it without
  // stale-closure issues -- the ref is updated every render via the useEffect
  // below and always sees current state.

  const startListeningRef = useRef<(() => void) | null>(null);
  const startNextCompetencyRef = useRef<((index: number) => Promise<void>) | null>(null);
  const pregenFirstQuestionRef = useRef<((index: number) => void) | null>(null);
  const currentCompetencyIndexRef = useRef(0);
  const onTTSPlaybackCompleteRef = useRef<(() => void) | null>(null);

  useEffect(() => {
    onTTSPlaybackCompleteRef.current = () => {
      setIsPlaying(false);
      setCurrentSpokenText(null);

      if (isCompetencyMode && greetingDoneRef.current) {
        greetingDoneRef.current = false;
        startNextCompetencyRef.current?.(currentCompetencyIndexRef.current).then(() => {});
        return;
      }

      stateMachine.onTTSEnd();
      setTimeout(() => startListeningRef.current?.(), 100);
    };
  }, [state, stateMachine, isPlaying, isCompetencyMode]);

  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;

    const handleEnded = () => {
      if (streamingOwnsPlaybackRef.current) return;
      if (state === InterviewState.AI_SPEAKING && isPlaying) {
        onTTSPlaybackCompleteRef.current?.();
      }
    };

    audio.addEventListener("ended", handleEnded);
    return () => audio.removeEventListener("ended", handleEnded);
  }, [state, stateMachine, isPlaying, isCompetencyMode]);

  // ───── Reset state on new turn ─────

  useEffect(() => {
    if (state === InterviewState.AI_SPEAKING) {
      setIsTextResponseMode(false);
      isTextResponseModeRef.current = false;
      setTextResponse("");
      setRepeatsRemaining(1);
    }
  }, [state]);

  useEffect(() => {
    isTextResponseModeRef.current = isTextResponseMode;
  }, [isTextResponseMode]);

  // ───── Core Functions ─────

  const startListening = useCallback(async () => {
    setTranscript("");
    setInterimText("");
    transcriptRef.current = "";
    recordingStartTimeRef.current = Date.now();
    silenceHandledRef.current = false;
    silenceDetection.reset();
    await startRecording();
  }, [startRecording, silenceDetection]);

  useEffect(() => { startListeningRef.current = startListening; }, [startListening]);

  const addAiMessage = useCallback((text: string, isQuestion = false) => {
    setConversationMessages(prev => [...prev, { role: "ai", text, isQuestion }]);
    if (isQuestion) {
      setCurrentQuestionText(text);
    }
  }, []);

  // ───── Streaming Turn Helper ─────

  // Tracks the last AI response text for the current competency, used to
  // prevent identical rephrasing when the LLM generates a [REPEAT].
  const lastAiResponseRef = useRef<string | null>(null);

  // When true, the streaming path owns the audio lifecycle and the generic
  // handleEnded → onTTSPlaybackComplete callback should be suppressed.
  const streamingOwnsPlaybackRef = useRef(false);

  // LATENCY-CRITICAL: Calls the streaming respond-to-turn endpoint and plays audio
  // via MediaSource as chunks arrive. Returns metadata (response text, type, follow-up target).
  // Waits for actual audio playback to finish (not just buffering) before resolving.
  const streamRespondToTurn = useCallback(async (
    turnPayload: {
      interviewId: string;
      candidateTranscript: string;
      competencyId: string;
      competencyName: string;
      currentQuestion: string;
      phase: string;
      followUpCount: number;
      accumulatedTranscript: string;
      previousFollowUpTarget?: string;
      repeatsRemaining?: number;
      language?: string;
      previousAiResponse?: string;
      isLastCompetency?: boolean;
    },
    onMetadata?: (meta: { responseText: string; responseType: string; followUpTarget?: string; languageCode?: string }) => void,
  ): Promise<{ responseText: string; responseType: string; followUpTarget?: string; languageCode?: string }> => {
    const response = await fetch("/api/voice/respond-to-turn", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(turnPayload),
    });

    if (!response.ok) {
      throw new Error(`respond-to-turn failed: ${response.status}`);
    }

    const responseText = decodeURIComponent(response.headers.get("X-Response-Text") || "");
    const responseType = response.headers.get("X-Response-Type") || "transition";
    const followUpTarget = response.headers.get("X-Follow-Up-Target") || undefined;
    const languageCode = response.headers.get("X-Language-Code") || undefined;

    onMetadata?.({ responseText, responseType, followUpTarget, languageCode });

    // Stream audio via MediaSource if body is available
    if (response.body && supportsMediaSource && audioRef.current) {
      streamingOwnsPlaybackRef.current = true;
      setCurrentSpokenText(responseText);
      setIsPlaying(true);

      const audio = audioRef.current;
      const mediaSource = new MediaSource();
      mediaSourceRef.current = mediaSource;
      audio.src = URL.createObjectURL(mediaSource);

      await new Promise<void>((resolve, reject) => {
        const onPlaybackEnded = () => {
          audio.removeEventListener("ended", onPlaybackEnded);
          // Keep streamingOwnsPlaybackRef true so the useEffect "ended"
          // handler (which may fire for the same event in a different
          // listener-registration order after React re-renders) still
          // sees the guard and returns early. Cleared after the await.
          setIsPlaying(false);
          setCurrentSpokenText(null);
          resolve();
        };
        audio.addEventListener("ended", onPlaybackEnded);

        mediaSource.addEventListener("sourceopen", async () => {
          try {
            const sourceBuffer = mediaSource.addSourceBuffer("audio/mpeg");
            const reader = response.body!.getReader();
            let playbackStarted = false;
            while (true) {
              const { done, value } = await reader.read();
              if (done) break;
              if (sourceBuffer.updating) {
                await new Promise<void>(r => sourceBuffer.addEventListener("updateend", () => r(), { once: true }));
              }
              sourceBuffer.appendBuffer(value);
              if (!playbackStarted && audio) {
                await new Promise<void>(r => sourceBuffer.addEventListener("updateend", () => r(), { once: true }));
                audio.play().catch(console.error);
                playbackStarted = true;
              }
            }
            if (sourceBuffer.updating) {
              await new Promise<void>(r => sourceBuffer.addEventListener("updateend", () => r(), { once: true }));
            }
            if (mediaSource.readyState === "open") mediaSource.endOfStream();
          } catch (err) {
            audio.removeEventListener("ended", onPlaybackEnded);
            streamingOwnsPlaybackRef.current = false;
            reject(err);
          }
        }, { once: true });
      });
      streamingOwnsPlaybackRef.current = false;
    } else if (responseText) {
      // No audio body — display text with a reading delay
      setCurrentSpokenText(responseText);
      setIsPlaying(true);
      const readingTime = Math.max(2000, responseText.length * 50);
      await new Promise<void>(resolve => setTimeout(resolve, readingTime));
      setIsPlaying(false);
      setCurrentSpokenText(null);
    }

    return { responseText, responseType, followUpTarget, languageCode };
  }, [supportsMediaSource]);

  // ───── Competency Mode: Process Response ─────

  const processCompetencyResponseRef = useRef<((message: string) => Promise<void>) | null>(null);
  const processQuestionResponseRef = useRef<((message: string) => Promise<void>) | null>(null);

  const handleOffScriptResponse = useCallback(async (
    comp: CompetencyData,
    classification: ResponseClassification,
    questionText: string
  ) => {
    const compId = comp.competencyId;
    if (classification.consumesRedirect) {
      const current = redirectCountRef.current.get(compId) ?? 0;
      redirectCountRef.current.set(compId, current + 1);
    }

    const redirectCount = redirectCountRef.current.get(compId) ?? 0;
    const shouldAbandon = classification.abandonCompetency ||
      (classification.classification === "off_topic" && redirectCount > 1);

    if (shouldAbandon) {
      const skipNote = classification.storeNote
        ?? "Competency skipped — candidate gave two off-topic responses and did not answer the question.";
      const abandonText = "No problem — let's move on to the next question.";
      stateMachine.finishThinking(false);
      await new Promise<void>(resolve => queueMicrotask(resolve));
      addAiMessage(abandonText);
      await speakText(abandonText);

      try {
        await onSkipCompetency!(compId, questionText, skipNote);
      } catch (err) {
        console.error("Failed to record skipped competency:", err);
      }

      setCompletedCompetencies(prev => [...prev, {
        competencyName: comp.name,
        competencyId: compId,
        score: 0,
        exchanges: [],
      }]);

      setCompetencyPhase("done");
      if (isLastCompetency) {
        await handleComplete();
      } else {
        setCurrentCompetencyIndex(prev => prev + 1);
        resetCompetencyState();
        await startNextCompetency(currentCompetencyIndex + 1);
      }
      return;
    }

    if (classification.requiresResponse && classification.responseText) {
      setTranscript("");
      transcriptRef.current = "";
      stateMachine.finishThinking(false);
      await new Promise<void>(resolve => queueMicrotask(resolve));
      addAiMessage(classification.responseText);
      await speakText(classification.responseText);
    }
  }, [stateMachine, addAiMessage, isLastCompetency, currentCompetencyIndex, onSkipCompetency]);

  const processCompetencyResponse = useCallback(async (message: string) => {
    const comp = competencies![currentCompetencyIndex];
    const questionText = currentQuestionText || competencyPrimaryQuestion;

    // ───── Primary response ─────
    if (competencyPhase === "primary" || competencyPhase === "generating") {
      setCompetencyPrimaryResponse(message);
      setProcessingMessage("Thinking...");
      setCompetencyPhase("evaluating");

      // LATENCY-CRITICAL: Try streaming respond-to-turn endpoint first.
      // This replaces classify + evaluate + TTS with a single streaming pipeline.
      // Skip playAcknowledgment here — the streaming response IS the acknowledgment + follow-up,
      // and browser speechSynthesis would overlap with the ElevenLabs audio.
      if (interviewId) {
        try {
          // Transition to AI_SPEAKING BEFORE audio plays so the <audio> onended handler
          // fires in the correct state and can transition back to LISTENING.
          stateMachine.finishThinking(false);
          await new Promise<void>(resolve => queueMicrotask(resolve));

          const { responseText, responseType, followUpTarget, languageCode } = await streamRespondToTurn({
            interviewId,
            candidateTranscript: message,
            competencyId: comp.competencyId,
            competencyName: comp.name,
            currentQuestion: questionText,
            phase: "primary",
            followUpCount: 0,
            accumulatedTranscript: message,
            repeatsRemaining: repeatsRemaining,
            language: interviewLanguageRef.current,
            previousAiResponse: lastAiResponseRef.current || undefined,
            isLastCompetency,
          }, (meta) => {
            if (meta.responseType === "transition" && !isLastCompetency) {
              pregenFirstQuestionRef.current?.(currentCompetencyIndex + 1);
            }
          });
          if (responseText) lastAiResponseRef.current = responseText;
          console.log("[StreamingTurn]", comp.name, responseType, followUpTarget);

          if (languageCode) {
            interviewLanguageRef.current = languageCode;
          }

          if (responseType === "end_interview") {
            if (responseText) addAiMessage(responseText, false);
            try { await onSkipCompetency?.(comp.competencyId, questionText, "Candidate requested to end the interview."); } catch {}
            setCompletedCompetencies(prev => [...prev, { competencyName: comp.name, competencyId: comp.competencyId, score: 0, exchanges: [] }]);
            setCompetencyPhase("done");
            await handleComplete();
          } else if (responseType === "repeat") {
            setCompetencyPhase("primary");
            setCompetencyPrimaryResponse("");
            setRepeatsRemaining(prev => Math.max(0, prev - 1));
            setTranscript("");
            transcriptRef.current = "";
            if (responseText) addAiMessage(responseText, true);
            stateMachine.onTTSEnd();
            setTimeout(() => startListeningRef.current?.(), 100);
          } else if (responseType === "off_topic") {
            const compId = comp.competencyId;
            const current = redirectCountRef.current.get(compId) ?? 0;
            redirectCountRef.current.set(compId, current + 1);
            if (current + 1 > 1) {
              const abandonText = "No problem — let's move on to the next question.";
              addAiMessage(abandonText, false);
              await speakText(abandonText);
              try { await onSkipCompetency?.(compId, questionText, "Competency skipped — candidate gave two off-topic responses."); } catch {}
              setCompletedCompetencies(prev => [...prev, { competencyName: comp.name, competencyId: compId, score: 0, exchanges: [] }]);
              setCompetencyPhase("done");
              if (isLastCompetency) { await handleComplete(); } else { setCurrentCompetencyIndex(prev => prev + 1); resetCompetencyState(); await startNextCompetency(currentCompetencyIndex + 1); }
            } else {
              setCompetencyPhase("primary");
              setCompetencyPrimaryResponse("");
              setTranscript("");
              transcriptRef.current = "";
              if (responseText) addAiMessage(responseText, true);
              stateMachine.onTTSEnd();
              setTimeout(() => startListeningRef.current?.(), 100);
            }
          } else if (responseType === "language_switch") {
            setCompetencyPhase("primary");
            setCompetencyPrimaryResponse("");
            setTranscript("");
            transcriptRef.current = "";
            if (responseText) addAiMessage(responseText, true);
            stateMachine.onTTSEnd();
            setTimeout(() => startListeningRef.current?.(), 100);
          } else if (responseType === "follow_up" && responseText) {
            const evaluation: CompetencyEvaluation = {
              competencyScore: 0,
              rationale: "",
              followUpNeeded: true,
              followUpTarget: followUpTarget,
              followUpQuestion: responseText,
            };
            setLatestEvaluation(evaluation);
            setCompetencyFollowUpCount(1);
            setCompetencyPhase("followup");
            setTranscript("");
            transcriptRef.current = "";
            addAiMessage(responseText, true);
            stateMachine.onTTSEnd();
            setTimeout(() => startListeningRef.current?.(), 100);
          } else {
            if (responseText) {
              addAiMessage(responseText, false);
            }
            const fallbackEval: CompetencyEvaluation = { competencyScore: 0, rationale: "", followUpNeeded: false };
            await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, message, [], fallbackEval, true, true);
          }
          return;
        } catch (err) {
          console.warn("Streaming respond-to-turn failed, falling back to legacy flow:", err);
        }
      }

      // Fallback: play acknowledgment while legacy flow processes
      const ackPromise = playAcknowledgment(message);

      // Fallback: classify + evaluate in a single round-trip
      if (onClassifyAndEvaluate) {
        try {
          const result = await onClassifyAndEvaluate(
            comp.competencyId, message, questionText, message
          );
          console.log("[ClassifyAndEvaluate]", comp.name, result.classification);

          if (result.classification !== "on_topic") {
            await handleOffScriptResponse(comp, result as ResponseClassification, questionText);
            return;
          }

          const evaluation: CompetencyEvaluation = {
            competencyScore: result.competencyScore ?? 1,
            rationale: result.rationale ?? "",
            followUpNeeded: result.followUpNeeded ?? false,
            followUpTarget: result.followUpTarget,
            followUpQuestion: result.followUpQuestion,
          };
          console.log("[Competency Eval]", comp.name, evaluation);
          setLatestEvaluation(evaluation);

          if (evaluation.followUpNeeded && evaluation.followUpQuestion) {
            setCompetencyFollowUpCount(1);
            setCompetencyPhase("followup");
            setTranscript("");
            transcriptRef.current = "";
            stateMachine.finishThinking(false);
            await new Promise<void>(resolve => queueMicrotask(resolve));
            addAiMessage(evaluation.followUpQuestion, true);
            await speakText(evaluation.followUpQuestion);
          } else {
            await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, message, [], evaluation);
          }
          return;
        } catch (err) {
          console.error("Classify+evaluate failed:", err);
        }
      }

      // All evaluation paths failed — score with zero and move on
      const fallback: CompetencyEvaluation = { competencyScore: 0, rationale: "", followUpNeeded: false };
      await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, message, [], fallback);
    } else if (competencyPhase === "followup") {
      const currentFollowUpQuestion = latestEvaluation?.followUpQuestion || "";
      const newExchange = { question: currentFollowUpQuestion, response: message };
      const updatedFollowUps = [...competencyFollowUps, newExchange];
      setCompetencyFollowUps(updatedFollowUps);

      if (competencyFollowUpCount < 2) {
        setProcessingMessage("Thinking...");
        const allResponses = [competencyPrimaryResponse, ...updatedFollowUps.map(fu => fu.response)];
        const accumulatedTranscript = allResponses.join("\n\n");
        const previousTarget = latestEvaluation?.followUpTarget ?? undefined;

        // LATENCY-CRITICAL: Try streaming endpoint for follow-ups too
        if (interviewId) {
          try {
            stateMachine.finishThinking(false);
            await new Promise<void>(resolve => queueMicrotask(resolve));

            const { responseText, responseType, followUpTarget, languageCode } = await streamRespondToTurn({
              interviewId,
              candidateTranscript: message,
              competencyId: comp.competencyId,
              competencyName: comp.name,
              currentQuestion: questionText,
              phase: "followup",
              followUpCount: competencyFollowUpCount,
              accumulatedTranscript,
              previousFollowUpTarget: previousTarget,
              repeatsRemaining: repeatsRemaining,
              language: interviewLanguageRef.current,
              previousAiResponse: lastAiResponseRef.current || undefined,
              isLastCompetency,
            }, (meta) => {
              if (meta.responseType === "transition" && !isLastCompetency) {
                pregenFirstQuestionRef.current?.(currentCompetencyIndex + 1);
              }
            });
            if (responseText) lastAiResponseRef.current = responseText;
            console.log("[StreamingTurn followup]", comp.name, responseType, followUpTarget);

            if (languageCode) {
              interviewLanguageRef.current = languageCode;
            }

            if (responseType === "end_interview") {
              if (responseText) addAiMessage(responseText, false);
              try { await onSkipCompetency?.(comp.competencyId, questionText, "Candidate requested to end the interview."); } catch {}
              setCompletedCompetencies(prev => [...prev, { competencyName: comp.name, competencyId: comp.competencyId, score: 0, exchanges: [] }]);
              setCompetencyPhase("done");
              await handleComplete();
            } else if (responseType === "repeat") {
              setCompetencyFollowUps(competencyFollowUps);
              setRepeatsRemaining(prev => Math.max(0, prev - 1));
              setTranscript("");
              transcriptRef.current = "";
              if (responseText) addAiMessage(responseText, true);
              stateMachine.onTTSEnd();
              setTimeout(() => startListeningRef.current?.(), 100);
            } else if (responseType === "off_topic") {
              const compId = comp.competencyId;
              const current = redirectCountRef.current.get(compId) ?? 0;
              redirectCountRef.current.set(compId, current + 1);
              if (current + 1 > 1) {
                const abandonText = "No problem — let's move on to the next question.";
                addAiMessage(abandonText, false);
                await speakText(abandonText);
                try { await onSkipCompetency?.(compId, questionText, "Competency skipped — candidate gave two off-topic responses."); } catch {}
                setCompletedCompetencies(prev => [...prev, { competencyName: comp.name, competencyId: compId, score: 0, exchanges: [] }]);
                setCompetencyPhase("done");
                if (isLastCompetency) { await handleComplete(); } else { setCurrentCompetencyIndex(prev => prev + 1); resetCompetencyState(); await startNextCompetency(currentCompetencyIndex + 1); }
              } else {
                setCompetencyFollowUps(competencyFollowUps);
                setTranscript("");
                transcriptRef.current = "";
                if (responseText) addAiMessage(responseText, true);
                stateMachine.onTTSEnd();
                setTimeout(() => startListeningRef.current?.(), 100);
              }
            } else if (responseType === "language_switch") {
              setCompetencyFollowUps(competencyFollowUps);
              setTranscript("");
              transcriptRef.current = "";
              if (responseText) addAiMessage(responseText, true);
              stateMachine.onTTSEnd();
              setTimeout(() => startListeningRef.current?.(), 100);
            } else if (responseType === "follow_up" && responseText) {
              const evaluation: CompetencyEvaluation = {
                competencyScore: 0,
                rationale: "",
                followUpNeeded: true,
                followUpTarget: followUpTarget,
                followUpQuestion: responseText,
              };
              setLatestEvaluation(evaluation);
              setCompetencyFollowUpCount(competencyFollowUpCount + 1);
              setTranscript("");
              transcriptRef.current = "";
              addAiMessage(responseText, true);
              stateMachine.onTTSEnd();
              setTimeout(() => startListeningRef.current?.(), 100);
            } else {
              if (responseText) {
                addAiMessage(responseText, false);
              }
              await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, competencyPrimaryResponse, updatedFollowUps,
                latestEvaluation || { competencyScore: 0, rationale: "", followUpNeeded: false }, true, true);
            }
            return;
          } catch (err) {
            console.warn("Streaming respond-to-turn failed for follow-up, falling back:", err);
          }
        }

        // Fallback: existing combined endpoint
        if (onClassifyAndEvaluate) {
          try {
            const result = await onClassifyAndEvaluate(
              comp.competencyId, message, questionText, accumulatedTranscript, previousTarget
            );

            if (result.classification !== "on_topic") {
              await handleOffScriptResponse(comp, result as ResponseClassification, questionText);
              return;
            }

            const evaluation: CompetencyEvaluation = {
              competencyScore: result.competencyScore ?? 1,
              rationale: result.rationale ?? "",
              followUpNeeded: result.followUpNeeded ?? false,
              followUpTarget: result.followUpTarget,
              followUpQuestion: result.followUpQuestion,
            };
            setLatestEvaluation(evaluation);

            if (evaluation.followUpNeeded && evaluation.followUpQuestion) {
              setCompetencyFollowUpCount(competencyFollowUpCount + 1);
              setTranscript("");
              transcriptRef.current = "";
              stateMachine.finishThinking(false);
              await new Promise<void>(resolve => queueMicrotask(resolve));
              addAiMessage(evaluation.followUpQuestion, true);
              await speakText(evaluation.followUpQuestion);
            } else {
              await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, competencyPrimaryResponse, updatedFollowUps, evaluation);
            }
            return;
          } catch (err) {
            console.error("Classify+evaluate failed for follow-up:", err);
          }
        }

        // All evaluation paths failed — score with what we have and move on
        await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, competencyPrimaryResponse, updatedFollowUps, latestEvaluation || { competencyScore: 0, rationale: "", followUpNeeded: false });
      } else {
        await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, competencyPrimaryResponse, updatedFollowUps, latestEvaluation || { competencyScore: 0, rationale: "", followUpNeeded: false });
      }
    }
  }, [competencies, currentCompetencyIndex, competencyPhase, competencyPrimaryQuestion, competencyPrimaryResponse, competencyFollowUps, competencyFollowUpCount, latestEvaluation, onClassifyAndEvaluate, currentQuestionText, handleOffScriptResponse, stateMachine, addAiMessage, interviewId, streamRespondToTurn, repeatsRemaining]);

  useEffect(() => { processCompetencyResponseRef.current = processCompetencyResponse; }, [processCompetencyResponse]);

  const scoreInBackground = useCallback((
    comp: CompetencyData,
    primaryQuestion: string,
    primaryResponse: string,
    followUps: { question: string; response: string }[],
    evaluation: CompetencyEvaluation
  ) => {
    const exchanges: CompetencyExchange[] = [
      { question: primaryQuestion, response: primaryResponse, type: "primary" }
    ];
    for (const fu of followUps) {
      exchanges.push({ question: fu.question, response: fu.response, type: "followup" });
    }

    onCompleteCompetency?.(
      comp.competencyId,
      primaryQuestion,
      primaryResponse,
      followUps.length > 0 ? followUps : undefined,
      { competencyScore: evaluation.competencyScore, rationale: evaluation.rationale }
    ).then(result => {
      setCompletedCompetencies(prev =>
        prev.map(c => c.competencyId === comp.competencyId ? { ...c, score: result.competencyScore } : c)
      );
    }).catch(err => {
      console.error("Background scoring failed:", err);
    });

    setCompletedCompetencies(prev => [...prev, {
      competencyName: comp.name,
      competencyId: comp.competencyId,
      score: 0,
      exchanges,
    }]);
  }, [onCompleteCompetency]);

  const scoreAndFinishCompetency = useCallback(async (
    comp: CompetencyData,
    primaryQuestion: string,
    primaryResponse: string,
    followUps: { question: string; response: string }[],
    evaluation: CompetencyEvaluation,
    skipScoreWait = false,
    alreadyTransitioned = false,
  ) => {
    setProcessingMessage("Thinking...");
    setCompetencyPhase("scoring");

    const nextIndex = currentCompetencyIndex + 1;

    if (skipScoreWait) {
      scoreInBackground(comp, primaryQuestion, primaryResponse, followUps, evaluation);
    } else {
      let score = 0;
      try {
        const result = await onCompleteCompetency!(
          comp.competencyId,
          primaryQuestion,
          primaryResponse,
          followUps.length > 0 ? followUps : undefined,
          { competencyScore: evaluation.competencyScore, rationale: evaluation.rationale }
        );
        score = result.competencyScore;
      } catch (err) {
        console.error("Failed to score competency:", err);
      }

      const exchanges: CompetencyExchange[] = [
        { question: primaryQuestion, response: primaryResponse, type: "primary" }
      ];
      for (const fu of followUps) {
        exchanges.push({ question: fu.question, response: fu.response, type: "followup" });
      }

      setCompletedCompetencies(prev => [...prev, {
        competencyName: comp.name,
        competencyId: comp.competencyId,
        score,
        exchanges,
      }]);
    }

    setCompetencyPhase("done");

    if (isLastCompetency) {
      await handleComplete();
    } else {
      setCurrentCompetencyIndex(prev => prev + 1);
      resetCompetencyState();
      if (!alreadyTransitioned) {
        stateMachine.finishThinking(false);
        await new Promise<void>(resolve => queueMicrotask(resolve));
      }
      await startNextCompetency(nextIndex);
    }
  }, [isLastCompetency, currentCompetencyIndex, onCompleteCompetency, stateMachine, scoreInBackground]);

  const skipCompetencyTurn = useCallback(async () => {
    const comp = competencies![currentCompetencyIndex];
    const fallback: CompetencyEvaluation = { competencyScore: 0, rationale: "", followUpNeeded: false };

    if (competencyPhase === "primary" || competencyPhase === "generating") {
      await scoreAndFinishCompetency(comp, competencyPrimaryQuestion, "", [], fallback);
    } else if (competencyPhase === "followup") {
      await scoreAndFinishCompetency(
        comp, competencyPrimaryQuestion, competencyPrimaryResponse,
        competencyFollowUps, latestEvaluation || fallback
      );
    }
  }, [competencies, currentCompetencyIndex, competencyPhase, competencyPrimaryQuestion, competencyPrimaryResponse, competencyFollowUps, latestEvaluation, scoreAndFinishCompetency]);

  const resetCompetencyState = useCallback(() => {
    setCompetencyPrimaryQuestion("");
    setCompetencyPrimaryResponse("");
    setCompetencyFollowUps([]);
    setLatestEvaluation(null);
    setCompetencyFollowUpCount(0);
    setCompetencyPhase("generating");
    setTranscript("");
    setInterimText("");
    transcriptRef.current = "";
    setRepeatsRemaining(2);
    setIdlePromptVisible(false);
    lastAiResponseRef.current = null;
  }, []);

  const startNextCompetency = useCallback(async (index: number) => {
    const comp = competencies![index];
    setProcessingMessage("Thinking...");

    const isFirstCompetency = index === 0;
    const previousCompetencyName = !isFirstCompetency ? competencies![index - 1]?.name : undefined;
    let question = comp.primaryQuestion;

    // Check if we already pre-generated the question during greeting playback
    const pregen = pregenQuestionRef.current;
    if (pregen && pregen.competencyIndex === index && pregen.text) {
      question = pregen.text;
      pregenQuestionRef.current = null;
    } else if (onGenerateQuestion) {
      try {
        question = await onGenerateQuestion(comp.competencyId, !isFirstCompetency, previousCompetencyName);
      } catch (err) {
        console.warn("Failed to generate question, using canonical example:", err);
      }
    }

    setCompetencyPrimaryQuestion(question);
    setCompetencyPhase("primary");
    addAiMessage(question, true);

    // Check if we already pre-generated the TTS audio for this question
    if (pregenAudioRef.current && pregenAudioRef.current.text === question) {
      setCurrentSpokenText(question);
      setIsPlaying(true);
      if (audioRef.current) {
        audioRef.current.src = pregenAudioRef.current.url;
        await audioRef.current.play();
      }
      pregenAudioRef.current = null;
      return;
    }

    // Try combined generate-and-stream endpoint for non-pre-generated questions
    if (interviewId && agentId && !pregen) {
      try {
        const res = await fetch("/api/voice/generate-and-stream", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            interviewId,
            competencyId: comp.competencyId,
            includeTransition: !isFirstCompetency,
            previousCompetencyName,
          }),
        });
        if (res.ok && res.body) {
          const generatedQ = decodeURIComponent(res.headers.get("X-Generated-Question") || "");
          if (generatedQ) {
            question = generatedQ;
            setCompetencyPrimaryQuestion(question);
            setCurrentQuestionText(question);
            setConversationMessages(prev => {
              const updated = [...prev];
              const lastMsg = updated[updated.length - 1];
              if (lastMsg?.role === "ai" && lastMsg.isQuestion) {
                lastMsg.text = question;
              }
              return updated;
            });
          }

          // Stream audio directly
          const blob = await res.blob();
          const audioUrl = URL.createObjectURL(blob);
          setCurrentSpokenText(question);
          setIsPlaying(true);
          if (audioRef.current) {
            audioRef.current.src = audioUrl;
            await audioRef.current.play();
          }
          return;
        }
      } catch (err) {
        console.warn("Combined generate-and-stream failed, falling back:", err);
      }
    }

    await speakText(question);
  }, [competencies, onGenerateQuestion, addAiMessage, interviewId, agentId]);

  useEffect(() => { startNextCompetencyRef.current = startNextCompetency; }, [startNextCompetency]);
  useEffect(() => { currentCompetencyIndexRef.current = currentCompetencyIndex; }, [currentCompetencyIndex]);

  // ───── Question Mode: Process Response ─────

  const processQuestionResponse = useCallback(async (message: string) => {
    setProcessingMessage("Thinking...");

    const isFollowUp = currentQuestion.isFollowUp || false;
    const followUpTemplateId = currentQuestion.followUpTemplateId;

    let audioUrl: string | undefined;
    let durationSeconds: number | undefined;
    if (onUploadAudio) {
      durationSeconds = Math.round((Date.now() - recordingStartTimeRef.current) / 1000);
    }

    let followUpResponse: FollowUpSelectionResponse | void;
    try {
      followUpResponse = await onSaveResponse!(
        currentQuestion.id,
        currentQuestion.text,
        message,
        currentQuestionIndex,
        isFollowUp,
        followUpTemplateId,
        audioUrl,
        durationSeconds
      );
    } catch (err) {
      console.warn("Failed to save response:", err);
      followUpResponse = undefined;
    }

    if (followUpResponse && followUpResponse.nextQuestionType === "followup" && followUpResponse.questionText) {
      const mainQuestionId = currentQuestion.isFollowUp ? questionsArray[currentQuestionIndex - 1]?.id : currentQuestion.id;
      const currentFollowUpCount = followUpCounts[mainQuestionId || ""] || 0;
      const newFollowUpCount = currentFollowUpCount + 1;

      const followUpQuestion: InterviewQuestion = {
        id: followUpResponse.selectedTemplateId || `followup-${Date.now()}`,
        text: followUpResponse.questionText,
        isFollowUp: true,
        followUpTemplateId: followUpResponse.selectedTemplateId,
        followUpNumber: newFollowUpCount,
        maxFollowUps: questionsArray.find(q => q.id === mainQuestionId)?.maxFollowUps || 2
      };

      setFollowUpQuestions(prev => [...prev, followUpQuestion]);
      setFollowUpCounts(prev => ({ ...prev, [mainQuestionId || ""]: newFollowUpCount }));

      const allQuestionsCount = questionsArray.length + followUpQuestions.length + 1;
      const nextIndex = allQuestionsCount - 1;
      setCurrentQuestionIndex(nextIndex);
      setTranscript("");
      transcriptRef.current = "";

      stateMachine.finishThinking(false);
      await new Promise<void>(resolve => queueMicrotask(resolve));
      addAiMessage(followUpQuestion.text, true);
      await speakText(followUpQuestion.text);
      return;
    }

    if (followUpResponse?.nextQuestionType === "complete" || isLastQuestion) {
      await handleComplete();
    } else {
      advanceToNextQuestion();
    }
  }, [currentQuestion, currentQuestionIndex, isLastQuestion, questionsArray, onSaveResponse, onUploadAudio, stateMachine, followUpCounts, followUpQuestions, addAiMessage]);

  useEffect(() => { processQuestionResponseRef.current = processQuestionResponse; }, [processQuestionResponse]);

  const advanceToNextQuestion = useCallback(async () => {
    const nextIndex = currentQuestionIndex + 1;
    const nextQuestionText = questionsArray[nextIndex]?.text;
    if (!nextQuestionText) {
      await handleComplete();
      return;
    }

    setCurrentQuestionIndex(nextIndex);
    setTranscript("");
    transcriptRef.current = "";

    stateMachine.finishThinking(false);
    await new Promise<void>(resolve => queueMicrotask(resolve));
    addAiMessage(nextQuestionText, true);
    await speakText(nextQuestionText);
  }, [currentQuestionIndex, questionsArray, stateMachine, addAiMessage]);

  const handleComplete = useCallback(async () => {
    destroyDeepgramConnection();

    const completePromise = onComplete().catch(err => {
      console.error("Failed to complete interview:", err);
    });

    const closing = closingTemplate
      ? substituteTemplateVariables(closingTemplate, { applicantName, agentName, jobTitle })
      : `Thank you for completing this interview, ${applicantName}! We appreciate your time and will be in touch soon with next steps. Have a great day!`;

    addAiMessage(closing);
    await speakText(closing);
    await completePromise;
    stateMachine.complete();
  }, [applicantName, agentName, jobTitle, closingTemplate, onComplete, stateMachine, addAiMessage, destroyDeepgramConnection]);

  // ───── Repeat Question ─────

  const handleRepeatQuestion = useCallback(async () => {
    if (repeatsRemaining <= 0) return;
    setRepeatsRemaining((prev) => Math.max(0, prev - 1));

    const wasListening = state === InterviewState.LISTENING;
    if (wasListening) {
      stopRecording();
    }
    stopPlayback();

    const questionToRepeat = currentQuestionText;
    if (questionToRepeat) {
      await speakText(questionToRepeat);
    }
  }, [state, currentQuestionText, stopRecording, repeatsRemaining]);

  // ───── Text Response Mode ─────

  const handleSwitchToTextResponse = useCallback(() => {
    setIsTextResponseMode(true);
    isTextResponseModeRef.current = true;
    setTextResponse("");
    if (state === InterviewState.LISTENING) {
      stopRecording();
    }
    if (state === InterviewState.PREP) {
      setAccommodationRequested(true);
      setMicTestPassed(true);
    }
  }, [state, stopRecording]);

  const handleTextSubmit = useCallback(() => {
    const message = textResponse.trim();
    if (!message) return;

    transcriptRef.current = message;
    setTranscript(message);
    silenceHandledRef.current = true;
    setInterimText("");
    stateMachine.onSilenceDetected();

    setConversationMessages(prev => [...prev, { role: "candidate", text: message }]);

    if (isCompetencyMode) {
      processCompetencyResponseRef.current?.(message);
    } else {
      processQuestionResponseRef.current?.(message);
    }
  }, [textResponse, isCompetencyMode, stateMachine]);

  // ───── Playback / TTS ─────

  const stopPlayback = useCallback(() => {
    abortControllerRef.current?.abort();
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
    if (mediaSourceRef.current?.readyState === "open") {
      try { mediaSourceRef.current.endOfStream(); } catch (e) {}
    }
    mediaSourceRef.current = null;
    setIsPlaying(false);
  }, []);

  useEffect(() => { stopPlaybackRef.current = stopPlayback; }, [stopPlayback]);

  const speakText = async (text: string, silentFail = false): Promise<void> => {
    try {
      await speakTextInternal(text, silentFail);
    } catch (err) {
      setIsPlaying(false);
      setCurrentSpokenText(null);
      if (silentFail) {
        console.warn("Speech failed (silent):", err);
        return;
      }
      throw err;
    }
  };

  const speakWithBrowserTTS = async (text: string): Promise<boolean> => {
    if (typeof window === "undefined" || !("speechSynthesis" in window)) return false;

    try {
      // Ensure voices are loaded (Chrome loads them async)
      let voices = window.speechSynthesis.getVoices();
      if (voices.length === 0) {
        await new Promise<void>((resolve) => {
          const onVoices = () => { resolve(); window.speechSynthesis.removeEventListener("voiceschanged", onVoices); };
          window.speechSynthesis.addEventListener("voiceschanged", onVoices);
          setTimeout(resolve, 1000);
        });
        voices = window.speechSynthesis.getVoices();
      }

      await new Promise<void>((resolve, reject) => {
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = 1.0;
        utterance.pitch = 1.0;
        utterance.volume = 1.0;
        if (voices.length > 0) {
          const preferred = voices.find(v => v.lang.startsWith("en") && v.localService) || voices[0];
          utterance.voice = preferred;
        }
        utterance.onend = () => resolve();
        utterance.onerror = (e) => reject(e);
        window.speechSynthesis.speak(utterance);

        // Safety: if utterance.onend doesn't fire within a reasonable time, resolve anyway
        const safetyTimeout = Math.max(5000, text.length * 80);
        setTimeout(resolve, safetyTimeout);
      });

      onTTSPlaybackCompleteRef.current?.();
      return true;
    } catch {
      return false;
    }
  };

  const speakTextInternal = async (text: string, silentFail = false): Promise<void> => {
    pregenAbortRef.current?.abort();
    await ttsGateRef.current;

    if (state !== InterviewState.AI_SPEAKING) {
      abortControllerRef.current?.abort();
    }
    abortControllerRef.current = new AbortController();
    setCurrentSpokenText(text);
    setIsPlaying(true);

    // No agent configured -- display text with a reading delay
    if (!agentId) {
      const readingTime = Math.max(3000, text.length * 50);
      setTimeout(() => {
        onTTSPlaybackCompleteRef.current?.();
      }, readingTime);
      return;
    }

    // When ElevenLabs is known to be down, skip directly to browser TTS
    if (ttsUnavailableRef.current) {
      const ok = await speakWithBrowserTTS(text);
      if (ok) return;
      // Browser TTS also failed: show text with reading delay
      const readingTime = Math.max(3000, text.length * 50);
      setTimeout(() => { onTTSPlaybackCompleteRef.current?.(); }, readingTime);
      return;
    }

    // Try ElevenLabs streaming
    if (supportsMediaSource) {
      try {
        await speakTextStreaming(text, silentFail);
        return;
      } catch (err) {
        if (silentFail) return;
        console.warn("Streaming playback failed, falling back to blob:", err);
      }
    }

    // Try ElevenLabs blob
    try {
      const response = await fetch("/api/voice/speak", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ agentId, text }),
        signal: abortControllerRef.current.signal,
      });
      if (!response.ok) {
        ttsUnavailableRef.current = true;
        throw new Error("Failed to generate speech");
      }
      const audioBlob = await response.blob();
      const audioUrl = URL.createObjectURL(audioBlob);
      if (audioRef.current) {
        audioRef.current.src = audioUrl;
        await audioRef.current.play();
      }
    } catch (err) {
      if (err instanceof Error && err.name === "AbortError") return;
      if (silentFail) { setIsPlaying(false); setCurrentSpokenText(null); return; }

      console.warn("ElevenLabs TTS failed, falling back to browser speech synthesis:", err);
      ttsUnavailableRef.current = true;

      const ok = await speakWithBrowserTTS(text);
      if (ok) return;

      // Final fallback: show text with reading delay, then proceed
      console.warn("All TTS methods failed, using reading delay");
      const readingTime = Math.max(3000, text.length * 50);
      await new Promise<void>(resolve => setTimeout(resolve, readingTime));
      onTTSPlaybackCompleteRef.current?.();
    }
  };

  const speakTextStreaming = async (text: string, silentFail = false) => {
    if (!agentId) return;
    const streamingTimeout = 2000;
    let timeoutId: NodeJS.Timeout | null = null;
    let streamingFailed = false;

    try {
      const response = await fetch("/api/voice/stream", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ agentId, text }),
        signal: abortControllerRef.current?.signal,
      });
      if (!response.ok) {
        ttsUnavailableRef.current = true;
        if (silentFail) return;
        throw new Error("Failed to stream speech");
      }
      if (!response.body) {
        if (silentFail) return;
        throw new Error("No response body");
      }

      const mediaSource = new MediaSource();
      mediaSourceRef.current = mediaSource;
      if (audioRef.current) {
        audioRef.current.src = URL.createObjectURL(mediaSource);
      }

      const timeoutPromise = new Promise<void>((resolve, reject) => {
        timeoutId = setTimeout(() => {
          streamingFailed = true;
          if (silentFail) resolve();
          else reject(new Error("Streaming initialization timeout"));
        }, streamingTimeout);
      });

      await Promise.race([
        new Promise<void>((resolve, reject) => {
          const audio = audioRef.current;

          const onPlaybackEnded = () => {
            audio?.removeEventListener("ended", onPlaybackEnded);
            resolve();
          };

          const safetyMs = Math.max(15000, text.length * 120);
          const safetyTimer = setTimeout(() => {
            audio?.removeEventListener("ended", onPlaybackEnded);
            resolve();
          }, safetyMs);

          mediaSource.addEventListener("sourceopen", async () => {
            if (timeoutId) { clearTimeout(timeoutId); timeoutId = null; }
            try {
              const sourceBuffer = mediaSource.addSourceBuffer("audio/mpeg");
              const reader = response.body!.getReader();
              let playbackStarted = false;
              while (true) {
                if (streamingFailed) { reader.cancel(); break; }
                const { done, value } = await reader.read();
                if (done) break;
                if (sourceBuffer.updating) {
                  await new Promise<void>((res) => { sourceBuffer.addEventListener("updateend", () => res(), { once: true }); });
                }
                sourceBuffer.appendBuffer(value);
                if (!playbackStarted && audio) {
                  await new Promise<void>((res) => { sourceBuffer.addEventListener("updateend", () => res(), { once: true }); });
                  audio.play().catch(console.error);
                  playbackStarted = true;
                }
              }
              if (sourceBuffer.updating) {
                await new Promise<void>((res) => { sourceBuffer.addEventListener("updateend", () => res(), { once: true }); });
              }
              if (mediaSource.readyState === "open") mediaSource.endOfStream();
              if (audio) {
                audio.addEventListener("ended", onPlaybackEnded);
              } else {
                clearTimeout(safetyTimer);
                resolve();
              }
            } catch (err) {
              clearTimeout(safetyTimer);
              audio?.removeEventListener("ended", onPlaybackEnded);
              if (silentFail) resolve();
              else reject(err);
            }
          });
          mediaSource.addEventListener("error", () => {
            if (timeoutId) { clearTimeout(timeoutId); timeoutId = null; }
            clearTimeout(safetyTimer);
            audio?.removeEventListener("ended", onPlaybackEnded);
            if (silentFail) resolve();
            else reject(new Error("MediaSource error"));
          });
        }),
        timeoutPromise
      ]);
    } catch (err) {
      if (timeoutId) clearTimeout(timeoutId);
      if (mediaSourceRef.current) {
        try { if (mediaSourceRef.current.readyState === "open") mediaSourceRef.current.endOfStream(); } catch (e) {}
        mediaSourceRef.current = null;
      }
      if (silentFail) return;
      throw err;
    }
  };

  const preGenerateAudio = useCallback(async (text: string) => {
    if (!agentId || ttsUnavailableRef.current) return;
    pregenAbortRef.current?.abort();
    pregenAbortRef.current = new AbortController();
    const job = (async () => {
      try {
        const response = await fetch("/api/voice/speak", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ agentId, text }),
          signal: pregenAbortRef.current!.signal,
        });
        if (!response.ok) {
          ttsUnavailableRef.current = true;
          return;
        }
        const audioBlob = await response.blob();
        const audioUrl = URL.createObjectURL(audioBlob);
        pregenAudioRef.current = { url: audioUrl, text };
      } catch (err) {
        if (err instanceof Error && err.name === "AbortError") return;
        ttsUnavailableRef.current = true;
      }
    })();
    ttsGateRef.current = job;
    await job;
  }, [agentId]);

  // ───── Pipelining: pre-generate question + TTS during greeting ─────

  const pregenFirstQuestionForCompetency = useCallback(async (index: number) => {
    if (!onGenerateQuestion || !competencies || index >= competencies.length) return;
    pregenQuestionAbortRef.current?.abort();
    pregenQuestionAbortRef.current = new AbortController();

    const comp = competencies[index];
    const isFirst = index === 0;
    const previousCompetencyName = !isFirst ? competencies[index - 1]?.name : undefined;
    try {
      const question = await onGenerateQuestion(comp.competencyId, !isFirst, previousCompetencyName);
      if (pregenQuestionAbortRef.current?.signal.aborted) return;
      pregenQuestionRef.current = { competencyIndex: index, text: question };
      preGenerateAudio(question);
    } catch (err) {
      console.warn("Pre-generation of first question failed (will retry in startNextCompetency):", err);
    }
  }, [competencies, onGenerateQuestion, preGenerateAudio]);

  useEffect(() => { pregenFirstQuestionRef.current = pregenFirstQuestionForCompetency; }, [pregenFirstQuestionForCompetency]);

  // ───── Begin Conversation ─────

  const greetingDoneRef = useRef(false);

  const beginConversation = async () => {
    stateMachine.beginInterview();
    greetingDoneRef.current = false;

    const greeting = getGreetingText();
    addAiMessage(greeting);

    // Pipeline: start generating the first competency question while the greeting plays
    if (isCompetencyMode) {
      pregenFirstQuestionForCompetency(0);
    }

    if (pregenAudioRef.current && pregenAudioRef.current.text === greeting) {
      setCurrentSpokenText(greeting);
      setIsPlaying(true);
      if (audioRef.current) {
        audioRef.current.src = pregenAudioRef.current.url;
        await audioRef.current.play();
      }
      pregenAudioRef.current = null;
      if (onBegin) onBegin().catch(err => console.error("onBegin callback failed:", err));
      if (isCompetencyMode) greetingDoneRef.current = true;
      return;
    }

    if (onBegin) onBegin().catch(err => console.error("onBegin callback failed:", err));

    try {
      if (isCompetencyMode) greetingDoneRef.current = true;
      await speakText(greeting);
    } catch (err) {
      console.error("All speech methods failed for greeting, switching to text mode:", err);
      setIsPlaying(false);
      setCurrentSpokenText(null);
      setIsTextResponseMode(true);
      isTextResponseModeRef.current = true;
      // greetingDoneRef is left true so onTTSPlaybackCompleteRef detects competency
      // greeting and starts the first competency question instead of just listening.
      onTTSPlaybackCompleteRef.current?.();
    }
  };

  // ───── Practice Prompt ─────

  const handlePracticePrompt = useCallback(async (): Promise<void> => {
    const practiceText = "Please tell us about yourself in a few sentences.";
    return new Promise<void>((resolve) => {
      if (typeof window === "undefined" || !('speechSynthesis' in window)) { resolve(); return; }
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(practiceText);
      utterance.rate = 1.0;
      utterance.pitch = 1.0;
      utterance.volume = 1.0;
      utterance.onend = () => setTimeout(() => resolve(), 200);
      utterance.onerror = () => resolve();
      window.speechSynthesis.speak(utterance);
    });
  }, []);

  const handleRequestAccommodationFromPrep = useCallback(() => {
    setAccommodationRequested(true);
    setAccommodationModalOpen(true);
  }, []);

  const handleGoBack = useCallback(() => { stateMachine.goBack(); }, [stateMachine]);
  const handleRetry = useCallback(() => {
    stateMachine.retry();
    if (stateMachine.previousState === InterviewState.AI_SPEAKING ||
        stateMachine.previousState === InterviewState.LISTENING) {
      beginConversation();
    }
  }, [stateMachine]);

  // Pre-generate greeting audio
  useEffect(() => {
    const hasContent = isCompetencyMode ? competencies!.length > 0 : questionsArray.length > 0;
    if (agentId && hasContent) {
      preGenerateAudio(getGreetingText());
    }
  }, [agentId, getGreetingText, questionsArray.length, isCompetencyMode, competencies?.length]);

  const playAcknowledgment = useCallback(async (candidateMessage: string): Promise<void> => {
    if (typeof window === "undefined" || !("speechSynthesis" in window)) return;

    // Build a short, contextual acknowledgment from what the candidate said
    const firstWords = candidateMessage.split(/\s+/).slice(0, 5).join(" ");
    const hasEnough = candidateMessage.split(/\s+/).length > 8;

    const templates = hasEnough
      ? [
          `Okay, I hear you on ${firstWords}...`,
          `Right, interesting point about ${firstWords}...`,
          `Got it.`,
          `Okay, let me think about that.`,
        ]
      : [
          `Okay.`,
          `Got it, let me think.`,
          `Alright.`,
        ];

    const ack = templates[Math.floor(Math.random() * templates.length)];

    try {
      let voices = window.speechSynthesis.getVoices();
      if (voices.length === 0) {
        await new Promise<void>(r => {
          window.speechSynthesis.addEventListener("voiceschanged", () => r(), { once: true });
          setTimeout(r, 500);
        });
        voices = window.speechSynthesis.getVoices();
      }

      await new Promise<void>((resolve) => {
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(ack);
        utterance.rate = 1.1;
        utterance.pitch = 1.0;
        utterance.volume = 0.85;
        if (voices.length > 0) {
          const preferred = voices.find(v => v.lang.startsWith("en") && v.localService) || voices[0];
          utterance.voice = preferred;
        }
        utterance.onend = () => resolve();
        utterance.onerror = () => resolve();
        setTimeout(resolve, 2500);
        window.speechSynthesis.speak(utterance);
      });
    } catch {
      // Acknowledgment is optional -- never block the flow
    }
  }, []);

  // ───── Render ─────

  if (!isSupported) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
        <div className="text-center p-8">
          <h1 className="text-2xl font-bold text-white mb-4">Browser Not Supported</h1>
          <p className="text-slate-400">
            Voice interviews require a modern browser with microphone access.
            Please use a recent version of Chrome, Edge, Firefox, or Safari.
          </p>
        </div>
      </div>
    );
  }

  const isActiveConversation = [InterviewState.AI_SPEAKING, InterviewState.LISTENING, InterviewState.AI_THINKING].includes(state);

  return (
    <div className="min-h-screen bg-black flex flex-col">
      <audio ref={audioRef} className="hidden" />

      <main className="flex-1 flex flex-col items-center justify-center p-8 pb-24">
        <AnimatePresence mode="wait">
          {/* PREP State */}
          {state === InterviewState.PREP && (
            <motion.div
              key="prep"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="flex flex-col items-center justify-center max-w-2xl w-full text-center"
            >
              <AgentAvatar imageUrl={agentImageUrl} displayName={agentName} size="xl" className="mb-6" />
              <h2 className="text-2xl font-bold text-white mb-2">
                {applicantName !== "there" ? `Hi ${applicantName}, get ready to start` : "Get Ready to Start"}
              </h2>
              {jobTitle && (
                <p className="text-white/50 text-sm mb-4">
                  Interview for <span className="text-white/80 font-medium">{jobTitle}</span>
                </p>
              )}
              <p className="text-white/70 mb-6">
                ~5 minutes • {isCompetencyMode ? `${totalCompetencies} ${totalCompetencies === 1 ? "competency" : "competencies"}` : `${totalQuestions} ${totalQuestions === 1 ? "question" : "questions"}`} • You can request accommodations anytime.
              </p>

              <div className="w-full mb-6 p-4 bg-cyan-500/10 border border-cyan-500/30 rounded-lg text-left">
                <p className="text-white/90 text-sm mb-3">
                  This is a conversational interview. The AI will ask questions and listen to your spoken responses. Your audio and transcript may be reviewed for this role.
                </p>
                <div className="flex items-center gap-4 text-xs text-cyan-300/80">
                  <button onClick={() => window.open("#", "_blank")} className="hover:text-cyan-300 underline flex items-center gap-1">
                    Privacy <ExternalLink className="w-3 h-3" />
                  </button>
                  <button onClick={() => window.open("#", "_blank")} className="hover:text-cyan-300 underline flex items-center gap-1">
                    Data retention <ExternalLink className="w-3 h-3" />
                  </button>
                </div>
              </div>

              <div className="w-full mb-6 space-y-4">
                <div className={`rounded-xl p-4 space-y-4 border ${!micTestPassed && !accommodationRequested ? "bg-yellow-500/5 border-yellow-500/30" : "bg-white/5 border-white/10"}`}>
                  <h3 className="text-white/90 font-semibold text-left mb-3">Preflight Checklist</h3>
                  <MicTestCheck
                    onTestComplete={(passed) => setMicTestPassed(passed)}
                    onRequestAccommodation={handleRequestAccommodationFromPrep}
                  />
                  <div className="border-t border-white/10 pt-4">
                    <PracticeRecordingCheck onPracticePrompt={handlePracticePrompt} />
                  </div>
                </div>
              </div>

              <Button
                variant="outline"
                onClick={handleRequestAccommodationFromPrep}
                className="mb-6 border-white/20 !text-white hover:bg-white/10 hover:!text-white bg-transparent"
              >
                <HelpCircle className="w-4 h-4 mr-2" />
                Request Accommodation
              </Button>

              <div className="w-full space-y-3">
                {!micTestPassed && !accommodationRequested && (
                  <div className="p-3 bg-yellow-500/10 border border-yellow-500/30 rounded-lg">
                    <p className="text-sm text-yellow-300 font-medium mb-1">Complete the microphone test above to continue</p>
                    <p className="text-xs text-yellow-200/80">Click "Test Mic" in the Preflight Checklist, or request an accommodation if you need an alternative</p>
                  </div>
                )}
                <Button
                  size="lg"
                  onClick={beginConversation}
                  disabled={!micTestPassed && !accommodationRequested}
                  className="bg-emerald-600 hover:bg-emerald-700 text-white gap-2 disabled:opacity-50 disabled:cursor-not-allowed w-full"
                >
                  <Play className="w-5 h-5" />
                  Begin Interview
                </Button>
              </div>

              {onExit && (
                <Button variant="ghost" className="mt-4 text-white/40 hover:text-white/60" onClick={onExit}>Go Back</Button>
              )}
            </motion.div>
          )}

          {/* Active Conversation (AI_SPEAKING, LISTENING, AI_THINKING) */}
          {isActiveConversation && (
            <motion.div
              key="conversation"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex flex-col items-center w-full max-w-2xl"
            >
              {/* Progress indicator */}
              <div className="mb-6 text-center w-full">
                <p className="text-sm text-white/60 mb-2">{getQuestionIndicator()}</p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10 mx-auto" indicatorClassName="bg-emerald-500" />
              </div>

              {/* Agent header with status */}
              <div className="flex items-center gap-3 mb-6">
                <AgentAvatar imageUrl={agentImageUrl} displayName={agentName} size="md" />
                <div>
                  <h2 className="text-lg font-semibold text-white">{agentName}</h2>
                  <p className={`text-xs font-medium ${
                    state === InterviewState.AI_SPEAKING ? "text-cyan-400" :
                    state === InterviewState.LISTENING ? "text-emerald-400" :
                    "text-amber-400"
                  }`}>
                    {state === InterviewState.AI_SPEAKING ? "Speaking..." :
                     state === InterviewState.LISTENING ? "Listening..." :
                     "Thinking..."}
                  </p>
                </div>
              </div>

              {/* Current question text (persistent during candidate turn) */}
              {currentQuestionText && (state === InterviewState.LISTENING || state === InterviewState.AI_THINKING) && (
                <motion.div
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="w-full mb-4 px-5 py-4 bg-cyan-500/5 border border-cyan-500/20 rounded-xl"
                >
                  <p className="text-cyan-300/50 text-xs uppercase tracking-wide mb-1.5">Current Question</p>
                  <p className="text-white/90 text-base leading-relaxed">{currentQuestionText}</p>
                </motion.div>
              )}

              {/* Visualizer */}
              <div className="relative mb-4">
                <AudioVisualizer
                  audioElement={audioRef.current}
                  isPlaying={state === InterviewState.AI_SPEAKING && isPlaying}
                  isListening={state === InterviewState.LISTENING}
                  isProcessing={state === InterviewState.AI_THINKING}
                  size={200}
                />
                <div className="absolute inset-0 flex items-center justify-center">
                  {state === InterviewState.AI_SPEAKING && (
                    <div className="w-14 h-14 rounded-full flex items-center justify-center bg-cyan-500/40">
                      <Volume2 className="w-6 h-6 text-white/60" />
                    </div>
                  )}
                  {state === InterviewState.LISTENING && (
                    <div className="w-14 h-14 rounded-full flex items-center justify-center bg-emerald-500/40">
                      <Mic className="w-6 h-6 text-white" />
                    </div>
                  )}
                  {state === InterviewState.AI_THINKING && (
                    <motion.div
                      animate={{ rotate: 360 }}
                      transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
                    >
                      <Loader2 className="w-6 h-6 text-amber-400" />
                    </motion.div>
                  )}
                </div>
              </div>

              {/* Visual idle prompt */}
              <AnimatePresence>
                {idlePromptVisible && state === InterviewState.LISTENING && (
                  <motion.div
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    className="w-full mb-3 px-4 py-3 bg-amber-500/10 border border-amber-500/20 rounded-xl text-center"
                  >
                    <p className="text-amber-300/80 text-sm">Take your time, I&apos;m listening whenever you&apos;re ready.</p>
                  </motion.div>
                )}
              </AnimatePresence>

              {/* Conversation transcript area */}
              <div className="w-full bg-white/[0.03] border border-white/10 rounded-xl p-4 max-h-[40vh] overflow-y-auto mb-4">
                {conversationMessages.length === 0 && state === InterviewState.AI_SPEAKING && currentSpokenText && (
                  <div className="flex gap-3 mb-3">
                    <span className="text-cyan-400 text-xs font-medium shrink-0 mt-0.5 w-16 text-right">AI</span>
                    <p className="text-white/80 text-sm leading-relaxed">{currentSpokenText}</p>
                  </div>
                )}

                {conversationMessages.map((msg, idx) => (
                  <div key={idx} className={`flex gap-3 mb-3 ${msg.role === "candidate" ? "" : ""}`}>
                    <span className={`text-xs font-medium shrink-0 mt-0.5 w-16 text-right ${msg.role === "ai" ? "text-cyan-400" : "text-emerald-400"}`}>
                      {msg.role === "ai" ? agentName.split(" ")[0] : "You"}
                    </span>
                    <p className={`text-sm leading-relaxed ${msg.role === "ai" ? "text-white/80" : "text-white/60"} ${msg.isQuestion ? "font-medium text-white/90" : ""}`}>
                      {msg.text}
                    </p>
                  </div>
                ))}

                {/* AI currently speaking text */}
                {state === InterviewState.AI_SPEAKING && currentSpokenText && conversationMessages.length > 0 &&
                  conversationMessages[conversationMessages.length - 1]?.text !== currentSpokenText && (
                  <div className="flex gap-3 mb-3">
                    <span className="text-cyan-400 text-xs font-medium shrink-0 mt-0.5 w-16 text-right">{agentName.split(" ")[0]}</span>
                    <p className="text-white/80 text-sm leading-relaxed">{currentSpokenText}</p>
                  </div>
                )}

                {/* Live candidate transcript */}
                {state === InterviewState.LISTENING && interimText && (
                  <div className="flex gap-3 mb-3">
                    <span className="text-emerald-400 text-xs font-medium shrink-0 mt-0.5 w-16 text-right">You</span>
                    <p className="text-white/60 text-sm leading-relaxed">
                      {interimText}
                    </p>
                  </div>
                )}

                {/* Thinking indicator */}
                {state === InterviewState.AI_THINKING && (
                  <div className="flex gap-3 mb-3">
                    <span className="text-amber-400 text-xs font-medium shrink-0 mt-0.5 w-16 text-right">{agentName.split(" ")[0]}</span>
                    <div className="flex gap-1 items-center">
                      <div className="w-1.5 h-1.5 rounded-full bg-amber-400/60 animate-bounce" style={{ animationDelay: "0ms" }} />
                      <div className="w-1.5 h-1.5 rounded-full bg-amber-400/60 animate-bounce" style={{ animationDelay: "150ms" }} />
                      <div className="w-1.5 h-1.5 rounded-full bg-amber-400/60 animate-bounce" style={{ animationDelay: "300ms" }} />
                    </div>
                  </div>
                )}

                <div ref={conversationEndRef} />
              </div>

              {/* Text response fallback */}
              {isTextResponseMode && state === InterviewState.LISTENING && (
                <motion.div initial={{ y: 20, opacity: 0 }} animate={{ y: 0, opacity: 1 }} className="w-full">
                  <textarea
                    value={textResponse}
                    onChange={(e) => setTextResponse(e.target.value)}
                    placeholder="Type your answer here..."
                    className="w-full px-4 py-3 bg-white/10 border border-white/20 rounded-lg text-white placeholder:text-white/40 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 resize-none"
                    rows={4}
                    aria-label="Text response input"
                  />
                  <div className="mt-3 flex gap-3">
                    <Button onClick={handleTextSubmit} disabled={!textResponse.trim()} className="bg-emerald-600 hover:bg-emerald-700 text-white">
                      Submit Answer
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setIsTextResponseMode(false);
                        isTextResponseModeRef.current = false;
                        setTextResponse("");
                        startRecording();
                      }}
                      className="text-white/60 hover:text-white hover:bg-white/10"
                    >
                      Switch to Voice
                    </Button>
                  </div>
                </motion.div>
              )}

              {/* Listening duration */}
              {state === InterviewState.LISTENING && !isTextResponseMode && (
                <p className="text-white/30 text-xs mt-2">
                  Listening... {formatDuration(stateMachine.listeningDuration)}
                </p>
              )}
            </motion.div>
          )}

          {/* COMPLETE State */}
          {state === InterviewState.COMPLETE && (
            <motion.div
              key="complete"
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="text-center max-w-2xl w-full"
            >
              <div className="mb-8">
                <div className="w-20 h-20 rounded-full bg-emerald-600/20 flex items-center justify-center mx-auto mb-6">
                  <PhoneOff className="w-10 h-10 text-emerald-400" />
                </div>
                <h2 className="text-3xl font-bold text-white mb-4">Interview Complete</h2>
                <p className="text-slate-400">Thank you for completing your interview! Your responses have been submitted.</p>
              </div>

              <div className="mb-6">
                <p className="text-sm text-white/60 mb-2">
                  {isCompetencyMode
                    ? `${completedCompetencies.length} of ${totalCompetencies} competencies completed`
                    : `${totalQuestions} of ${totalQuestions} questions completed`}
                </p>
                <Progress value={100} className="w-48 h-1.5 bg-white/10 mx-auto" indicatorClassName="bg-emerald-500" />
              </div>

              {isCompetencyMode && completedCompetencies.length > 0 && (
                <div className="mb-8 max-h-[50vh] overflow-y-auto space-y-4 text-left px-2">
                  {completedCompetencies.map((block) => (
                    <div key={block.competencyId} className="bg-white/5 border border-white/10 rounded-xl p-5">
                      <div className="mb-3">
                        <h3 className="text-white font-semibold text-base">{block.competencyName}</h3>
                      </div>
                      <div className="space-y-3">
                        {block.exchanges.map((exchange, idx) => (
                          <div key={idx} className="space-y-1.5">
                            <div className="flex gap-2">
                              <span className="text-cyan-400 text-xs font-medium shrink-0 mt-0.5">{exchange.type === "primary" ? "Q" : `F${idx}`}</span>
                              <p className="text-white/80 text-sm">{exchange.question}</p>
                            </div>
                            <div className="flex gap-2 pl-0.5">
                              <span className="text-emerald-400 text-xs font-medium shrink-0 mt-0.5">A</span>
                              <p className="text-white/60 text-sm">{exchange.response}</p>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {onExit && (
                <Button onClick={onExit} className="bg-emerald-600 hover:bg-emerald-700 text-white">Continue</Button>
              )}
            </motion.div>
          )}

          {/* ERROR State */}
          {state === InterviewState.ERROR && (
            <motion.div
              key="error"
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="flex flex-col items-center justify-center max-w-md text-center"
            >
              <div className="w-16 h-16 rounded-full bg-red-500/20 flex items-center justify-center mb-6">
                <AlertCircle className="w-8 h-8 text-red-400" />
              </div>
              <h2 className="text-2xl font-bold text-white mb-2">Something went wrong</h2>
              <p className="text-white/70 mb-6">{stateMachine.errorMessage}</p>
              {stateMachine.errorType === "mic_permission" && (
                <div className="mb-6 px-4 py-3 bg-white/5 rounded-lg text-left text-sm text-white/60">
                  <p className="font-medium text-white/80 mb-2">How to enable microphone:</p>
                  <ol className="list-decimal list-inside space-y-1">
                    <li>Click the lock/info icon in your browser's address bar</li>
                    <li>Find "Microphone" in the permissions list</li>
                    <li>Change it to "Allow"</li>
                    <li>Refresh this page</li>
                  </ol>
                </div>
              )}
              <div className="flex gap-3">
                <Button variant="outline" onClick={handleGoBack} className="border-white/20 !text-white hover:bg-white/10 hover:!text-white bg-transparent">
                  <ArrowLeft className="w-4 h-4 mr-2" />Back
                </Button>
                <Button onClick={handleRetry} className="bg-emerald-600 hover:bg-emerald-700 text-white">
                  <RotateCcw className="w-4 h-4 mr-2" />Try Again
                </Button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </main>

      {/* Controls Bar */}
      <InterviewControlsBar
        state={state}
        repeatsRemaining={repeatsRemaining}
        onRepeatQuestion={handleRepeatQuestion}
        onOpenAccommodationModal={() => setAccommodationModalOpen(true)}
      />

      {/* Accommodation Modal */}
      <InterviewAccommodationModal
        open={accommodationModalOpen}
        onOpenChange={setAccommodationModalOpen}
        onRepeatQuestion={handleRepeatQuestion}
        onSwitchToTextResponse={handleSwitchToTextResponse}
        onHumanAlternative={() => {
          setAccommodationRequested(true);
          setMicTestPassed(true);
        }}
        onTechnicalHelp={() => {}}
      />
    </div>
  );
}
