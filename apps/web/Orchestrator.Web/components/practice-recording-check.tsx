"use client";

import { Button } from "@/components/ui/button";
import { Mic, Square, RotateCcw } from "lucide-react";
import { usePracticeRecording } from "@/hooks/use-practice-recording";
import { motion, AnimatePresence } from "framer-motion";
import { formatDuration } from "@/hooks/use-interview-state-machine";
import { useState, useEffect } from "react";

interface PracticeRecordingCheckProps {
  onPracticePrompt?: () => Promise<void>;
}

const PRACTICE_PROMPT_TEXT = "Please tell us about yourself in a few sentences.";
const PRACTICE_DURATION_SEC = 10;

/**
 * Practice recording component that plays a practice prompt and records user response.
 * Shows live transcript preview and indicates this is practice (not saved).
 */
export function PracticeRecordingCheck({
  onPracticePrompt,
}: PracticeRecordingCheckProps) {
  const { isRecording, transcript, startPractice, stopPractice, clearPractice } =
    usePracticeRecording();
  const [recordingDuration, setRecordingDuration] = useState(0);
  const [hasPlayedPrompt, setHasPlayedPrompt] = useState(false);

  // Track recording duration
  useEffect(() => {
    let interval: NodeJS.Timeout | null = null;
    if (isRecording) {
      interval = setInterval(() => {
        setRecordingDuration((prev) => {
          const newDuration = prev + 1;
          if (newDuration >= PRACTICE_DURATION_SEC) {
            stopPractice();
            return PRACTICE_DURATION_SEC;
          }
          return newDuration;
        });
      }, 1000);
    } else {
      setRecordingDuration(0);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isRecording, stopPractice]);

  const handleStartPractice = async () => {
    // Clear any previous transcript first
    clearPractice();
    
    // Play practice prompt first and wait for it to finish
    if (onPracticePrompt) {
      await onPracticePrompt();
    }
    setHasPlayedPrompt(true);
    
    // Wait a moment after audio ends, then start recording
    // This ensures the mic doesn't pick up any echo/feedback from the prompt
    setTimeout(() => {
      // Clear transcript again in case anything was captured during prompt
      clearPractice();
      startPractice();
    }, 300);
  };

  const handleStopPractice = () => {
    stopPractice();
  };

  const handleClearPractice = () => {
    clearPractice();
    setHasPlayedPrompt(false);
  };

  return (
    <div className="w-full space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Mic className="w-5 h-5 text-white/70" />
          <span className="text-white/90 font-medium">Practice Recording</span>
        </div>

        <AnimatePresence mode="wait">
          {!hasPlayedPrompt && !isRecording && !transcript && (
            <motion.div
              key="start"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
            >
              <Button
                onClick={handleStartPractice}
                size="sm"
                className="bg-cyan-600 hover:bg-cyan-700 text-white"
              >
                Start Practice
              </Button>
            </motion.div>
          )}

          {isRecording && (
            <motion.div
              key="recording"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-2"
            >
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
                <span className="text-white/70 text-sm font-mono">
                  {formatDuration(recordingDuration)}
                </span>
              </div>
              <Button
                onClick={handleStopPractice}
                size="sm"
                variant="ghost"
                className="text-white/60 hover:text-white/80"
              >
                <Square className="w-4 h-4" />
              </Button>
            </motion.div>
          )}

          {!isRecording && transcript && (
            <motion.div
              key="complete"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-2"
            >
              <Button
                onClick={handleClearPractice}
                size="sm"
                variant="ghost"
                className="text-white/60 hover:text-white/80"
              >
                <RotateCcw className="w-4 h-4 mr-1" />
                Try Again
              </Button>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Practice Notice */}
      {!isRecording && !transcript && (
        <p className="text-xs text-white/50">
          Practice your response to get familiar with the recording process
        </p>
      )}

      {/* Recording Indicator */}
      {isRecording && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-3 bg-white/5 border border-white/10 rounded-lg"
        >
          <div className="flex items-center justify-between mb-2">
            <p className="text-sm text-white/70">Recording practice response...</p>
            <p className="text-xs text-white/50 font-mono">
              {formatDuration(PRACTICE_DURATION_SEC - recordingDuration)} remaining
            </p>
          </div>
          <div className="h-1 bg-white/10 rounded-full overflow-hidden">
            <motion.div
              className="h-full bg-cyan-500 rounded-full"
              initial={{ width: 0 }}
              animate={{ width: `${(recordingDuration / PRACTICE_DURATION_SEC) * 100}%` }}
              transition={{ duration: 0.3 }}
            />
          </div>
        </motion.div>
      )}

      {/* Transcript Preview */}
      {transcript && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-3 bg-white/5 border border-cyan-500/30 rounded-lg space-y-2"
        >
          <div className="flex items-center justify-between">
            <p className="text-xs text-cyan-400 uppercase tracking-wide">Practice Transcript</p>
            <p className="text-xs text-white/40 italic">Not saved</p>
          </div>
          <p className="text-sm text-white/90">{transcript || "No speech detected"}</p>
        </motion.div>
      )}

      {/* Info Message */}
      {transcript && !isRecording && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="p-2 bg-blue-500/10 border border-blue-500/30 rounded-lg"
        >
          <p className="text-xs text-blue-300">
            This was a practice recording. Your actual interview responses will be saved.
          </p>
        </motion.div>
      )}
    </div>
  );
}
