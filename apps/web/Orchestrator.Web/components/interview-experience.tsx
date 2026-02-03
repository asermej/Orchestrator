"use client";

import { useState, useCallback, useRef, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Mic, Play, PhoneOff, Volume2, Square, Pause, RotateCcw, ArrowLeft, Loader2, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { useInterviewStateMachine, InterviewState, formatDuration } from "@/hooks/use-interview-state-machine";
import { AgentAvatar } from "@/components/agent-avatar";
import { AudioVisualizer } from "@/components/audio-visualizer";
import { InterviewControlsBar } from "@/components/interview-controls-bar";
import { InterviewAccommodationModal } from "@/components/interview-accommodation-modal";
import { MicTestCheck } from "@/components/mic-test-check";
import { PracticeRecordingCheck } from "@/components/practice-recording-check";
import { Eye, EyeOff, HelpCircle, ExternalLink } from "lucide-react";

export interface InterviewQuestion {
  id: string;
  text: string;
}

export interface InterviewExperienceProps {
  questions: InterviewQuestion[];
  agentId?: string;
  agentName: string;
  agentImageUrl?: string;
  applicantName?: string;
  onSaveResponse: (questionId: string, questionText: string, transcript: string, order: number) => Promise<void>;
  onComplete: () => Promise<void>;
  onExit?: () => void;
  onBegin?: () => Promise<void>;
}

/**
 * Shared interview experience component that handles the full voice interview flow.
 * Used by both the real interview page and the test interview page.
 */
export function InterviewExperience({
  questions,
  agentId,
  agentName,
  agentImageUrl,
  applicantName = "there",
  onSaveResponse,
  onComplete,
  onExit,
  onBegin,
}: InterviewExperienceProps) {
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [transcript, setTranscript] = useState("");
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentSpokenText, setCurrentSpokenText] = useState<string | null>(null);
  const [processingMessage, setProcessingMessage] = useState<string>("Saving your response");
  const [playbackSpeed, setPlaybackSpeed] = useState<1.0 | 0.8>(1.0);
  const [showCaptions, setShowCaptions] = useState(() => {
    // Load captions preference from localStorage, default to true
    if (typeof window !== "undefined") {
      const saved = localStorage.getItem("interview-captions-preference");
      return saved !== null ? saved === "true" : true;
    }
    return true;
  });
  const [isTextResponseMode, setIsTextResponseMode] = useState(false);
  const [textResponse, setTextResponse] = useState("");
  const [accommodationModalOpen, setAccommodationModalOpen] = useState(false);
  const [repeatsRemaining, setRepeatsRemaining] = useState(1); // Max 1 repeat per question
  const [micTestPassed, setMicTestPassed] = useState(false);
  const [accommodationRequested, setAccommodationRequested] = useState(false);
  
  const transcriptRef = useRef("");
  const audioRef = useRef<HTMLAudioElement>(null);
  const mediaSourceRef = useRef<MediaSource | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const pregenAudioRef = useRef<{ url: string; text: string } | null>(null);
  const pregenAbortRef = useRef<AbortController | null>(null);
  const isTextResponseModeRef = useRef(false);
  
  // State machine
  const stateMachine = useInterviewStateMachine();
  const { state } = stateMachine;
  
  // Check if MediaSource is supported for streaming playback
  const supportsMediaSource = typeof window !== "undefined" && "MediaSource" in window;

  // Apply playback speed to audio element
  useEffect(() => {
    if (audioRef.current) {
      audioRef.current.playbackRate = playbackSpeed;
    }
  }, [playbackSpeed]);

  const currentQuestion = questions[currentQuestionIndex];
  const isLastQuestion = currentQuestionIndex >= questions.length - 1;
  const totalQuestions = questions.length;
  const progressPercent = totalQuestions > 0 ? ((currentQuestionIndex + 1) / totalQuestions) * 100 : 0;
  
  // Pre-generate greeting text
  const getGreetingText = useCallback(() => {
    const question = questions[0];
    return `Hello ${applicantName}! I'm ${agentName}, and I'll be conducting your interview today. Let's get started with our first question. ${question?.text || "Tell me about yourself."}`;
  }, [applicantName, agentName, questions]);

  const handleTranscript = useCallback((text: string) => {
    setTranscript((prev) => {
      const newTranscript = prev + " " + text;
      transcriptRef.current = newTranscript.trim();
      return newTranscript.trim();
    });
  }, []);

  const {
    isRecording,
    isSupported,
    error: voiceError,
    startRecording,
    stopRecording,
  } = useVoiceInput({
    onTranscript: handleTranscript,
    onError: (err) => {
      if (err.includes("denied") || err.includes("permission")) {
        stateMachine.handleError("mic_permission", err);
      } else if (err.includes("microphone") || err.includes("audio")) {
        stateMachine.handleError("audio_capture", err);
      } else if (err.includes("No speech")) {
        // Ignore "no-speech" errors when in text response mode - user is typing, not speaking
        if (isTextResponseModeRef.current) {
          return; // Silently ignore - user is using text input
        }
        
        // Handle "no-speech" timeout gracefully - auto-restart recording instead of showing error
        // This happens when user takes too long to start speaking (browser timeout ~5-10s)
        // The browser's SpeechRecognition API times out after ~5-10s of silence even with continuous mode
        // We automatically restart so the user can continue speaking without interruption
        const currentState = stateMachine.state;
        if (currentState === InterviewState.RECORDING) {
          // Silently restart recording - user can still speak
          setTimeout(() => {
            // Check state again to ensure we're still recording before restarting
            // Also check we're not in text mode (user might have switched)
            if (stateMachine.state === InterviewState.RECORDING && !isTextResponseModeRef.current) {
              startRecording();
            }
          }, 500);
        } else {
          // Only show error if not actively recording (shouldn't happen, but safety check)
          stateMachine.handleError("no_speech", err);
        }
      } else {
        stateMachine.handleError("network", err);
      }
    },
    continuous: true,
    interimResults: false,
  });

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
      pregenAbortRef.current?.abort();
      if (mediaSourceRef.current?.readyState === "open") {
        try {
          mediaSourceRef.current.endOfStream();
        } catch (e) {
          // Ignore errors during cleanup
        }
      }
      if (pregenAudioRef.current?.url) {
        URL.revokeObjectURL(pregenAudioRef.current.url);
      }
    };
  }, []);

  // Handle audio ended - auto-transition to RECORDING
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;

    const handleEnded = () => {
      // Only handle ended event if we're actually playing and in QUESTION_PLAYING state
      if (state === InterviewState.QUESTION_PLAYING && isPlaying) {
        setIsPlaying(false);
        setCurrentSpokenText(null);
        stateMachine.onTTSEnd();
        setTimeout(() => startListening(), 300);
      }
    };

    audio.addEventListener("ended", handleEnded);
    return () => audio.removeEventListener("ended", handleEnded);
  }, [state, stateMachine, isPlaying]);

  // Pre-generate next question audio when user starts recording
  useEffect(() => {
    if (state === InterviewState.RECORDING && !isLastQuestion) {
      const nextQuestionIndex = currentQuestionIndex + 1;
      const nextQuestion = questions[nextQuestionIndex];
      if (nextQuestion) {
        preGenerateAudio(nextQuestion.text);
      }
    }
  }, [state, currentQuestionIndex, isLastQuestion, questions]);

  const startListening = useCallback(() => {
    setTranscript("");
    transcriptRef.current = "";
    startRecording();
  }, [startRecording]);

  const stopListeningAndFinish = useCallback(() => {
    stopRecording();
    stateMachine.finishAnswer();
  }, [stopRecording, stateMachine]);

  const handleSubmitAnswer = useCallback(async () => {
    const message = transcriptRef.current.trim();
    
    if (!message) {
      stateMachine.handleError("no_speech", "No response provided. Please try again.");
      return;
    }

    stateMachine.startProcessing();
    setProcessingMessage("Saving your response");

    // Save the response (don't fail the interview if this fails)
    try {
      await onSaveResponse(
        currentQuestion.id,
        currentQuestion.text,
        message,
        currentQuestionIndex
      );
    } catch (err) {
      // Log but continue - the interview flow is more important than persisting each response
      console.warn("Failed to save response (continuing anyway):", err);
    }

    // Move to next question or complete
    if (isLastQuestion) {
      await handleComplete();
    } else {
      const nextIndex = currentQuestionIndex + 1;
      const nextQuestionText = questions[nextIndex].text;
      
      // Update question index and clear transcript BEFORE state transition
      setCurrentQuestionIndex(nextIndex);
      setTranscript("");
      transcriptRef.current = "";
      
      // Check if we have pre-generated audio
      if (pregenAudioRef.current && pregenAudioRef.current.text === nextQuestionText) {
        console.log("Using pre-generated audio for next question");
        
        // Transition to QUESTION_PLAYING state before playing audio
        stateMachine.completeProcessing(false);
        
        setCurrentSpokenText(nextQuestionText);
        setIsPlaying(true);
        
        if (audioRef.current) {
          audioRef.current.src = pregenAudioRef.current.url;
          await audioRef.current.play();
        }
        
        pregenAudioRef.current = null;
      } else {
        // Transition to QUESTION_PLAYING state before loading audio
        stateMachine.completeProcessing(false);
        
        // Ensure audio element is ready and previous audio is stopped
        // Don't clear src as it might trigger ended event
        if (audioRef.current) {
          audioRef.current.pause();
          audioRef.current.currentTime = 0;
        }
        
        // Small delay to ensure state transition completes before audio starts
        await new Promise(resolve => setTimeout(resolve, 50));
        
        await speakText(nextQuestionText);
      }
    }
  }, [currentQuestion, currentQuestionIndex, isLastQuestion, questions, onSaveResponse, stateMachine]);

  const handleComplete = useCallback(async () => {
    stateMachine.completeProcessing(true);
    
    try {
      await onComplete();
    } catch (err) {
      console.error("Failed to complete interview:", err);
    }

    // Thank the applicant
    const closing = `Thank you for completing this interview, ${applicantName}! We appreciate your time and will be in touch soon with next steps. Have a great day!`;
    await speakText(closing);
  }, [applicantName, onComplete, stateMachine]);

  const handleReRecord = useCallback(async () => {
    setTranscript("");
    transcriptRef.current = "";
    stateMachine.reRecord();
    if (currentQuestion) {
      await speakText(currentQuestion.text);
    }
  }, [currentQuestion, stateMachine]);

  const stopPlayback = useCallback(() => {
    abortControllerRef.current?.abort();
    
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
    
    if (mediaSourceRef.current?.readyState === "open") {
      try {
        mediaSourceRef.current.endOfStream();
      } catch (e) {
        // Ignore errors during cleanup
      }
    }
    mediaSourceRef.current = null;
    
    setIsPlaying(false);
  }, []);

  const handleRepeatQuestion = useCallback(async () => {
    // Check if repeat is available
    if (repeatsRemaining <= 0) {
      return; // Button should be disabled, but guard against it
    }
    
    // Consume a repeat
    setRepeatsRemaining((prev) => Math.max(0, prev - 1));
    
    const wasRecording = state === InterviewState.RECORDING;
    
    if (wasRecording) {
      stopRecording();
      stateMachine.pauseRecording();
    }
    
    // Stop current playback
    stopPlayback();
    
    // Replay current question
    if (currentQuestion) {
      await speakText(currentQuestion.text);
    }
    
    // If was recording, will auto-resume via onTTSEnd -> startListening
  }, [state, currentQuestion, stopRecording, stateMachine, stopPlayback, repeatsRemaining]);

  const handleTogglePlaybackSpeed = useCallback(() => {
    setPlaybackSpeed((prev) => (prev === 1.0 ? 0.8 : 1.0));
  }, []);

  const handleToggleCaptions = useCallback(() => {
    setShowCaptions((prev) => {
      const newValue = !prev;
      // Save preference to localStorage
      if (typeof window !== "undefined") {
        localStorage.setItem("interview-captions-preference", String(newValue));
      }
      return newValue;
    });
  }, []);

  const handleSwitchToTextResponse = useCallback(() => {
    setIsTextResponseMode(true);
    isTextResponseModeRef.current = true;
    setTextResponse("");
    if (state === InterviewState.RECORDING) {
      stopRecording();
      stateMachine.pauseRecording();
    }
    // If in PREP state, allow proceeding
    if (state === InterviewState.PREP) {
      setAccommodationRequested(true);
      setMicTestPassed(true);
    }
  }, [state, stopRecording, stateMachine]);

  // Reset text response mode and repeats when moving to next question
  useEffect(() => {
    if (state === InterviewState.QUESTION_PLAYING) {
      setIsTextResponseMode(false);
      isTextResponseModeRef.current = false;
      setTextResponse("");
      // Reset repeats for new question
      setRepeatsRemaining(1);
    }
  }, [state]);

  // Keep ref in sync with state
  useEffect(() => {
    isTextResponseModeRef.current = isTextResponseMode;
  }, [isTextResponseMode]);

  const speakText = async (text: string) => {
    // Only abort previous requests if we're not already in QUESTION_PLAYING state
    // This prevents interrupting audio that's about to play
    if (state !== InterviewState.QUESTION_PLAYING) {
      abortControllerRef.current?.abort();
    }
    abortControllerRef.current = new AbortController();
    
    setCurrentSpokenText(text);
    setIsPlaying(true);
    
    // If no agent configured, show text fallback
    if (!agentId) {
      const readingTime = Math.max(3000, text.length * 50);
      setTimeout(() => {
        setIsPlaying(false);
        setCurrentSpokenText(null);
        if (state !== InterviewState.COMPLETE) {
          stateMachine.onTTSEnd();
          startListening();
        }
      }, readingTime);
      return;
    }
    
    if (supportsMediaSource) {
      try {
        await speakTextStreaming(text);
        return;
      } catch (err) {
        console.warn("Streaming playback failed or timed out, falling back to blob:", err);
        // Continue to blob fallback below
      }
    }
    
    try {
      const response = await fetch("/api/voice/speak", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          agentId,
          text 
        }),
        signal: abortControllerRef.current.signal,
      });

      if (!response.ok) {
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
      console.error("Speech error:", err);
      stateMachine.handleError("audio_playback", "Failed to play audio. Please try again.");
    }
  };
  
  const speakTextStreaming = async (text: string) => {
    if (!agentId) return;
    
    // Set timeout for streaming initialization (2 seconds)
    const streamingTimeout = 2000;
    let timeoutId: NodeJS.Timeout | null = null;
    let streamingFailed = false;
    
    try {
      const response = await fetch("/api/voice/stream", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          agentId,
          text 
        }),
        signal: abortControllerRef.current?.signal,
      });

      if (!response.ok) {
        throw new Error("Failed to stream speech");
      }

      if (!response.body) {
        throw new Error("No response body");
      }

      const mediaSource = new MediaSource();
      mediaSourceRef.current = mediaSource;

      if (audioRef.current) {
        audioRef.current.src = URL.createObjectURL(mediaSource);
      }

      // Set timeout for MediaSource initialization
      const timeoutPromise = new Promise<void>((_, reject) => {
        timeoutId = setTimeout(() => {
          streamingFailed = true;
          reject(new Error("Streaming initialization timeout"));
        }, streamingTimeout);
      });

      await Promise.race([
        new Promise<void>((resolve, reject) => {
          mediaSource.addEventListener("sourceopen", async () => {
            if (timeoutId) {
              clearTimeout(timeoutId);
              timeoutId = null;
            }
            
            try {
              const sourceBuffer = mediaSource.addSourceBuffer("audio/mpeg");
              const reader = response.body!.getReader();
              let playbackStarted = false;

              while (true) {
                if (streamingFailed) {
                  reader.cancel();
                  break;
                }
                
                const { done, value } = await reader.read();
                
                if (done) {
                  break;
                }

                if (sourceBuffer.updating) {
                  await new Promise<void>((res) => {
                    sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                  });
                }

                sourceBuffer.appendBuffer(value);

                if (!playbackStarted && audioRef.current) {
                  await new Promise<void>((res) => {
                    sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                  });
                  
                  audioRef.current.play().catch(console.error);
                  playbackStarted = true;
                }
              }

              if (sourceBuffer.updating) {
                await new Promise<void>((res) => {
                  sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                });
              }

              if (mediaSource.readyState === "open") {
                mediaSource.endOfStream();
              }

              resolve();
            } catch (err) {
              reject(err);
            }
          });

          mediaSource.addEventListener("error", () => {
            if (timeoutId) {
              clearTimeout(timeoutId);
              timeoutId = null;
            }
            reject(new Error("MediaSource error"));
          });
        }),
        timeoutPromise
      ]);
    } catch (err) {
      // Clean up timeout
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
      
      // Clean up MediaSource if it was created
      if (mediaSourceRef.current) {
        try {
          if (mediaSourceRef.current.readyState === "open") {
            mediaSourceRef.current.endOfStream();
          }
        } catch (e) {
          // Ignore cleanup errors
        }
        mediaSourceRef.current = null;
      }
      
      // Re-throw to trigger fallback in speakText
      throw err;
    }
  };

  const preGenerateAudio = useCallback(async (text: string) => {
    if (!agentId) return;
    
    pregenAbortRef.current?.abort();
    pregenAbortRef.current = new AbortController();
    
    try {
      const response = await fetch("/api/voice/speak", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          agentId,
          text 
        }),
        signal: pregenAbortRef.current.signal,
      });

      if (!response.ok) {
        console.warn("Pre-generation failed:", response.status);
        return;
      }

      const audioBlob = await response.blob();
      const audioUrl = URL.createObjectURL(audioBlob);
      pregenAudioRef.current = { url: audioUrl, text };
      console.log("Pre-generated audio ready for:", text.substring(0, 50) + "...");
    } catch (err) {
      if (err instanceof Error && err.name === "AbortError") return;
      console.warn("Pre-generation error:", err);
    }
  }, [agentId]);

  const beginConversation = async () => {
    // Clear any practice data before starting
    // (Practice data is already ephemeral, but ensure cleanup)
    
    // Start state machine transition immediately (don't wait for onBegin)
    stateMachine.beginInterview();
    
    const greeting = getGreetingText();
    
    // Prioritize pre-generated blob audio for instant playback (no MediaSource overhead)
    if (pregenAudioRef.current && pregenAudioRef.current.text === greeting) {
      console.log("Using pre-generated audio for instant playback");
      setCurrentSpokenText(greeting);
      setIsPlaying(true);
      
      if (audioRef.current) {
        audioRef.current.src = pregenAudioRef.current.url;
        await audioRef.current.play();
      }
      
      pregenAudioRef.current = null;
      
      // Call onBegin callback in parallel (non-blocking)
      if (onBegin) {
        onBegin().catch(err => console.error("onBegin callback failed:", err));
      }
      return;
    }
    
    // Fall back to streaming if pre-generation failed or not ready
    // Call onBegin in parallel (non-blocking)
    if (onBegin) {
      onBegin().catch(err => console.error("onBegin callback failed:", err));
    }
    
    await speakText(greeting);
  };

  // Practice prompt handler
  const handlePracticePrompt = useCallback(async () => {
    const practiceText = "Please tell us about yourself in a few sentences.";
    await speakText(practiceText);
  }, []);

  // Handle accommodation request from PREP
  const handleRequestAccommodationFromPrep = useCallback(() => {
    setAccommodationRequested(true);
    setAccommodationModalOpen(true);
  }, []);

  // Handle switch to text response from accommodation modal
  const handleSwitchToTextResponseFromPrep = useCallback(() => {
    setAccommodationRequested(true);
    setIsTextResponseMode(true);
    isTextResponseModeRef.current = true;
    // Allow proceeding even without mic test
    setMicTestPassed(true);
  }, []);

  const handlePauseRecording = useCallback(() => {
    stopRecording();
    stateMachine.pauseRecording();
  }, [stopRecording, stateMachine]);

  const handleResumeRecording = useCallback(() => {
    stateMachine.resumeRecording();
    startRecording();
  }, [startRecording, stateMachine]);

  const handleGoBack = useCallback(() => {
    stateMachine.goBack();
  }, [stateMachine]);

  const handleRetry = useCallback(() => {
    stateMachine.retry();
    if (stateMachine.previousState === InterviewState.QUESTION_PLAYING || 
        stateMachine.previousState === InterviewState.RECORDING) {
      beginConversation();
    }
  }, [stateMachine]);

  // Pre-generate greeting audio immediately when agentId is available (not waiting for PREP state)
  useEffect(() => {
    if (agentId && questions.length > 0) {
      const greetingText = getGreetingText();
      preGenerateAudio(greetingText);
    }
  }, [agentId, getGreetingText, questions.length]);

  if (!isSupported) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
        <div className="text-center p-8">
          <h1 className="text-2xl font-bold text-white mb-4">Browser Not Supported</h1>
          <p className="text-slate-400">
            Voice interviews require a browser with speech recognition support.
            Please use Chrome, Edge, or Safari.
          </p>
        </div>
      </div>
    );
  }

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
              <AgentAvatar
                imageUrl={agentImageUrl}
                displayName={agentName}
                size="xl"
                className="mb-6"
              />
              
              <h2 className="text-2xl font-bold text-white mb-4">
                Get Ready to Start
              </h2>
              
              {/* Expectations Line */}
              <p className="text-white/70 mb-6">
                ~5 minutes • {totalQuestions} {totalQuestions === 1 ? "question" : "questions"} • You can request accommodations anytime.
              </p>

              {/* AI Interviewer Notice Card */}
              <div className="w-full mb-6 p-4 bg-cyan-500/10 border border-cyan-500/30 rounded-lg text-left">
                <p className="text-white/90 text-sm mb-3">
                  You'll answer by voice. Your audio and transcript may be reviewed for this role.
                </p>
                <div className="flex items-center gap-4 text-xs text-cyan-300/80">
                  <button
                    onClick={() => {
                      // Placeholder for Privacy link
                      window.open("#", "_blank");
                    }}
                    className="hover:text-cyan-300 underline flex items-center gap-1"
                  >
                    Privacy
                    <ExternalLink className="w-3 h-3" />
                  </button>
                  <button
                    onClick={() => {
                      // Placeholder for Data retention link
                      window.open("#", "_blank");
                    }}
                    className="hover:text-cyan-300 underline flex items-center gap-1"
                  >
                    Data retention
                    <ExternalLink className="w-3 h-3" />
                  </button>
                </div>
              </div>

              {/* Preflight Checklist */}
              <div className="w-full mb-6 space-y-4">
                <div className="bg-white/5 rounded-xl p-4 space-y-4 border border-white/10">
                  <h3 className="text-white/90 font-semibold text-left mb-3">Preflight Checklist</h3>
                  
                  {/* Mic Test */}
                  <MicTestCheck
                    onTestComplete={(passed) => {
                      setMicTestPassed(passed);
                    }}
                    onRequestAccommodation={handleRequestAccommodationFromPrep}
                  />

                  {/* Practice Recording */}
                  <div className="border-t border-white/10 pt-4">
                    <PracticeRecordingCheck
                      agentId={agentId}
                      onPracticePrompt={handlePracticePrompt}
                    />
                  </div>

                  {/* Captions Toggle */}
                  <div className="border-t border-white/10 pt-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        {showCaptions ? (
                          <Eye className="w-5 h-5 text-white/70" />
                        ) : (
                          <EyeOff className="w-5 h-5 text-white/70" />
                        )}
                        <span className="text-white/90 font-medium">Captions</span>
                      </div>
                      <Button
                        onClick={handleToggleCaptions}
                        variant="outline"
                        size="sm"
                        className="border-white/20 !text-white hover:bg-white/10 hover:!text-white bg-transparent"
                      >
                        {showCaptions ? "Turn Off" : "Turn On"}
                      </Button>
                    </div>
                    <p className="text-xs text-white/50 mt-2 text-left">
                      Show captions for questions and your responses
                    </p>
                  </div>
                </div>
              </div>

              {/* Accommodation Entry */}
              <Button
                variant="outline"
                onClick={handleRequestAccommodationFromPrep}
                className="mb-6 border-white/20 !text-white hover:bg-white/10 hover:!text-white bg-transparent"
              >
                <HelpCircle className="w-4 h-4 mr-2" />
                Request Accommodation
              </Button>

              {/* Begin Interview Button */}
              <Button
                size="lg"
                onClick={beginConversation}
                disabled={!micTestPassed && !accommodationRequested}
                className="bg-emerald-600 hover:bg-emerald-700 text-white gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Play className="w-5 h-5" />
                Begin Interview
              </Button>
              {!micTestPassed && !accommodationRequested && (
                <p className="text-xs text-white/50 mt-2">
                  Please complete the microphone test or request an accommodation to continue
                </p>
              )}
              
              {onExit && (
                <Button
                  variant="ghost"
                  className="mt-4 text-white/40 hover:text-white/60"
                  onClick={onExit}
                >
                  Go Back
                </Button>
              )}
            </motion.div>
          )}

          {/* QUESTION_PLAYING State */}
          {state === InterviewState.QUESTION_PLAYING && (
            <motion.div
              key="question-playing"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex flex-col items-center justify-center"
            >
              <div className="mb-6 text-center">
                <p className="text-sm text-white/60 mb-2">
                  Question {currentQuestionIndex + 1} of {totalQuestions}
                </p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10" />
              </div>

              <motion.div
                initial={{ y: -20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                className="flex flex-col items-center mb-8"
              >
                <AgentAvatar
                  imageUrl={agentImageUrl}
                  displayName={agentName}
                  size="lg"
                />
                <h2 className="mt-4 text-xl font-semibold text-white">
                  {agentName}
                </h2>
                <p className="text-sm text-cyan-400 font-medium">
                  Speaking...
                </p>
              </motion.div>

              {currentSpokenText && showCaptions && (
                <motion.div
                  initial={{ y: -20, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  className="mb-6 px-6 py-4 bg-white/5 rounded-xl max-w-md border border-white/10"
                >
                  <p className="text-white/90 text-center text-lg leading-relaxed select-none">
                    "{currentSpokenText}"
                  </p>
                </motion.div>
              )}

              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                className="relative"
              >
                <AudioVisualizer
                  audioElement={audioRef.current}
                  isPlaying={isPlaying}
                  isListening={false}
                  isProcessing={false}
                  size={300}
                />

                <div className="absolute inset-0 flex items-center justify-center">
                  <motion.div className="w-20 h-20 rounded-full flex items-center justify-center bg-cyan-500/40 cursor-not-allowed">
                    <Volume2 className="w-8 h-8 text-white/60" />
                  </motion.div>
                </div>
              </motion.div>

              <p className="mt-8 text-white/40 text-xs">
                Please wait for the question to finish...
              </p>
            </motion.div>
          )}

          {/* RECORDING State */}
          {state === InterviewState.RECORDING && (
            <motion.div
              key="recording"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex flex-col items-center justify-center"
            >
              <div className="mb-6 text-center">
                <p className="text-sm text-white/60 mb-2">
                  Question {currentQuestionIndex + 1} of {totalQuestions}
                </p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10" />
              </div>

              <motion.div
                initial={{ scale: 0.9, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                className="mb-4 flex items-center gap-2"
              >
                <div className="w-3 h-3 rounded-full bg-red-500 animate-pulse" />
                <span className="text-2xl font-mono text-white">
                  {formatDuration(stateMachine.recordingDuration)}
                </span>
              </motion.div>

              <motion.div
                initial={{ y: -20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                className="flex flex-col items-center mb-6"
              >
                <AgentAvatar
                  imageUrl={agentImageUrl}
                  displayName={agentName}
                  size="lg"
                />
                <h2 className="mt-4 text-xl font-semibold text-white">
                  {agentName}
                </h2>
                <p className="text-sm text-emerald-400 font-medium">
                  Recording your answer...
                </p>
              </motion.div>

              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                className="relative"
              >
                <AudioVisualizer
                  audioElement={audioRef.current}
                  isPlaying={false}
                  isListening={true}
                  isProcessing={false}
                  size={300}
                />

                <button
                  onClick={stopListeningAndFinish}
                  className="absolute inset-0 flex items-center justify-center cursor-pointer"
                >
                  <motion.div
                    whileHover={{ scale: 1.1 }}
                    whileTap={{ scale: 0.95 }}
                    className="w-20 h-20 rounded-full flex items-center justify-center bg-red-500/80 hover:bg-red-500"
                  >
                    <Square className="w-8 h-8 text-white" />
                  </motion.div>
                </button>
              </motion.div>

              {isTextResponseMode ? (
                <motion.div
                  initial={{ y: 20, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  className="mt-6 w-full max-w-md"
                >
                  <textarea
                    value={textResponse}
                    onChange={(e) => setTextResponse(e.target.value)}
                    placeholder="Type your answer here..."
                    className="w-full px-4 py-3 bg-white/10 border border-white/20 rounded-lg text-white placeholder:text-white/40 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 resize-none"
                    rows={6}
                    aria-label="Text response input"
                  />
                  <div className="mt-4 flex gap-3">
                    <Button
                      onClick={() => {
                        transcriptRef.current = textResponse.trim();
                        setTranscript(textResponse.trim());
                        stateMachine.finishAnswer();
                      }}
                      disabled={!textResponse.trim()}
                      className="bg-emerald-600 hover:bg-emerald-700 text-white"
                    >
                      Submit Answer
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setIsTextResponseMode(false);
                        isTextResponseModeRef.current = false;
                        setTextResponse("");
                        stateMachine.resumeRecording();
                        startRecording();
                      }}
                      className="text-white/60 hover:text-white hover:bg-white/10"
                    >
                      Switch to Voice
                    </Button>
                  </div>
                </motion.div>
              ) : (
                <>
                  {transcript && showCaptions && (
                    <motion.div
                      initial={{ y: 20, opacity: 0 }}
                      animate={{ y: 0, opacity: 1 }}
                      className="mt-6 px-6 py-3 bg-white/10 rounded-lg max-w-md"
                    >
                      <p className="text-white/80 text-center">{transcript}</p>
                    </motion.div>
                  )}
                </>
              )}
            </motion.div>
          )}

          {/* PAUSED State */}
          {state === InterviewState.PAUSED && (
            <motion.div
              key="paused"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex flex-col items-center justify-center"
            >
              <div className="mb-6 text-center">
                <p className="text-sm text-white/60 mb-2">
                  Question {currentQuestionIndex + 1} of {totalQuestions}
                </p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10" />
              </div>

              <motion.div
                initial={{ scale: 0.9, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                className="mb-4 flex flex-col items-center gap-2"
              >
                <div className="flex items-center gap-2">
                  <div className="px-2 py-0.5 rounded bg-yellow-500/20 text-yellow-400 text-xs font-medium">
                    PAUSED
                  </div>
                  <span className="text-2xl font-mono text-white/60">
                    {formatDuration(stateMachine.recordingDuration)}
                  </span>
                </div>
                <div className="text-xs text-white/50">
                  Pause time remaining: {formatDuration(stateMachine.pauseTimeRemaining)} | Total remaining: {formatDuration(Math.max(0, 120 - stateMachine.totalPauseTimeUsed))}
                </div>
              </motion.div>

              <motion.div
                initial={{ y: -20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                className="flex flex-col items-center mb-6"
              >
                <AgentAvatar
                  imageUrl={agentImageUrl}
                  displayName={agentName}
                  size="lg"
                />
                <h2 className="mt-4 text-xl font-semibold text-white">
                  {agentName}
                </h2>
                <p className="text-sm text-yellow-400 font-medium">
                  Recording paused
                </p>
              </motion.div>

              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                className="relative"
              >
                <AudioVisualizer
                  audioElement={audioRef.current}
                  isPlaying={false}
                  isListening={false}
                  isProcessing={false}
                  size={300}
                />

                <button
                  onClick={handleResumeRecording}
                  className="absolute inset-0 flex items-center justify-center cursor-pointer"
                >
                  <motion.div
                    whileHover={{ scale: 1.1 }}
                    whileTap={{ scale: 0.95 }}
                    className="w-20 h-20 rounded-full flex items-center justify-center bg-emerald-500/80 hover:bg-emerald-500"
                  >
                    <Mic className="w-8 h-8 text-white" />
                  </motion.div>
                </button>
              </motion.div>

              {isTextResponseMode ? (
                <motion.div
                  initial={{ y: 20, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  className="mt-6 w-full max-w-md"
                >
                  <textarea
                    value={textResponse}
                    onChange={(e) => setTextResponse(e.target.value)}
                    placeholder="Type your answer here..."
                    className="w-full px-4 py-3 bg-white/10 border border-white/20 rounded-lg text-white placeholder:text-white/40 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 resize-none"
                    rows={6}
                    aria-label="Text response input"
                  />
                  <div className="mt-4 flex gap-3">
                    <Button
                      onClick={() => {
                        transcriptRef.current = textResponse.trim();
                        setTranscript(textResponse.trim());
                        stateMachine.finishAnswer();
                      }}
                      disabled={!textResponse.trim()}
                      className="bg-emerald-600 hover:bg-emerald-700 text-white"
                    >
                      Submit Answer
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setIsTextResponseMode(false);
                        isTextResponseModeRef.current = false;
                        setTextResponse("");
                        stateMachine.resumeRecording();
                        startRecording();
                      }}
                      className="text-white/60 hover:text-white hover:bg-white/10"
                    >
                      Switch to Voice
                    </Button>
                  </div>
                </motion.div>
              ) : (
                <>
                  {transcript && showCaptions && (
                    <motion.div
                      initial={{ y: 20, opacity: 0 }}
                      animate={{ y: 0, opacity: 1 }}
                      className="mt-6 px-6 py-3 bg-white/10 rounded-lg max-w-md"
                    >
                      <p className="text-white/80 text-center">{transcript}</p>
                    </motion.div>
                  )}

                  <div className="mt-8 flex flex-col items-center gap-4">
                    <Button
                      onClick={handleResumeRecording}
                      className="bg-emerald-600 hover:bg-emerald-700 text-white gap-2"
                    >
                      <Mic className="w-4 h-4" />
                      Resume Recording
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={stopListeningAndFinish}
                      className="text-white/60 hover:text-white hover:bg-white/10"
                    >
                      Finish Answer
                    </Button>
                  </div>
                </>
              )}
            </motion.div>
          )}

          {/* REVIEW State */}
          {state === InterviewState.REVIEW && (
            <motion.div
              key="review"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex flex-col items-center justify-center max-w-lg"
            >
              <div className="mb-6 text-center">
                <p className="text-sm text-white/60 mb-2">
                  Question {currentQuestionIndex + 1} of {totalQuestions}
                </p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10" />
              </div>

              <h2 className="text-xl font-semibold text-white mb-2">
                Review Your Answer
              </h2>
              <p className="text-white/60 text-sm mb-6">
                Review your response before submitting
              </p>

              <div className="w-full mb-4 px-4 py-3 bg-white/5 rounded-lg border border-white/10">
                <p className="text-white/50 text-xs uppercase tracking-wide mb-1">Question</p>
                <p className="text-white/90 select-none">{currentQuestion?.text}</p>
              </div>

              <div className="w-full mb-6 px-4 py-3 bg-white/10 rounded-lg border border-emerald-500/30">
                <p className="text-emerald-400 text-xs uppercase tracking-wide mb-1">Your Answer</p>
                <p className="text-white">{transcriptRef.current || "No response recorded"}</p>
              </div>

              <p className="text-white/40 text-sm mb-6">
                Recording duration: {formatDuration(stateMachine.finalRecordingDuration)}
              </p>

              <div className="flex gap-3">
                <Button
                  variant="secondary"
                  onClick={handleReRecord}
                  className="bg-white/10 text-white hover:bg-white/20 border-0"
                >
                  <RotateCcw className="w-4 h-4 mr-2" />
                  Re-record
                </Button>
                <Button
                  onClick={handleSubmitAnswer}
                  className="bg-emerald-600 hover:bg-emerald-700 text-white"
                >
                  {isLastQuestion ? "Submit & Finish" : "Submit & Next"}
                </Button>
              </div>
            </motion.div>
          )}

          {/* PROCESSING State */}
          {state === InterviewState.PROCESSING && (
            <motion.div
              key="processing"
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              className="flex flex-col items-center justify-center"
            >
              <div className="mb-8 text-center">
                <p className="text-sm text-white/60 mb-2">
                  Question {currentQuestionIndex + 1} of {totalQuestions}
                </p>
                <Progress value={progressPercent} className="w-48 h-1.5 bg-white/10" />
              </div>

              <motion.div
                animate={{ rotate: 360 }}
                transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
                className="mb-6"
              >
                <Loader2 className="w-16 h-16 text-cyan-400" />
              </motion.div>

              <h2 className="text-xl font-semibold text-white mb-2">
                Processing...
              </h2>
              <p className="text-white/60 text-sm">
                {processingMessage}
              </p>
            </motion.div>
          )}

          {/* COMPLETE State */}
          {state === InterviewState.COMPLETE && (
            <motion.div
              key="complete"
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="text-center max-w-lg"
            >
              <div className="mb-8">
                <div className="w-20 h-20 rounded-full bg-emerald-600/20 flex items-center justify-center mx-auto mb-6">
                  <PhoneOff className="w-10 h-10 text-emerald-400" />
                </div>
                <h2 className="text-3xl font-bold text-white mb-4">
                  Interview Complete
                </h2>
                <p className="text-slate-400">
                  Thank you for completing your interview! Your responses have been submitted.
                </p>
              </div>

              <div className="mb-6">
                <p className="text-sm text-white/60 mb-2">
                  {totalQuestions} of {totalQuestions} questions completed
                </p>
                <Progress value={100} className="w-48 h-1.5 bg-white/10 mx-auto" />
              </div>

              {onExit && (
                <Button
                  onClick={onExit}
                  className="bg-emerald-600 hover:bg-emerald-700 text-white"
                >
                  Continue
                </Button>
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

              <h2 className="text-2xl font-bold text-white mb-2">
                Something went wrong
              </h2>
              
              <p className="text-white/70 mb-6">
                {stateMachine.errorMessage}
              </p>

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
                <Button
                  variant="outline"
                  onClick={handleGoBack}
                  className="border-white/20 text-white hover:bg-white/10"
                >
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Back
                </Button>
                <Button
                  onClick={handleRetry}
                  className="bg-emerald-600 hover:bg-emerald-700 text-white"
                >
                  <RotateCcw className="w-4 h-4 mr-2" />
                  Try Again
                </Button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </main>

      {/* Controls Bar */}
      <InterviewControlsBar
        state={state}
        pauseTimeRemaining={stateMachine.pauseTimeRemaining}
        totalPauseTimeUsed={stateMachine.totalPauseTimeUsed}
        playbackSpeed={playbackSpeed}
        showCaptions={showCaptions}
        repeatsRemaining={repeatsRemaining}
        onRepeatQuestion={handleRepeatQuestion}
        onTogglePlaybackSpeed={handleTogglePlaybackSpeed}
        onToggleCaptions={handleToggleCaptions}
        onPause={handlePauseRecording}
        onOpenAccommodationModal={() => setAccommodationModalOpen(true)}
      />

      {/* Accommodation Modal */}
      <InterviewAccommodationModal
        open={accommodationModalOpen}
        onOpenChange={setAccommodationModalOpen}
        showCaptions={showCaptions}
        playbackSpeed={playbackSpeed}
        pauseTimeRemaining={stateMachine.pauseTimeRemaining}
        totalPauseTimeUsed={stateMachine.totalPauseTimeUsed}
        onTurnOnCaptions={() => {
          setShowCaptions(true);
          if (typeof window !== "undefined") {
            localStorage.setItem("interview-captions-preference", "true");
          }
        }}
        onTogglePlaybackSpeed={handleTogglePlaybackSpeed}
        onRepeatQuestion={handleRepeatQuestion}
        onSwitchToTextResponse={handleSwitchToTextResponse}
        onHumanAlternative={() => {
          // Human alternative is handled via alert in the modal
          // This allows proceeding if user requests human alternative
          setAccommodationRequested(true);
        }}
        onTechnicalHelp={() => {
          // Technical help is handled via alert in the modal
        }}
      />
    </div>
  );
}
