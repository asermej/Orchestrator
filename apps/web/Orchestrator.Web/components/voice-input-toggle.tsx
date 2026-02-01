"use client";

import { Mic, MicOff, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface VoiceInputToggleProps {
  isRecording: boolean;
  isSupported: boolean;
  error: string | null;
  onToggle: () => void;
  disabled?: boolean;
  className?: string;
}

export function VoiceInputToggle({
  isRecording,
  isSupported,
  error,
  onToggle,
  disabled = false,
  className,
}: VoiceInputToggleProps) {
  if (!isSupported) {
    return (
      <div className={cn("flex items-center gap-2 text-sm text-muted-foreground", className)}>
        <AlertCircle className="h-4 w-4" />
        <span>Voice input not supported in this browser</span>
      </div>
    );
  }

  return (
    <div className={cn("flex items-center gap-3", className)}>
      {/* Mode Indicator */}
      <div className="flex items-center gap-2 text-sm">
        <span className="text-muted-foreground">Input Mode:</span>
        <span className={cn(
          "font-medium px-2 py-0.5 rounded-md transition-colors",
          isRecording 
            ? "bg-red-100 dark:bg-red-950 text-red-700 dark:text-red-300" 
            : "bg-muted text-muted-foreground"
        )}>
          {isRecording ? "Voice" : "Type"}
        </span>
      </div>

      {/* Recording Button */}
      <Button
        type="button"
        variant={isRecording ? "destructive" : "outline"}
        size="sm"
        onClick={onToggle}
        disabled={disabled || !isSupported}
        className={cn(
          "relative transition-all",
          isRecording && "animate-pulse"
        )}
      >
        {isRecording ? (
          <>
            <MicOff className="h-4 w-4 mr-2" />
            Stop Recording
          </>
        ) : (
          <>
            <Mic className="h-4 w-4 mr-2" />
            Start Recording
          </>
        )}
        
        {/* Pulsing indicator when recording */}
        {isRecording && (
          <span className="absolute -top-1 -right-1 flex h-3 w-3">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-3 w-3 bg-red-500"></span>
          </span>
        )}
      </Button>

      {/* Error Message */}
      {error && !isRecording && (
        <div className="flex items-center gap-1 text-sm text-destructive">
          <AlertCircle className="h-4 w-4" />
          <span>{error}</span>
        </div>
      )}
    </div>
  );
}

