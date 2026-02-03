"use client";

import { RotateCcw, Gauge, Eye, EyeOff, Pause, HelpCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { InterviewState } from "@/hooks/use-interview-state-machine";
import { formatDuration } from "@/hooks/use-interview-state-machine";

interface InterviewControlsBarProps {
  state: InterviewState;
  pauseTimeRemaining: number;
  totalPauseTimeUsed: number;
  playbackSpeed: 1.0 | 0.8;
  showCaptions: boolean;
  repeatsRemaining: number;
  onRepeatQuestion: () => void;
  onTogglePlaybackSpeed: () => void;
  onToggleCaptions: () => void;
  onPause: () => void;
  onOpenAccommodationModal: () => void;
}

/**
 * Controls bar component that provides always-visible candidate-first controls
 * during question/answer screens.
 */
export function InterviewControlsBar({
  state,
  pauseTimeRemaining,
  totalPauseTimeUsed,
  playbackSpeed,
  showCaptions,
  repeatsRemaining,
  onRepeatQuestion,
  onTogglePlaybackSpeed,
  onToggleCaptions,
  onPause,
  onOpenAccommodationModal,
}: InterviewControlsBarProps) {
  // Only show controls during question/answer screens
  const showControls = [
    InterviewState.QUESTION_PLAYING,
    InterviewState.RECORDING,
    InterviewState.PAUSED,
    InterviewState.REVIEW,
  ].includes(state);

  if (!showControls) {
    return null;
  }

  const canPause = state === InterviewState.RECORDING;
  const pauseDisabled = pauseTimeRemaining <= 0 || totalPauseTimeUsed >= 120;
  const totalPauseRemaining = 120 - totalPauseTimeUsed;
  const canRepeat = repeatsRemaining > 0;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-40 bg-black/80 backdrop-blur-sm border-t border-white/10">
      <div className="max-w-4xl mx-auto px-4 py-3">
        <div className="flex items-center justify-between gap-4">
          {/* Left side: Main controls */}
          <div className="flex items-center gap-2">
            {/* Repeat Question */}
            <div className="relative group">
              <Button
                variant="ghost"
                size="sm"
                onClick={onRepeatQuestion}
                disabled={!canRepeat}
                className="text-white/70 hover:text-white hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed"
                aria-label={
                  canRepeat
                    ? `Repeat question (${repeatsRemaining} left)`
                    : "Repeat limit reached"
                }
                title={
                  canRepeat
                    ? `Repeat question (${repeatsRemaining} left)`
                    : "Repeat limit reached. You can request an accommodation for additional support."
                }
              >
                <RotateCcw className="w-4 h-4" />
                <span className="hidden sm:inline">
                  Repeat {canRepeat && `(${repeatsRemaining} left)`}
                </span>
              </Button>
              {!canRepeat && (
                <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-3 py-1.5 bg-white/90 text-black text-xs rounded shadow-lg whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity z-50">
                  Repeat limit reached. You can request an accommodation for additional support.
                  <div className="absolute top-full left-1/2 -translate-x-1/2 -mt-1 border-4 border-transparent border-t-white/90"></div>
                </div>
              )}
            </div>

            {/* Playback Speed Toggle */}
            <Button
              variant="ghost"
              size="sm"
              onClick={onTogglePlaybackSpeed}
              className="text-white/70 hover:text-white hover:bg-white/10"
              aria-label={`Playback speed: ${playbackSpeed}x`}
              title={`Playback speed: ${playbackSpeed}x`}
            >
              <Gauge className="w-4 h-4" />
              <span className="hidden sm:inline">{playbackSpeed}x</span>
            </Button>

            {/* Captions Toggle */}
            <Button
              variant="ghost"
              size="sm"
              onClick={onToggleCaptions}
              className="text-white/70 hover:text-white hover:bg-white/10"
              aria-label={showCaptions ? "Hide captions" : "Show captions"}
              title={showCaptions ? "Hide captions" : "Show captions"}
            >
              {showCaptions ? (
                <>
                  <Eye className="w-4 h-4" />
                  <span className="hidden sm:inline">Captions</span>
                </>
              ) : (
                <>
                  <EyeOff className="w-4 h-4" />
                  <span className="hidden sm:inline">Show Captions</span>
                </>
              )}
            </Button>

            {/* Pause Button (only when recording) */}
            {canPause && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onPause}
                disabled={pauseDisabled}
                className="text-white/70 hover:text-white hover:bg-white/10 disabled:opacity-30"
                aria-label="Pause recording"
                title={
                  pauseDisabled
                    ? "Pause limit reached"
                    : `Pause (${formatDuration(pauseTimeRemaining)} remaining)`
                }
              >
                <Pause className="w-4 h-4" />
                <span className="hidden sm:inline">Pause</span>
              </Button>
            )}
          </div>

          {/* Right side: Pause info and accommodation */}
          <div className="flex items-center gap-4">
            {/* Pause time info (when paused or recording) */}
            {(state === InterviewState.PAUSED || state === InterviewState.RECORDING) && (
              <div className="hidden md:flex items-center gap-2 text-xs text-white/50">
                <span>
                  Question: {formatDuration(pauseTimeRemaining)} | Total:{" "}
                  {formatDuration(Math.max(0, totalPauseRemaining))}
                </span>
              </div>
            )}

            {/* Request Accommodation */}
            <Button
              variant="ghost"
              size="sm"
              onClick={onOpenAccommodationModal}
              className="text-white/70 hover:text-white hover:bg-white/10"
              aria-label="Request accommodation"
              title="Request accommodation"
            >
              <HelpCircle className="w-4 h-4" />
              <span className="hidden sm:inline">Accommodation</span>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
