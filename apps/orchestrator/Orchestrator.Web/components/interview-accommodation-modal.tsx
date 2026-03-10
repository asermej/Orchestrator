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
  RotateCcw,
  FileText,
  Users,
  HelpCircle,
} from "lucide-react";

interface InterviewAccommodationModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onRepeatQuestion: () => void;
  onSwitchToTextResponse: () => void;
  onHumanAlternative: () => void;
  onTechnicalHelp: () => void;
}

export function InterviewAccommodationModal({
  open,
  onOpenChange,
  onRepeatQuestion,
  onSwitchToTextResponse,
  onHumanAlternative,
  onTechnicalHelp,
}: InterviewAccommodationModalProps) {
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

          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onHumanAlternative();
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

          <Button
            variant="outline"
            className="w-full justify-start"
            onClick={() => {
              onTechnicalHelp();
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
