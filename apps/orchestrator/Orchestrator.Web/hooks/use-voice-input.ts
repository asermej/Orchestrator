"use client";

import { useState, useEffect, useRef, useCallback } from "react";

interface VoiceInputOptions {
  onTranscript: (text: string) => void;
  onError?: (error: string) => void;
  language?: string;
  continuous?: boolean;
  interimResults?: boolean;
}

interface VoiceInputReturn {
  isRecording: boolean;
  isSupported: boolean;
  error: string | null;
  startRecording: () => void;
  /** Stops recording. Optional callback is invoked when recognition has ended (after final transcript may have been delivered). */
  stopRecording: (onStopped?: () => void) => void;
  toggleRecording: () => void;
  /** Returns the recorded audio blob (webm format) from the last recording session, or null if unavailable. */
  getAudioBlob: () => Blob | null;
}

declare global {
  interface Window {
    SpeechRecognition: any;
    webkitSpeechRecognition: any;
  }
}

export function useVoiceInput({
  onTranscript,
  onError,
  language = "en-US",
  continuous = true,
  interimResults = true,
}: VoiceInputOptions): VoiceInputReturn {
  const [isRecording, setIsRecording] = useState(false);
  const [isSupported, setIsSupported] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const recognitionRef = useRef<any>(null);
  const onStoppedRef = useRef<(() => void) | null>(null);
  const hasTranscriptRef = useRef(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const audioBlobRef = useRef<Blob | null>(null);
  const mediaStreamRef = useRef<MediaStream | null>(null);

  // Check browser compatibility on mount
  useEffect(() => {
    const SpeechRecognition =
      typeof window !== "undefined" &&
      (window.SpeechRecognition || window.webkitSpeechRecognition);

    if (SpeechRecognition) {
      setIsSupported(true);
      recognitionRef.current = new SpeechRecognition();
      recognitionRef.current.continuous = continuous;
      recognitionRef.current.interimResults = interimResults;
      recognitionRef.current.lang = language;

      // Handle speech recognition results
      recognitionRef.current.onresult = (event: any) => {
        let transcript = "";
        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          if (result.isFinal) {
            transcript += result[0].transcript;
          }
        }

        if (transcript) {
          hasTranscriptRef.current = true;
          onTranscript(transcript);
        }
      };

      // Handle errors
      recognitionRef.current.onerror = (event: any) => {
        let errorMessage = "An error occurred with speech recognition.";

        switch (event.error) {
          case "not-allowed":
          case "permission-denied":
            errorMessage = "Microphone access denied. Please enable microphone permissions.";
            break;
          case "no-speech":
            // Expected when user doesn't speak in time; show message in UI, don't log as error
            errorMessage = "No speech detected. Please try again.";
            break;
          case "audio-capture":
            errorMessage = "No microphone found. Please check your audio devices.";
            break;
          case "network":
            // Network errors are often false positives when user doesn't speak (timeout)
            // If no transcript was received, treat it like "no-speech" for graceful handling
            if (!hasTranscriptRef.current) {
              errorMessage = "No speech detected. Please try again.";
            } else {
              errorMessage = "Network error. Please check your connection.";
            }
            break;
          case "aborted":
            // User stopped recording or recognition was stopped; not an error
            setIsRecording(false);
            const abortedCallback = onStoppedRef.current;
            onStoppedRef.current = null;
            abortedCallback?.();
            return;
        }

        // Only log unexpected errors to console (not "aborted", "no-speech", or "network" without transcript)
        // Use console.warn instead of console.error to avoid Next.js error overlay intercepting it
        const shouldLogError = event.error !== "no-speech" && 
                               !(event.error === "network" && !hasTranscriptRef.current);
        if (shouldLogError) {
          console.warn("Speech recognition error:", event.error);
        }

        setError(errorMessage);
        if (onError) {
          onError(errorMessage);
        }
        setIsRecording(false);
      };

      // Handle end of speech recognition (final transcript may have been delivered by now)
      recognitionRef.current.onend = () => {
        setIsRecording(false);
        const callback = onStoppedRef.current;
        onStoppedRef.current = null;
        callback?.();
      };
    } else {
      setIsSupported(false);
      const errorMsg = "Speech recognition is not supported in this browser. Please use Chrome, Edge, or Safari.";
      setError(errorMsg);
      if (onError) {
        onError(errorMsg);
      }
    }

    // Cleanup on unmount
    return () => {
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (e) {
          // Ignore errors on cleanup
        }
      }
    };
  }, [continuous, interimResults, language, onTranscript, onError]);

  const startRecording = useCallback(() => {
    if (!isSupported || !recognitionRef.current) {
      return;
    }

    try {
      setError(null);
      hasTranscriptRef.current = false; // Reset transcript tracking when starting new recording
      audioBlobRef.current = null;
      audioChunksRef.current = [];
      recognitionRef.current.start();
      setIsRecording(true);

      // Start MediaRecorder in parallel for audio capture
      navigator.mediaDevices.getUserMedia({ audio: true }).then((stream) => {
        mediaStreamRef.current = stream;
        const mimeType = MediaRecorder.isTypeSupported("audio/webm;codecs=opus")
          ? "audio/webm;codecs=opus"
          : "audio/webm";
        const recorder = new MediaRecorder(stream, { mimeType });
        mediaRecorderRef.current = recorder;

        recorder.ondataavailable = (e) => {
          if (e.data.size > 0) {
            audioChunksRef.current.push(e.data);
          }
        };

        recorder.onstop = () => {
          if (audioChunksRef.current.length > 0) {
            audioBlobRef.current = new Blob(audioChunksRef.current, { type: mimeType });
          }
          // Release mic
          stream.getTracks().forEach((t) => t.stop());
          mediaStreamRef.current = null;
        };

        recorder.start(1000); // collect chunks every second
      }).catch((err) => {
        // Audio capture failed, but SpeechRecognition still works for transcript
        console.warn("MediaRecorder not available, transcript-only mode:", err);
      });
    } catch (e: any) {
      // Recognition might already be started
      if (e.message && !e.message.includes("already started")) {
        const errorMsg = "Failed to start recording. Please try again.";
        setError(errorMsg);
        if (onError) {
          onError(errorMsg);
        }
      }
    }
  }, [isSupported, onError]);

  const stopRecording = useCallback((onStopped?: () => void) => {
    if (!recognitionRef.current) {
      onStopped?.();
      return;
    }

    if (onStopped) {
      onStoppedRef.current = onStopped;
    }

    // Stop MediaRecorder
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== "inactive") {
      try {
        mediaRecorderRef.current.stop();
      } catch (e) {
        // Ignore errors on stop
      }
      mediaRecorderRef.current = null;
    }

    try {
      recognitionRef.current.stop();
      setIsRecording(false);
    } catch (e) {
      // Ignore errors on stop
      setIsRecording(false);
      const callback = onStoppedRef.current;
      onStoppedRef.current = null;
      callback?.();
    }
  }, []);

  const toggleRecording = useCallback(() => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  }, [isRecording, startRecording, stopRecording]);

  const getAudioBlob = useCallback(() => {
    return audioBlobRef.current;
  }, []);

  // Cleanup MediaRecorder on unmount
  useEffect(() => {
    return () => {
      if (mediaRecorderRef.current && mediaRecorderRef.current.state !== "inactive") {
        try { mediaRecorderRef.current.stop(); } catch (e) { /* ignore */ }
      }
      if (mediaStreamRef.current) {
        mediaStreamRef.current.getTracks().forEach((t) => t.stop());
      }
    };
  }, []);

  return {
    isRecording,
    isSupported,
    error,
    startRecording,
    stopRecording,
    toggleRecording,
    getAudioBlob,
  };
}

