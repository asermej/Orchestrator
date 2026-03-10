"use client";

import { useState, useCallback, useRef, useEffect } from "react";

export enum InterviewState {
  PREP = "PREP",
  AI_SPEAKING = "AI_SPEAKING",
  LISTENING = "LISTENING",
  AI_THINKING = "AI_THINKING",
  COMPLETE = "COMPLETE",
  ERROR = "ERROR",
}

export type ErrorType =
  | "mic_permission"
  | "audio_capture"
  | "audio_playback"
  | "network"
  | "no_speech";

export interface InterviewStateMachine {
  state: InterviewState;
  errorType: ErrorType | null;
  errorMessage: string | null;
  listeningDuration: number;
  previousState: InterviewState | null;

  beginInterview: () => void;
  onTTSEnd: () => void;
  onSilenceDetected: () => void;
  startThinking: () => void;
  finishThinking: (isComplete: boolean) => void;
  handleError: (type: ErrorType, message?: string) => void;
  goBack: () => void;
  retry: () => void;
  complete: () => void;
  resetListeningTimer: () => void;
}

export function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
}

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

export function useInterviewStateMachine(): InterviewStateMachine {
  const [state, setState] = useState<InterviewState>(InterviewState.PREP);
  const [previousState, setPreviousState] = useState<InterviewState | null>(null);
  const [errorType, setErrorType] = useState<ErrorType | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [listeningDuration, setListeningDuration] = useState(0);

  // Ref mirrors previousState so retry() can read it inside a functional
  // setState without a stale closure.
  const previousStateRef = useRef<InterviewState | null>(null);

  const timerIntervalRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (state === InterviewState.LISTENING) {
      timerIntervalRef.current = setInterval(() => {
        setListeningDuration((prev) => prev + 1);
      }, 1000);
    } else {
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

  useEffect(() => {
    if (state === InterviewState.AI_SPEAKING) {
      setListeningDuration(0);
    }
  }, [state]);

  // All transition methods use functional setState so they always read the
  // actual pending state instead of a potentially stale closure value.
  // This prevents "Invalid transition" errors when methods are called from
  // async callbacks whose closure captured an earlier render's stateMachine.

  const beginInterview = useCallback(() => {
    setState((currentState) => {
      if (currentState !== InterviewState.PREP) {
        console.warn(`Invalid transition: beginInterview from ${currentState}`);
        return currentState;
      }
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      setErrorType(null);
      setErrorMessage(null);
      return InterviewState.AI_SPEAKING;
    });
  }, []);

  const onTTSEnd = useCallback(() => {
    setState((currentState) => {
      if (currentState === InterviewState.LISTENING) return currentState;
      if (currentState !== InterviewState.AI_SPEAKING) {
        console.warn(`Invalid transition: onTTSEnd from ${currentState}`);
        return currentState;
      }
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      setListeningDuration(0);
      return InterviewState.LISTENING;
    });
  }, []);

  const onSilenceDetected = useCallback(() => {
    setState((currentState) => {
      if (currentState === InterviewState.AI_THINKING) return currentState;
      if (currentState === InterviewState.COMPLETE || currentState === InterviewState.ERROR) return currentState;
      if (currentState !== InterviewState.LISTENING) {
        console.warn(`Invalid transition: onSilenceDetected from ${currentState}`);
        return currentState;
      }
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      return InterviewState.AI_THINKING;
    });
  }, []);

  const startThinking = useCallback(() => {
    setState((currentState) => {
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      return InterviewState.AI_THINKING;
    });
  }, []);

  const finishThinking = useCallback((isComplete: boolean) => {
    setState((currentState) => {
      const target = isComplete ? InterviewState.COMPLETE : InterviewState.AI_SPEAKING;
      if (currentState === target) return currentState;
      if (currentState !== InterviewState.AI_THINKING) {
        console.warn(`Invalid transition: finishThinking from ${currentState}`);
        return currentState;
      }
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      return target;
    });
  }, []);

  const handleError = useCallback((type: ErrorType, message?: string) => {
    setState((currentState) => {
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      return InterviewState.ERROR;
    });
    setErrorType(type);
    setErrorMessage(message || getErrorMessage(type));
  }, []);

  const goBack = useCallback(() => {
    setState((currentState) => {
      if (currentState !== InterviewState.ERROR) {
        console.warn(`Invalid transition: goBack from ${currentState}`);
        return currentState;
      }
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      setErrorType(null);
      setErrorMessage(null);
      return InterviewState.PREP;
    });
  }, []);

  const retry = useCallback(() => {
    setState((currentState) => {
      if (currentState !== InterviewState.ERROR) {
        console.warn(`Invalid transition: retry from ${currentState}`);
        return currentState;
      }

      const prevState = previousStateRef.current;
      const retryState =
        prevState === InterviewState.LISTENING
          ? InterviewState.AI_SPEAKING
          : prevState === InterviewState.AI_SPEAKING
            ? InterviewState.AI_SPEAKING
            : InterviewState.PREP;

      previousStateRef.current = currentState;
      setPreviousState(currentState);
      setErrorType(null);
      setErrorMessage(null);
      return retryState;
    });
  }, []);

  const complete = useCallback(() => {
    setState((currentState) => {
      if (currentState === InterviewState.COMPLETE) return currentState;
      previousStateRef.current = currentState;
      setPreviousState(currentState);
      return InterviewState.COMPLETE;
    });
  }, []);

  const resetListeningTimer = useCallback(() => {
    setListeningDuration(0);
  }, []);

  return {
    state,
    errorType,
    errorMessage,
    listeningDuration,
    previousState,

    beginInterview,
    onTTSEnd,
    onSilenceDetected,
    startThinking,
    finishThinking,
    handleError,
    goBack,
    retry,
    complete,
    resetListeningTimer,
  };
}
