"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  Eye,
  Gauge,
  RotateCcw,
  Clock,
  FileText,
  Users,
  HelpCircle,
} from "lucide-react";
import { formatDuration } from "@/hooks/use-interview-state-machine";

interface InterviewAccommodationModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  showCaptions: boolean;
  playbackSpeed: 1.0 | 0.8;
  pauseTimeRemaining: number;
  totalPauseTimeUsed: number;
  onTurnOnCaptions: () => void;
  onTogglePlaybackSpeed: () => void;
  onRepeatQuestion: () => void;
  onSwitchToTextResponse: () => void;
  onHumanAlternative: () => void;
  onTechnicalHelp: () => void;
}

/**
 * Accommodation modal that provides accessibility options for candidates.
 * Only accessible via the "Request accommodation" button - not exposed as a normal option.
 */
export function InterviewAccommodationModal({
  open,
  onOpenChange,
  showCaptions,
  playbackSpeed,
  pauseTimeRemaining,
  totalPauseTimeUsed,
  onTurnOnCaptions,
  onTogglePlaybackSpeed,
  onRepeatQuestion,
  onSwitchToTextResponse,
  onHumanAlternative,
  onTechnicalHelp,
}: InterviewAccommodationModalProps) {
  const totalPauseRemaining = 120 - totalPauseTimeUsed;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Request Accommodation</DialogTitle>
          <DialogDescription>
            Choose an accommodation option to help you complete the interview.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 py-4">
          {/* Turn on captions */}
          {!showCaptions && (
            <Button
              variant="outline"
              className="w-full justify-start"
              onClick={() => {
                onTurnOnCaptions();
                onOpenChange(false);
              }}
            >
              <Eye className="w-4 h-4 mr-2" />
              Turn on captions
            </Button>
          )}

          {/* Slower playback */}
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onTogglePlaybackSpeed();
              onOpenChange(false);
            }}
          >
            <Gauge className="w-4 h-4 mr-2" />
            Slower playback ({playbackSpeed === 1.0 ? "Switch to 0.8x" : "Switch to 1.0x"})
          </Button>

          {/* Repeat question */}
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onRepeatQuestion();
              onOpenChange(false);
            }}
          >
            <RotateCcw className="w-4 h-4 mr-2" />
            Repeat question
          </Button>

          {/* Extra time */}
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              // Show explanation about pause limits
              alert(
                `Pause Limits:\n\n` +
                  `• Maximum 60 seconds per question\n` +
                  `• Total pause budget: 120 seconds for entire interview\n\n` +
                  `Current status:\n` +
                  `• Question time remaining: ${formatDuration(pauseTimeRemaining)}\n` +
                  `• Total time remaining: ${formatDuration(Math.max(0, totalPauseRemaining))}`
              );
            }}
          >
            <Clock className="w-4 h-4 mr-2" />
            Extra time (explains pause limits)
          </Button>

          {/* Switch to text response - IMPORTANT: Only in modal */}
          <Button
            variant="outline"
            className="w-full justify-start border-cyan-500/50 hover:bg-cyan-500/10"
            onClick={() => {
              onSwitchToTextResponse();
              onOpenChange(false);
            }}
          >
            <FileText className="w-4 h-4 mr-2" />
            Switch to text response (accommodation)
          </Button>

          {/* Human alternative */}
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onHumanAlternative();
              // Placeholder - show instructions
              alert(
                "Human Alternative:\n\n" +
                  "To schedule an interview with a human interviewer, please contact:\n" +
                  "• Email: support@example.com\n" +
                  "• Phone: (555) 123-4567\n\n" +
                  "We're happy to accommodate your needs."
              );
            }}
          >
            <Users className="w-4 h-4 mr-2" />
            Human alternative (contact/schedule)
          </Button>

          {/* Technical help */}
          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onTechnicalHelp();
              // Placeholder - show mic test instructions
              alert(
                "Technical Help / Mic Test:\n\n" +
                  "1. Check your microphone is connected\n" +
                  "2. Grant microphone permissions in your browser\n" +
                  "3. Test your microphone in your system settings\n" +
                  "4. Ensure you're in a quiet environment\n\n" +
                  "If issues persist, contact technical support:\n" +
                  "• Email: tech@example.com\n" +
                  "• Phone: (555) 123-4567"
              );
            }}
          >
            <HelpCircle className="w-4 h-4 mr-2" />
            Technical help / mic test
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
