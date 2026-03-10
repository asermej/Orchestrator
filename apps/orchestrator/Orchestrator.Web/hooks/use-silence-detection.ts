"use client";

import { useRef, useEffect, useCallback } from "react";

interface SilenceDetectionOptions {
  mediaStream: MediaStream | null;
  enabled: boolean;
  silenceThresholdDb: number;
  turnCompleteMs: number;
  idlePromptMs: number;
  idleSkipMs: number;
  maxSpeechDurationMs: number;
  onTurnComplete: () => void;
  onIdlePrompt: () => void;
  onIdleSkip: () => void;
}

const DEFAULT_SILENCE_THRESHOLD_DB = -45;
const DEFAULT_TURN_COMPLETE_MS = 2000;
const DEFAULT_IDLE_PROMPT_MS = 8000;
const DEFAULT_IDLE_SKIP_MS = 20000;
const DEFAULT_MAX_SPEECH_DURATION_MS = 60000;

export function useSilenceDetection({
  mediaStream,
  enabled,
  silenceThresholdDb = DEFAULT_SILENCE_THRESHOLD_DB,
  turnCompleteMs = DEFAULT_TURN_COMPLETE_MS,
  idlePromptMs = DEFAULT_IDLE_PROMPT_MS,
  idleSkipMs = DEFAULT_IDLE_SKIP_MS,
  maxSpeechDurationMs = DEFAULT_MAX_SPEECH_DURATION_MS,
  onTurnComplete,
  onIdlePrompt,
  onIdleSkip,
}: SilenceDetectionOptions) {
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const sourceRef = useRef<MediaStreamAudioSourceNode | null>(null);
  const rafRef = useRef<number | null>(null);

  const lastSpeechTimeRef = useRef<number>(0);
  const firstSpeechTimeRef = useRef<number>(0);
  const hasSpokenRef = useRef(false);
  const turnCompleteFiredRef = useRef(false);
  const idlePromptFiredRef = useRef(false);
  const idleSkipFiredRef = useRef(false);
  const listeningStartTimeRef = useRef<number>(0);

  const onTurnCompleteRef = useRef(onTurnComplete);
  const onIdlePromptRef = useRef(onIdlePrompt);
  const onIdleSkipRef = useRef(onIdleSkip);

  useEffect(() => { onTurnCompleteRef.current = onTurnComplete; }, [onTurnComplete]);
  useEffect(() => { onIdlePromptRef.current = onIdlePrompt; }, [onIdlePrompt]);
  useEffect(() => { onIdleSkipRef.current = onIdleSkip; }, [onIdleSkip]);

  const reset = useCallback(() => {
    lastSpeechTimeRef.current = 0;
    firstSpeechTimeRef.current = 0;
    hasSpokenRef.current = false;
    turnCompleteFiredRef.current = false;
    idlePromptFiredRef.current = false;
    idleSkipFiredRef.current = false;
    listeningStartTimeRef.current = Date.now();
  }, []);

  const signalSpeech = useCallback(() => {
    const now = Date.now();
    lastSpeechTimeRef.current = now;
    if (!hasSpokenRef.current) {
      firstSpeechTimeRef.current = now;
    }
    hasSpokenRef.current = true;
    turnCompleteFiredRef.current = false;
  }, []);

  useEffect(() => {
    if (!enabled || !mediaStream) {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = null;
      }
      if (audioContextRef.current) {
        audioContextRef.current.close().catch(() => {});
        audioContextRef.current = null;
      }
      analyserRef.current = null;
      sourceRef.current = null;
      return;
    }

    const audioContext = new AudioContext();
    const analyser = audioContext.createAnalyser();
    analyser.fftSize = 512;
    analyser.smoothingTimeConstant = 0.3;

    const source = audioContext.createMediaStreamSource(mediaStream);
    source.connect(analyser);

    audioContextRef.current = audioContext;
    analyserRef.current = analyser;
    sourceRef.current = source;

    reset();

    const dataArray = new Float32Array(analyser.fftSize);

    const tick = () => {
      if (!analyserRef.current) return;

      analyserRef.current.getFloatTimeDomainData(dataArray);

      let sumSquares = 0;
      for (let i = 0; i < dataArray.length; i++) {
        sumSquares += dataArray[i] * dataArray[i];
      }
      const rms = Math.sqrt(sumSquares / dataArray.length);
      const db = rms > 0 ? 20 * Math.log10(rms) : -100;

      const now = Date.now();
      const isSpeaking = db > silenceThresholdDb;

      if (isSpeaking) {
        lastSpeechTimeRef.current = now;
        if (!hasSpokenRef.current) {
          firstSpeechTimeRef.current = now;
        }
        hasSpokenRef.current = true;
        turnCompleteFiredRef.current = false;
      }

      if (hasSpokenRef.current && !turnCompleteFiredRef.current) {
        const silenceDuration = now - lastSpeechTimeRef.current;
        const speechDuration = now - firstSpeechTimeRef.current;

        if (silenceDuration >= turnCompleteMs || speechDuration >= maxSpeechDurationMs) {
          turnCompleteFiredRef.current = true;
          onTurnCompleteRef.current();
          rafRef.current = requestAnimationFrame(tick);
          return;
        }
      }

      if (!hasSpokenRef.current) {
        const waitingDuration = now - listeningStartTimeRef.current;

        if (!idlePromptFiredRef.current && waitingDuration >= idlePromptMs) {
          idlePromptFiredRef.current = true;
          onIdlePromptRef.current();
        }

        if (!idleSkipFiredRef.current && waitingDuration >= idleSkipMs) {
          idleSkipFiredRef.current = true;
          onIdleSkipRef.current();
          return;
        }
      }

      rafRef.current = requestAnimationFrame(tick);
    };

    rafRef.current = requestAnimationFrame(tick);

    return () => {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = null;
      }
      source.disconnect();
      audioContext.close().catch(() => {});
      audioContextRef.current = null;
      analyserRef.current = null;
      sourceRef.current = null;
    };
  }, [enabled, mediaStream, silenceThresholdDb, turnCompleteMs, idlePromptMs, idleSkipMs, maxSpeechDurationMs, reset]);

  return { reset, signalSpeech };
}
