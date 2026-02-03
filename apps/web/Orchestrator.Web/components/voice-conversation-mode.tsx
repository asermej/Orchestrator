"use client";

import { useState, useCallback, useEffect, useRef } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Mic, MicOff, Volume2, VolumeX } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AudioVisualizer } from "@/components/audio-visualizer";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { useStreamingAudio } from "@/hooks/use-streaming-audio";
import { AgentAvatar } from "@/components/agent-avatar";

type ConversationState = "idle" | "listening" | "processing" | "speaking";

interface VoiceConversationModeProps {
  isOpen: boolean;
  onClose: () => void;
  chatId: string;
  agentId: string;
  agentName: string;
  agentImageUrl?: string | null;
  onMessageSent?: (userMessage: string, aiResponse: string) => void;
}

/**
 * Full-screen voice conversation mode with audio visualizer.
 * Handles the complete voice conversation flow:
 * idle -> listening -> processing -> speaking -> listening (loop)
 */
export function VoiceConversationMode({
  isOpen,
  onClose,
  chatId,
  agentId,
  agentName,
  agentImageUrl,
  onMessageSent,
}: VoiceConversationModeProps) {
  const [state, setState] = useState<ConversationState>("idle");
  const [transcript, setTranscript] = useState<string>("");
  const [autoListen, setAutoListen] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const transcriptRef = useRef<string>("");
  const stateRef = useRef<ConversationState>(state);
  stateRef.current = state;

  const {
    audioRef,
    isStreaming,
    isPlaying,
    error: audioError,
    streamResponse,
    stopPlayback,
  } = useStreamingAudio({
    onPlayStart: () => setState("speaking"),
    onPlayEnd: () => {
      setState("idle");
      // Auto-continue listening if enabled
      if (autoListen) {
        setTimeout(() => {
          startListening();
        }, 500);
      }
    },
    onError: (err) => setError(err),
  });

  const handleTranscript = useCallback((text: string) => {
    setTranscript((prev) => {
      const newTranscript = prev + " " + text;
      transcriptRef.current = newTranscript;
      return newTranscript;
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
    onError: (err) => setError(err),
    continuous: true,
    interimResults: false,
  });

  // Handle sending the message when recording stops
  const sendMessage = useCallback(async () => {
    // Use ref to get latest transcript value (avoids stale closure)
    const message = transcriptRef.current.trim();
    if (!message) {
      setState("idle");
      setError("No speech detected. Speak first, then tap the mic again to send.");
      return;
    }

    setState("processing");
    setError(null);
    setTranscript("");
    transcriptRef.current = "";

    try {
      await streamResponse(chatId, agentId, message);
      // Note: state will be set to "speaking" by onPlayStart callback
    } catch (err) {
      setState("idle");
      setError(err instanceof Error ? err.message : "Failed to get response");
    }
  }, [chatId, agentId, streamResponse]);

  // Start listening
  const startListening = useCallback(() => {
    if (!isSupported) {
      setError("Voice input is not supported in this browser");
      return;
    }
    setTranscript("");
    transcriptRef.current = "";
    setError(null);
    startRecording();
    setState("listening");
  }, [isSupported, startRecording]);

  // Stop listening, then send when recognition has ended (so final transcript is in ref).
  // Short delay after onend lets any final onresult run first; fallback if onend never fires.
  const stopListeningAndSend = useCallback(() => {
    let sent = false;
    const doSend = () => {
      if (sent) return;
      sent = true;
      // Small delay so browser can deliver final transcript in onresult after stop()
      setTimeout(() => sendMessage(), 150);
    };
    const fallbackTimer = setTimeout(doSend, 600);
    stopRecording(() => {
      clearTimeout(fallbackTimer);
      doSend();
    });
  }, [stopRecording, sendMessage]);

  // Toggle listening
  const toggleListening = useCallback(() => {
    if (state === "listening") {
      stopListeningAndSend();
    } else if (state === "idle") {
      startListening();
    } else if (state === "speaking") {
      // Interrupt and start listening
      stopPlayback();
      startListening();
    }
  }, [state, startListening, stopListeningAndSend, stopPlayback]);

  // Handle close
  const handleClose = useCallback(() => {
    stopRecording();
    stopPlayback();
    setState("idle");
    setTranscript("");
    onClose();
  }, [stopRecording, stopPlayback, onClose]);

  // Keyboard shortcut: Space to toggle (use ref so handler always has latest state)
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.code === "Space" && !e.repeat) {
        e.preventDefault();
        const currentState = stateRef.current;
        if (currentState === "listening") {
          stopListeningAndSend();
        } else if (currentState === "idle") {
          startListening();
        } else if (currentState === "speaking") {
          stopPlayback();
          startListening();
        }
      }
      if (e.code === "Escape") {
        handleClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown, true);
    return () => window.removeEventListener("keydown", handleKeyDown, true);
  }, [isOpen, startListening, stopListeningAndSend, stopPlayback, handleClose]);

  // Update state based on recording
  useEffect(() => {
    if (isRecording && state !== "listening") {
      setState("listening");
    }
  }, [isRecording, state]);

  // Combine errors
  const displayError = error || audioError || voiceError;

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-black"
        >
          {/* Hidden audio element */}
          <audio ref={audioRef} className="hidden" />

          {/* Close button */}
          <Button
            variant="ghost"
            size="icon"
            className="absolute top-4 right-4 text-white/70 hover:text-white hover:bg-white/10"
            onClick={handleClose}
          >
            <X className="h-6 w-6" />
          </Button>

          {/* Agent info */}
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
            <p className="text-sm text-white/60">
              {state === "idle" && "Tap to speak"}
              {state === "listening" && "Listening..."}
              {state === "processing" && "Thinking..."}
              {state === "speaking" && "Speaking..."}
            </p>
          </motion.div>

          {/* Audio visualizer */}
          <motion.div
            initial={{ scale: 0.8, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            className="relative"
          >
            <AudioVisualizer
              audioElement={audioRef.current}
              isPlaying={isPlaying}
              isListening={state === "listening"}
              isProcessing={state === "processing" || isStreaming}
              size={300}
            />

            {/* Center button overlay */}
            <button
              onClick={toggleListening}
              className="absolute inset-0 flex items-center justify-center cursor-pointer"
              disabled={state === "processing"}
            >
              <motion.div
                whileHover={{ scale: 1.1 }}
                whileTap={{ scale: 0.95 }}
                className={`
                  w-20 h-20 rounded-full flex items-center justify-center
                  transition-colors duration-300
                  ${state === "listening" 
                    ? "bg-red-500/80" 
                    : state === "speaking"
                    ? "bg-cyan-500/80"
                    : "bg-white/20 hover:bg-white/30"
                  }
                `}
              >
                {state === "listening" ? (
                  <MicOff className="w-8 h-8 text-white" />
                ) : state === "speaking" ? (
                  <Volume2 className="w-8 h-8 text-white" />
                ) : (
                  <Mic className="w-8 h-8 text-white" />
                )}
              </motion.div>
            </button>
          </motion.div>

          {/* Transcript display */}
          {transcript && (
            <motion.div
              initial={{ y: 20, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              className="mt-8 px-6 py-3 bg-white/10 rounded-lg max-w-md"
            >
              <p className="text-white/80 text-center">{transcript}</p>
            </motion.div>
          )}

          {/* Error display */}
          {displayError && (
            <motion.div
              initial={{ y: 20, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              className="mt-4 px-4 py-2 bg-red-500/20 border border-red-500/40 rounded-lg"
            >
              <p className="text-red-400 text-sm">{displayError}</p>
            </motion.div>
          )}

          {/* Controls */}
          <div className="absolute bottom-8 flex items-center gap-4">
            <Button
              variant="outline"
              size="sm"
              className="text-white/70 border-white/20 hover:bg-white/10"
              onClick={() => setAutoListen(!autoListen)}
            >
              {autoListen ? "Auto-listen: ON" : "Auto-listen: OFF"}
            </Button>
            
            <p className="text-white/40 text-xs">
              Press Space to {state === "listening" ? "send" : "speak"}
            </p>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
