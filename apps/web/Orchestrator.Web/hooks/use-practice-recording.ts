"use client";

import { useState, useCallback, useRef, useEffect } from "react";
import { useVoiceInput } from "./use-voice-input";

interface PracticeRecordingResult {
  isRecording: boolean;
  transcript: string;
  startPractice: () => void;
  stopPractice: () => void;
  clearPractice: () => void;
}

const PRACTICE_DURATION_MS = 10000; // 10 seconds max

/**
 * Hook for practice recording functionality.
 * Records audio and shows live transcript preview.
 * All data is ephemeral (not stored, cleared on unmount).
 */
export function usePracticeRecording(): PracticeRecordingResult {
  const [isRecording, setIsRecording] = useState(false);
  const [transcript, setTranscript] = useState("");
  const transcriptRef = useRef("");
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  const handleTranscript = useCallback((text: string) => {
    setTranscript((prev) => {
      const newTranscript = prev + " " + text;
      transcriptRef.current = newTranscript.trim();
      return newTranscript.trim();
    });
  }, []);

  const {
    startRecording: startVoiceRecording,
    stopRecording: stopVoiceRecording,
  } = useVoiceInput({
    onTranscript: handleTranscript,
    onError: (err) => {
      console.warn("Practice recording error:", err);
      // Don't show errors for practice - just stop recording
      stopPractice();
    },
    continuous: true,
    interimResults: false,
  });

  const stopPractice = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
    stopVoiceRecording();
    setIsRecording(false);
  }, [stopVoiceRecording]);

  const startPractice = useCallback(() => {
    // Clear previous transcript
    setTranscript("");
    transcriptRef.current = "";

    // Start recording
    setIsRecording(true);
    startVoiceRecording();

    // Auto-stop after 10 seconds
    timeoutRef.current = setTimeout(() => {
      stopPractice();
    }, PRACTICE_DURATION_MS);
  }, [startVoiceRecording, stopPractice]);

  const clearPractice = useCallback(() => {
    stopPractice();
    setTranscript("");
    transcriptRef.current = "";
  }, [stopPractice]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
      stopVoiceRecording();
    };
  }, [stopVoiceRecording]);

  return {
    isRecording,
    transcript,
    startPractice,
    stopPractice,
    clearPractice,
  };
}
