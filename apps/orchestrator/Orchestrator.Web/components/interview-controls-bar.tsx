"use client";

import { RotateCcw, HelpCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { InterviewState } from "@/hooks/use-interview-state-machine";

interface InterviewControlsBarProps {
  state: InterviewState;
  repeatsRemaining: number;
  onRepeatQuestion: () => void;
  onOpenAccommodationModal: () => void;
}

export function InterviewControlsBar({
  state,
  repeatsRemaining,
  onRepeatQuestion,
  onOpenAccommodationModal,
}: InterviewControlsBarProps) {
  const showControls = [
    InterviewState.AI_SPEAKING,
    InterviewState.LISTENING,
    InterviewState.AI_THINKING,
  ].includes(state);

  if (!showControls) {
    return null;
  }

  const canRepeat = repeatsRemaining > 0;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-40 bg-black/80 backdrop-blur-sm border-t border-white/10">
      <div className="max-w-4xl mx-auto px-4 py-3">
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-2">
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
          </div>

          <div className="flex items-center gap-4">
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
