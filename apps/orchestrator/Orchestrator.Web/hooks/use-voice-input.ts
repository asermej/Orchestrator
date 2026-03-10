"use client";

import { useState, useEffect, useRef, useCallback } from "react";

interface VoiceInputOptions {
  onTranscript: (text: string) => void;
  onInterimTranscript?: (text: string) => void;
  onError?: (error: string) => void;
  language?: string;
  continuous?: boolean;
  interimResults?: boolean;
}

interface VoiceInputReturn {
  isRecording: boolean;
  isSupported: boolean;
  error: string | null;
  mediaStream: MediaStream | null;
  startRecording: () => void;
  stopRecording: (onStopped?: () => void) => void;
  toggleRecording: () => void;
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
  onInterimTranscript,
  onError,
  language = "en-US",
  continuous = true,
  interimResults = true,
}: VoiceInputOptions): VoiceInputReturn {
  const [isRecording, setIsRecording] = useState(false);
  const [isSupported, setIsSupported] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mediaStream, setMediaStream] = useState<MediaStream | null>(null);
  const recognitionRef = useRef<any>(null);
  const onStoppedRef = useRef<(() => void) | null>(null);
  const hasTranscriptRef = useRef(false);
  const isStoppedRef = useRef(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const audioBlobRef = useRef<Blob | null>(null);
  const mediaStreamRef = useRef<MediaStream | null>(null);

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

      recognitionRef.current.onresult = (event: any) => {
        if (isStoppedRef.current) return;

        let finalTranscript = "";
        let interim = "";
        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          if (result.isFinal) {
            finalTranscript += result[0].transcript;
          } else {
            interim += result[0].transcript;
          }
        }

        if (finalTranscript) {
          hasTranscriptRef.current = true;
          onTranscript(finalTranscript);
        }

        if (interim && onInterimTranscript) {
          onInterimTranscript(interim);
        }
      };

      recognitionRef.current.onerror = (event: any) => {
        let errorMessage = "An error occurred with speech recognition.";

        switch (event.error) {
          case "not-allowed":
          case "permission-denied":
            errorMessage = "Microphone access denied. Please enable microphone permissions.";
            break;
          case "no-speech":
            errorMessage = "No speech detected. Please try again.";
            break;
          case "audio-capture":
            errorMessage = "No microphone found. Please check your audio devices.";
            break;
          case "network":
            if (!hasTranscriptRef.current) {
              errorMessage = "No speech detected. Please try again.";
            } else {
              errorMessage = "Network error. Please check your connection.";
            }
            break;
          case "aborted":
            setIsRecording(false);
            const abortedCallback = onStoppedRef.current;
            onStoppedRef.current = null;
            abortedCallback?.();
            return;
        }

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

    return () => {
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (e) {
          // Ignore errors on cleanup
        }
      }
    };
  }, [continuous, interimResults, language, onTranscript, onInterimTranscript, onError]);

  const startRecording = useCallback(() => {
    if (!isSupported || !recognitionRef.current) {
      return;
    }

    try {
      setError(null);
      hasTranscriptRef.current = false;
      isStoppedRef.current = false;
      audioBlobRef.current = null;
      audioChunksRef.current = [];
      recognitionRef.current.start();
      setIsRecording(true);

      navigator.mediaDevices.getUserMedia({ audio: true }).then((stream) => {
        mediaStreamRef.current = stream;
        setMediaStream(stream);
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
          stream.getTracks().forEach((t) => t.stop());
          mediaStreamRef.current = null;
          setMediaStream(null);
        };

        recorder.start(1000);
      }).catch((err) => {
        console.warn("MediaRecorder not available, transcript-only mode:", err);
      });
    } catch (e: any) {
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

    isStoppedRef.current = true;

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
    mediaStream,
    startRecording,
    stopRecording,
    toggleRecording,
    getAudioBlob,
  };
}
