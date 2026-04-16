"use client";

import { useState, useRef, useCallback, useEffect } from "react";

interface DeepgramSttOptions {
  onFinalTranscript: (text: string, lastSpeechAt: number) => void;
  onInterimTranscript?: (text: string) => void;
  onBargeIn?: () => void;
  onError?: (error: string) => void;
}

interface DeepgramSttReturn {
  isListening: boolean;
  isSupported: boolean;
  error: string | null;
  mediaStream: MediaStream | null;
  startListening: () => Promise<void>;
  stopListening: () => void;
  destroyConnection: () => void;
}

const DEEPGRAM_WS_BASE = "wss://api.deepgram.com/v1/listen";
const SAMPLE_RATE = 16000;
const BARGE_IN_MIN_CHARS = 3;
const KEEPALIVE_INTERVAL_MS = 8000;

async function fetchDeepgramToken(): Promise<string> {
  const res = await fetch("/api/deepgram-token", { method: "POST" });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error || `Token request failed (${res.status})`);
  }
  const data = await res.json();
  return data.token;
}

export function useDeepgramStt({
  onFinalTranscript,
  onInterimTranscript,
  onBargeIn,
  onError,
}: DeepgramSttOptions): DeepgramSttReturn {
  const [isListening, setIsListening] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mediaStream, setMediaStream] = useState<MediaStream | null>(null);

  const wsRef = useRef<WebSocket | null>(null);
  const mediaStreamRef = useRef<MediaStream | null>(null);
  const audioContextRef = useRef<AudioContext | null>(null);
  const workletNodeRef = useRef<AudioWorkletNode | null>(null);
  const sourceRef = useRef<MediaStreamAudioSourceNode | null>(null);

  const bargeInFiredRef = useRef(false);
  const accumulatedTranscriptRef = useRef("");
  const lastSpeechAtRef = useRef<number>(0);
  const isStoppingRef = useRef(false);
  const isListeningRef = useRef(false);
  const isPausedRef = useRef(false);
  const keepAliveTimerRef = useRef<NodeJS.Timeout | null>(null);
  const connectionReadyRef = useRef(false);

  const onFinalTranscriptRef = useRef(onFinalTranscript);
  const onInterimTranscriptRef = useRef(onInterimTranscript);
  const onBargeInRef = useRef(onBargeIn);
  const onErrorRef = useRef(onError);

  useEffect(() => { onFinalTranscriptRef.current = onFinalTranscript; }, [onFinalTranscript]);
  useEffect(() => { onInterimTranscriptRef.current = onInterimTranscript; }, [onInterimTranscript]);
  useEffect(() => { onBargeInRef.current = onBargeIn; }, [onBargeIn]);
  useEffect(() => { onErrorRef.current = onError; }, [onError]);

  const isSupported =
    typeof window !== "undefined" &&
    typeof WebSocket !== "undefined" &&
    !!navigator.mediaDevices?.getUserMedia;

  const startKeepAlive = useCallback(() => {
    if (keepAliveTimerRef.current) clearInterval(keepAliveTimerRef.current);
    keepAliveTimerRef.current = setInterval(() => {
      if (wsRef.current?.readyState === WebSocket.OPEN && isPausedRef.current) {
        wsRef.current.send(JSON.stringify({ type: "KeepAlive" }));
      }
    }, KEEPALIVE_INTERVAL_MS);
  }, []);

  const stopKeepAlive = useCallback(() => {
    if (keepAliveTimerRef.current) {
      clearInterval(keepAliveTimerRef.current);
      keepAliveTimerRef.current = null;
    }
  }, []);

  const destroyConnection = useCallback(() => {
    stopKeepAlive();
    connectionReadyRef.current = false;
    isPausedRef.current = false;
    isListeningRef.current = false;
    isStoppingRef.current = false;
    setIsListening(false);

    if (workletNodeRef.current) {
      workletNodeRef.current.disconnect();
      workletNodeRef.current = null;
    }
    if (sourceRef.current) {
      sourceRef.current.disconnect();
      sourceRef.current = null;
    }
    if (audioContextRef.current) {
      audioContextRef.current.close().catch(() => {});
      audioContextRef.current = null;
    }
    if (mediaStreamRef.current) {
      mediaStreamRef.current.getTracks().forEach((t) => t.stop());
      mediaStreamRef.current = null;
      setMediaStream(null);
    }
    if (wsRef.current) {
      try {
        if (wsRef.current.readyState === WebSocket.OPEN) {
          wsRef.current.send(JSON.stringify({ type: "CloseStream" }));
        }
        wsRef.current.close();
      } catch {
        // ignore
      }
      wsRef.current = null;
    }
  }, [stopKeepAlive]);

  const stopListening = useCallback(() => {
    if (!isListeningRef.current || isStoppingRef.current) return;
    isStoppingRef.current = true;
    isListeningRef.current = false;
    isPausedRef.current = true;
    setIsListening(false);

    // Disconnect audio source to stop sending audio, but keep WS alive
    if (sourceRef.current && workletNodeRef.current) {
      try { sourceRef.current.disconnect(workletNodeRef.current); } catch { /* already disconnected */ }
    }

    startKeepAlive();
    isStoppingRef.current = false;
  }, [startKeepAlive]);

  const ensureConnection = useCallback(async (): Promise<boolean> => {
    // Already have a live connection
    if (wsRef.current?.readyState === WebSocket.OPEN && connectionReadyRef.current) {
      return true;
    }

    // Need to establish new connection
    let token: string;
    try {
      token = await fetchDeepgramToken();
      if (!token) throw new Error("Empty token received");
    } catch (err: any) {
      const msg = `Failed to get Deepgram token: ${err.message}`;
      setError(msg);
      onErrorRef.current?.(msg);
      return false;
    }

    // Get microphone if we don't already have it
    if (!mediaStreamRef.current) {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
            sampleRate: SAMPLE_RATE,
          },
        });
        mediaStreamRef.current = stream;
        setMediaStream(stream);
      } catch (err: any) {
        let msg: string;
        if (err.name === "NotAllowedError") {
          msg = "Microphone access denied. Please enable microphone permissions.";
        } else if (err.name === "NotFoundError") {
          msg = "No microphone found. Please check your audio devices.";
        } else {
          msg = "Failed to access microphone.";
        }
        setError(msg);
        onErrorRef.current?.(msg);
        return false;
      }
    }

    // Set up AudioContext and worklet if needed
    if (!audioContextRef.current || audioContextRef.current.state === "closed") {
      try {
        const audioContext = new AudioContext({ sampleRate: SAMPLE_RATE });
        audioContextRef.current = audioContext;
        await audioContext.audioWorklet.addModule("/audio-pcm-worklet.js");

        const source = audioContext.createMediaStreamSource(mediaStreamRef.current);
        sourceRef.current = source;

        const workletNode = new AudioWorkletNode(audioContext, "pcm-processor");
        workletNodeRef.current = workletNode;

        workletNode.port.onmessage = (e: MessageEvent) => {
          if (!wsRef.current || wsRef.current.readyState !== WebSocket.OPEN || isPausedRef.current) return;
          wsRef.current.send(e.data);
        };

        workletNode.connect(audioContext.destination);
      } catch {
        const msg = "Failed to set up audio processing.";
        setError(msg);
        onErrorRef.current?.(msg);
        return false;
      }
    }

    // speech_final (endpointing) is the primary turn-completion signal.
    // Interview candidates naturally pause to think mid-answer (1-2s is common),
    // so endpointing must be generous to avoid cutting them off.
    // Bounded experiment: 700ms (was 800) — revert if cut-offs increase (see interview-voice-metrics.md).
    // UtteranceEnd (utterance_end_ms) is the fallback for noisy environments
    // where the VAD can't detect sufficient silence to trigger endpointing.
    const params = new URLSearchParams({
      model: "nova-3",
      encoding: "linear16",
      sample_rate: String(SAMPLE_RATE),
      interim_results: "true",
      endpointing: "700",
      utterance_end_ms: "1500",
      vad_events: "true",
      channels: "1",
    });

    return new Promise<boolean>((resolve) => {
      const ws = new WebSocket(`${DEEPGRAM_WS_BASE}?${params.toString()}`, ["bearer", token]);
      wsRef.current = ws;

      ws.onopen = () => {
        connectionReadyRef.current = true;
        resolve(true);
      };

      ws.onmessage = (event) => {
        if (isPausedRef.current) return;

        let data: any;
        try {
          data = JSON.parse(event.data);
        } catch {
          return;
        }

        if (data.type === "UtteranceEnd") {
          const fullTranscript = accumulatedTranscriptRef.current.trim();
          if (fullTranscript) {
            const endpointingMs = lastSpeechAtRef.current > 0
              ? Math.round(performance.now() - lastSpeechAtRef.current)
              : null;
            console.log(`[TIMING][STT] UtteranceEnd fallback fired — ${fullTranscript.split(/\s+/).length} words${endpointingMs != null ? `, ${endpointingMs}ms after last speech (endpointing overhead)` : ""}`);
            onFinalTranscriptRef.current(fullTranscript, lastSpeechAtRef.current);
            stopListening();
          }
          return;
        }

        if (data.type !== "Results") return;

        const transcript = data.channel?.alternatives?.[0]?.transcript || "";
        const isFinal: boolean = data.is_final === true;
        const speechFinal: boolean = data.speech_final === true;

        if (isFinal && transcript) {
          console.log(`[STT][diag] is_final=true speech_final=${speechFinal} transcript="${transcript}"`);
        }

        if (transcript.trim().length >= BARGE_IN_MIN_CHARS && !bargeInFiredRef.current) {
          bargeInFiredRef.current = true;
          onBargeInRef.current?.();
        }

        if (isFinal && transcript) {
          lastSpeechAtRef.current = performance.now();
          accumulatedTranscriptRef.current += (accumulatedTranscriptRef.current ? " " : "") + transcript;
          onInterimTranscriptRef.current?.(accumulatedTranscriptRef.current);
        } else if (!isFinal && transcript) {
          const display = accumulatedTranscriptRef.current
            ? accumulatedTranscriptRef.current + " " + transcript
            : transcript;
          onInterimTranscriptRef.current?.(display);
        }

        if (speechFinal) {
          const fullTranscript = accumulatedTranscriptRef.current.trim();
          if (fullTranscript) {
            const endpointingMs = lastSpeechAtRef.current > 0
              ? Math.round(performance.now() - lastSpeechAtRef.current)
              : null;
            console.log(`[TIMING][STT] speech_final fired — ${fullTranscript.split(/\s+/).length} words${endpointingMs != null ? `, ${endpointingMs}ms after last speech (endpointing overhead)` : ""}`);
            onFinalTranscriptRef.current(fullTranscript, lastSpeechAtRef.current);
            stopListening();
          }
        }
      };

      ws.onerror = () => {
        connectionReadyRef.current = false;
        if (!isPausedRef.current) {
          const msg = "Deepgram WebSocket connection error.";
          setError(msg);
          onErrorRef.current?.(msg);
        }
        resolve(false);
      };

      ws.onclose = (event) => {
        connectionReadyRef.current = false;
        stopKeepAlive();

        if (isListeningRef.current && !isPausedRef.current) {
          const fullTranscript = accumulatedTranscriptRef.current.trim();
          if (fullTranscript) {
            onFinalTranscriptRef.current(fullTranscript, lastSpeechAtRef.current);
          }
          isListeningRef.current = false;
          setIsListening(false);

          if (event.code !== 1000 && event.code !== 1001) {
            const msg = "Deepgram connection closed unexpectedly.";
            setError(msg);
            onErrorRef.current?.(msg);
          }
        }

        wsRef.current = null;
      };
    });
  }, [isSupported, stopListening, stopKeepAlive]);

  const startListening = useCallback(async () => {
    if (isListeningRef.current) return;
    if (!isSupported) {
      const msg = "Browser does not support required audio APIs (getUserMedia, WebSocket).";
      setError(msg);
      onErrorRef.current?.(msg);
      return;
    }

    setError(null);
    bargeInFiredRef.current = false;
    accumulatedTranscriptRef.current = "";
    lastSpeechAtRef.current = 0;
    isStoppingRef.current = false;

    const connected = await ensureConnection();
    if (!connected) return;

    // Resume: reconnect audio source to worklet to start sending audio
    isPausedRef.current = false;
    stopKeepAlive();

    if (sourceRef.current && workletNodeRef.current) {
      try { sourceRef.current.connect(workletNodeRef.current); } catch { /* already connected */ }
    }

    isListeningRef.current = true;
    setIsListening(true);
  }, [isSupported, ensureConnection, stopKeepAlive]);

  useEffect(() => {
    return () => {
      destroyConnection();
    };
  }, [destroyConnection]);

  return {
    isListening,
    isSupported,
    error,
    mediaStream,
    startListening,
    stopListening,
    destroyConnection,
  };
}
