"use client";

import { useState, useRef, useCallback, useEffect } from "react";

interface UseStreamingAudioOptions {
  onPlayStart?: () => void;
  onPlayEnd?: () => void;
  onError?: (error: string) => void;
}

interface UseStreamingAudioReturn {
  audioRef: React.RefObject<HTMLAudioElement>;
  isStreaming: boolean;
  isPlaying: boolean;
  error: string | null;
  streamResponse: (chatId: string, personaId: string, message: string) => Promise<void>;
  stopPlayback: () => void;
  playMessageAudio: (messageId: string) => Promise<void>;
}

/**
 * Hook for handling streaming audio playback from the conversation API.
 * Supports both streaming responses (live conversation) and cached replay.
 */
export function useStreamingAudio({
  onPlayStart,
  onPlayEnd,
  onError,
}: UseStreamingAudioOptions = {}): UseStreamingAudioReturn {
  const [isStreaming, setIsStreaming] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const audioRef = useRef<HTMLAudioElement>(null);
  const mediaSourceRef = useRef<MediaSource | null>(null);
  const sourceBufferRef = useRef<SourceBuffer | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Use Next.js API route so auth (cookies/session) runs on the server
  const voiceAudioUrl = "/api/voice/respond/audio";

  // Clean up on unmount
  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
      if (mediaSourceRef.current?.readyState === "open") {
        try {
          mediaSourceRef.current.endOfStream();
        } catch (e) {
          // Ignore errors during cleanup
        }
      }
    };
  }, []);

  /**
   * Stop playback and clean up resources
   */
  const stopPlayback = useCallback(() => {
    abortControllerRef.current?.abort();
    
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
    
    setIsStreaming(false);
    setIsPlaying(false);
  }, []);

  /**
   * Stream audio response for a conversation message.
   * Input is text (already transcribed), output is streaming audio.
   */
  const streamResponse = useCallback(
    async (chatId: string, personaId: string, message: string) => {
      setError(null);
      setIsStreaming(true);

      // Abort any existing request
      abortControllerRef.current?.abort();
      abortControllerRef.current = new AbortController();

      try {
        const response = await fetch(voiceAudioUrl, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ chatId, personaId, message }),
          signal: abortControllerRef.current.signal,
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => ({}));
          throw new Error(errorData.error || `Request failed: ${response.status}`);
        }

        if (!response.body) {
          throw new Error("No response body");
        }

        // Create MediaSource for streaming playback
        const mediaSource = new MediaSource();
        mediaSourceRef.current = mediaSource;

        if (audioRef.current) {
          audioRef.current.src = URL.createObjectURL(mediaSource);
        }

        await new Promise<void>((resolve, reject) => {
          mediaSource.addEventListener("sourceopen", async () => {
            try {
              const sourceBuffer = mediaSource.addSourceBuffer("audio/mpeg");
              sourceBufferRef.current = sourceBuffer;

              const reader = response.body!.getReader();
              const chunks: Uint8Array[] = [];

              // Start playback as soon as we have some data
              let playbackStarted = false;

              while (true) {
                const { done, value } = await reader.read();
                
                if (done) {
                  break;
                }

                chunks.push(value);

                // Wait for previous append to complete
                if (sourceBuffer.updating) {
                  await new Promise<void>((res) => {
                    sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                  });
                }

                sourceBuffer.appendBuffer(value);

                // Start playback after receiving first chunk
                if (!playbackStarted && audioRef.current) {
                  await new Promise<void>((res) => {
                    sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                  });
                  
                  audioRef.current.play().catch(console.error);
                  setIsPlaying(true);
                  onPlayStart?.();
                  playbackStarted = true;
                }
              }

              // Wait for final buffer update
              if (sourceBuffer.updating) {
                await new Promise<void>((res) => {
                  sourceBuffer.addEventListener("updateend", () => res(), { once: true });
                });
              }

              // Signal end of stream
              if (mediaSource.readyState === "open") {
                mediaSource.endOfStream();
              }

              resolve();
            } catch (err) {
              reject(err);
            }
          });

          mediaSource.addEventListener("error", () => {
            reject(new Error("MediaSource error"));
          });
        });

      } catch (err) {
        if (err instanceof Error && err.name === "AbortError") {
          // Request was aborted, not an error
          return;
        }
        
        const errorMessage = err instanceof Error ? err.message : "Failed to stream audio";
        setError(errorMessage);
        onError?.(errorMessage);
      } finally {
        setIsStreaming(false);
      }
    },
    [voiceAudioUrl, onPlayStart, onError]
  );

  /**
   * Play cached audio for an existing message (replay).
   */
  const playMessageAudio = useCallback(
    async (messageId: string) => {
      setError(null);

      try {
        const response = await fetch(`/api/voice/message/${messageId}/audio`, {
          method: "GET",
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => ({}));
          throw new Error(errorData.error || `Request failed: ${response.status}`);
        }

        const audioBlob = await response.blob();
        const audioUrl = URL.createObjectURL(audioBlob);

        if (audioRef.current) {
          audioRef.current.src = audioUrl;
          await audioRef.current.play();
          setIsPlaying(true);
          onPlayStart?.();
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to play audio";
        setError(errorMessage);
        onError?.(errorMessage);
      }
    },
    [onPlayStart, onError]
  );

  // Handle audio ended event
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;

    const handleEnded = () => {
      setIsPlaying(false);
      onPlayEnd?.();
    };

    audio.addEventListener("ended", handleEnded);
    return () => audio.removeEventListener("ended", handleEnded);
  }, [onPlayEnd]);

  return {
    audioRef,
    isStreaming,
    isPlaying,
    error,
    streamResponse,
    stopPlayback,
    playMessageAudio,
  };
}
