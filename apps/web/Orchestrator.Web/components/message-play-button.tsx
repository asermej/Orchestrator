"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Volume2, Loader2, VolumeX } from "lucide-react";
import { useStreamingAudio } from "@/hooks/use-streaming-audio";
import { toast } from "sonner";

interface MessagePlayButtonProps {
  messageId: string;
  className?: string;
}

/**
 * Play button for TTS replay of a message.
 * Uses cached audio when available.
 */
export function MessagePlayButton({ messageId, className = "" }: MessagePlayButtonProps) {
  const [isLoading, setIsLoading] = useState(false);
  
  const { audioRef, isPlaying, playMessageAudio, stopPlayback, error } = useStreamingAudio({
    onError: (err) => {
      if (err.includes("disabled")) {
        toast.error("Voice is currently disabled");
      } else {
        toast.error("Failed to play audio");
      }
    },
  });

  const handleClick = async () => {
    if (isPlaying) {
      stopPlayback();
      return;
    }

    setIsLoading(true);
    try {
      await playMessageAudio(messageId);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <audio ref={audioRef} className="hidden" />
      <Button
        variant="ghost"
        size="sm"
        className={`h-6 w-6 p-0 opacity-60 hover:opacity-100 ${className}`}
        onClick={handleClick}
        disabled={isLoading}
        title={isPlaying ? "Stop" : "Play message"}
      >
        {isLoading ? (
          <Loader2 className="h-3 w-3 animate-spin" />
        ) : isPlaying ? (
          <VolumeX className="h-3 w-3" />
        ) : (
          <Volume2 className="h-3 w-3" />
        )}
      </Button>
    </>
  );
}
