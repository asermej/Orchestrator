"use client";

import { useState, useCallback, useRef, useEffect } from "react";

/**
 * Interview state machine states
 */
export enum InterviewState {
  PREP = "PREP",                         // Welcome/intro screens
  QUESTION_PLAYING = "QUESTION_PLAYING", // TTS speaking question
  RECORDING = "RECORDING",               // User recording answer
  PAUSED = "PAUSED",                     // Recording paused
  REVIEW = "REVIEW",                     // User reviewing transcript
  PROCESSING = "PROCESSING",             // Submitting answer
  COMPLETE = "COMPLETE",                 // Interview finished
  ERROR = "ERROR",                       // Error state
}

/**
 * Error types that can occur during the interview
 */
export type ErrorType = 
  | "mic_permission"   // Microphone access denied
  | "audio_capture"    // No microphone found
  | "audio_playback"   // TTS/audio playback failed
  | "network"          // Network error during submission
  | "no_speech";       // No speech detected

/**
 * State machine hook return type
 */
export interface InterviewStateMachine {
  state: InterviewState;
  errorType: ErrorType | null;
  errorMessage: string | null;
  recordingDuration: number; // seconds (current/live duration)
  finalRecordingDuration: number; // seconds (preserved duration for REVIEW state)
  canPause: boolean;
  canResume: boolean;
  previousState: InterviewState | null;
  pauseTimeRemaining: number; // Remaining pause time for current question (0-60s)
  totalPauseTimeUsed: number; // Total pause time used across interview (max 120s)
  
  // Transitions
  beginInterview: () => void;
  onTTSEnd: () => void;
  finishAnswer: () => void;
  pauseRecording: () => void;
  resumeRecording: () => void;
  startProcessing: () => void;
  completeProcessing: (isLastQuestion: boolean) => void;
  handleError: (type: ErrorType, message?: string) => void;
  goBack: () => void;
  retry: () => void;
  complete: () => void;
  resetRecordingTimer: () => void;
  reRecord: () => void;
}

/**
 * Format seconds as mm:ss
 */
export function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
}

/**
 * Get error message for error type
 */
function getErrorMessage(type: ErrorType): string {
  switch (type) {
    case "mic_permission":
      return "Microphone access denied. Please enable microphone permissions in your browser settings.";
    case "audio_capture":
      return "No microphone found. Please check your audio devices.";
    case "audio_playback":
      return "Failed to play audio. Please check your speaker/headphone connection.";
    case "network":
      return "Network error. Please check your internet connection and try again.";
    case "no_speech":
      return "No speech detected. Please try again and speak clearly.";
    default:
      return "An unexpected error occurred. Please try again.";
  }
}

/**
 * Hook that manages the interview state machine with explicit state transitions.
 * Provides recording timer, error handling, and transition guards.
 */
export function useInterviewStateMachine(): InterviewStateMachine {
  const [state, setState] = useState<InterviewState>(InterviewState.PREP);
  const [previousState, setPreviousState] = useState<InterviewState | null>(null);
  const [errorType, setErrorType] = useState<ErrorType | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [recordingDuration, setRecordingDuration] = useState(0);
  const [finalRecordingDuration, setFinalRecordingDuration] = useState(0);
  const [pauseTimeRemaining, setPauseTimeRemaining] = useState(60); // 60s per question
  const [totalPauseTimeUsed, setTotalPauseTimeUsed] = useState(0); // Max 120s total
  
  const timerIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const pauseTimerRef = useRef<NodeJS.Timeout | null>(null);
  const pauseStartTimeRef = useRef<number | null>(null);

  // Computed properties
  const canPause = state === InterviewState.RECORDING;
  const canResume = state === InterviewState.PAUSED;

  // Start/stop recording timer based on state
  useEffect(() => {
    if (state === InterviewState.RECORDING) {
      // Start timer
      timerIntervalRef.current = setInterval(() => {
        setRecordingDuration((prev) => prev + 1);
      }, 1000);
    } else if (state !== InterviewState.PAUSED) {
      // Stop and clear timer (except when paused - freeze timer)
      if (timerIntervalRef.current) {
        clearInterval(timerIntervalRef.current);
        timerIntervalRef.current = null;
      }
    }

    return () => {
      if (timerIntervalRef.current) {
        clearInterval(timerIntervalRef.current);
      }
    };
  }, [state]);

  // Reset timer when entering QUESTION_PLAYING (but preserve finalRecordingDuration in REVIEW)
  useEffect(() => {
    if (state === InterviewState.QUESTION_PLAYING) {
      setRecordingDuration(0);
      setFinalRecordingDuration(0);
      // Reset pause time for new question (60s per question)
      setPauseTimeRemaining(60);
    }
  }, [state]);

  // Pause countdown timer
  useEffect(() => {
    if (state === InterviewState.PAUSED) {
      // Start pause timer
      pauseStartTimeRef.current = Date.now();
      pauseTimerRef.current = setInterval(() => {
        setPauseTimeRemaining((prev) => {
          if (prev <= 1) {
            // Auto-resume when time expires
            setState((currentState) => {
              if (currentState === InterviewState.PAUSED) {
                setPreviousState(currentState);
                return InterviewState.RECORDING;
              }
              return currentState;
            });
            return 0;
          }
          return prev - 1;
        });
        setTotalPauseTimeUsed((prev) => {
          const newTotal = prev + 1;
          // Cap at 120s total
          if (newTotal >= 120) {
            // Auto-resume if total limit reached
            setState((currentState) => {
              if (currentState === InterviewState.PAUSED) {
                setPreviousState(currentState);
                return InterviewState.RECORDING;
              }
              return currentState;
            });
            return 120;
          }
          return newTotal;
        });
      }, 1000);
    } else {
      // Stop pause timer when not paused
      if (pauseTimerRef.current) {
        clearInterval(pauseTimerRef.current);
        pauseTimerRef.current = null;
      }
      pauseStartTimeRef.current = null;
    }

    return () => {
      if (pauseTimerRef.current) {
        clearInterval(pauseTimerRef.current);
      }
    };
  }, [state]);

  /**
   * Transition from PREP to QUESTION_PLAYING
   */
  const beginInterview = useCallback(() => {
    if (state !== InterviewState.PREP) {
      console.warn(`Invalid transition: beginInterview from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.QUESTION_PLAYING);
    setErrorType(null);
    setErrorMessage(null);
  }, [state]);

  /**
   * Auto-transition from QUESTION_PLAYING to RECORDING when TTS ends
   */
  const onTTSEnd = useCallback(() => {
    if (state !== InterviewState.QUESTION_PLAYING) {
      console.warn(`Invalid transition: onTTSEnd from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.RECORDING);
    setRecordingDuration(0);
  }, [state]);

  /**
   * Transition from RECORDING to REVIEW when user finishes answer
   */
  const finishAnswer = useCallback(() => {
    if (state !== InterviewState.RECORDING && state !== InterviewState.PAUSED) {
      console.warn(`Invalid transition: finishAnswer from ${state}`);
      return;
    }
    // Preserve the recording duration before transitioning to REVIEW
    setFinalRecordingDuration(recordingDuration);
    setPreviousState(state);
    setState(InterviewState.REVIEW);
  }, [state, recordingDuration]);

  /**
   * Pause recording
   */
  const pauseRecording = useCallback(() => {
    if (state !== InterviewState.RECORDING) {
      console.warn(`Invalid transition: pauseRecording from ${state}`);
      return;
    }
    // Check if pause is allowed (has remaining time and total limit not reached)
    if (pauseTimeRemaining <= 0 || totalPauseTimeUsed >= 120) {
      console.warn("Pause limit reached");
      return;
    }
    setPreviousState(state);
    setState(InterviewState.PAUSED);
  }, [state, pauseTimeRemaining, totalPauseTimeUsed]);

  /**
   * Resume recording
   */
  const resumeRecording = useCallback(() => {
    if (state !== InterviewState.PAUSED) {
      console.warn(`Invalid transition: resumeRecording from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.RECORDING);
  }, [state]);

  /**
   * Start processing (submitting answer)
   */
  const startProcessing = useCallback(() => {
    if (state !== InterviewState.REVIEW) {
      console.warn(`Invalid transition: startProcessing from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.PROCESSING);
  }, [state]);

  /**
   * Complete processing and move to next state
   */
  const completeProcessing = useCallback((isLastQuestion: boolean) => {
    setState((currentState) => {
      if (currentState !== InterviewState.PROCESSING) {
        console.warn(`Invalid transition: completeProcessing from ${currentState}`);
        return currentState; // Don't change state if invalid transition
      }
      // Update previous state before transitioning
      setPreviousState(currentState);
      if (isLastQuestion) {
        return InterviewState.COMPLETE;
      } else {
        return InterviewState.QUESTION_PLAYING;
      }
    });
  }, []);

  /**
   * Handle error - can transition from most states
   */
  const handleError = useCallback((type: ErrorType, message?: string) => {
    setPreviousState(state);
    setState(InterviewState.ERROR);
    setErrorType(type);
    setErrorMessage(message || getErrorMessage(type));
  }, [state]);

  /**
   * Go back from ERROR to PREP
   */
  const goBack = useCallback(() => {
    if (state !== InterviewState.ERROR) {
      console.warn(`Invalid transition: goBack from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.PREP);
    setErrorType(null);
    setErrorMessage(null);
  }, [state]);

  /**
   * Retry from ERROR - return to previous state or PREP
   */
  const retry = useCallback(() => {
    if (state !== InterviewState.ERROR) {
      console.warn(`Invalid transition: retry from ${state}`);
      return;
    }
    
    // Determine which state to retry from
    const retryState = previousState === InterviewState.RECORDING 
      ? InterviewState.QUESTION_PLAYING // Re-play question then start recording
      : previousState === InterviewState.QUESTION_PLAYING
      ? InterviewState.QUESTION_PLAYING
      : InterviewState.PREP;
    
    setPreviousState(state);
    setState(retryState);
    setErrorType(null);
    setErrorMessage(null);
  }, [state, previousState]);

  /**
   * Mark interview as complete
   */
  const complete = useCallback(() => {
    setPreviousState(state);
    setState(InterviewState.COMPLETE);
  }, [state]);

  /**
   * Reset recording timer manually
   */
  const resetRecordingTimer = useCallback(() => {
    setRecordingDuration(0);
  }, []);

  /**
   * Re-record: transition from REVIEW back to QUESTION_PLAYING
   */
  const reRecord = useCallback(() => {
    if (state !== InterviewState.REVIEW) {
      console.warn(`Invalid transition: reRecord from ${state}`);
      return;
    }
    setPreviousState(state);
    setState(InterviewState.QUESTION_PLAYING);
    setRecordingDuration(0);
    setFinalRecordingDuration(0);
  }, [state]);

  return {
    state,
    errorType,
    errorMessage,
    recordingDuration,
    finalRecordingDuration,
    canPause,
    canResume,
    previousState,
    pauseTimeRemaining,
    totalPauseTimeUsed,
    
    beginInterview,
    onTTSEnd,
    finishAnswer,
    pauseRecording,
    resumeRecording,
    startProcessing,
    completeProcessing,
    handleError,
    goBack,
    retry,
    complete,
    resetRecordingTimer,
    reRecord,
  };
}
