"use client";

import { useRef, useEffect, useCallback } from "react";
import { motion } from "framer-motion";

interface AudioVisualizerProps {
  audioElement: HTMLAudioElement | null;
  isPlaying: boolean;
  isListening?: boolean;
  isProcessing?: boolean;
  size?: number;
  className?: string;
}

/**
 * Audio visualizer component that displays a glowing ring animation.
 * The ring responds to audio frequencies when playing, pulses when listening,
 * and spins when processing.
 */
export function AudioVisualizer({
  audioElement,
  isPlaying,
  isListening = false,
  isProcessing = false,
  size = 300,
  className = "",
}: AudioVisualizerProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const sourceRef = useRef<MediaElementAudioSourceNode | null>(null);
  const animationFrameRef = useRef<number | null>(null);
  const connectedRef = useRef<boolean>(false);

  // Initialize Web Audio API when audio element is available
  useEffect(() => {
    if (!audioElement || connectedRef.current) return;

    try {
      // Create or resume audio context
      audioContextRef.current = new (window.AudioContext || (window as any).webkitAudioContext)();
      analyserRef.current = audioContextRef.current.createAnalyser();
      analyserRef.current.fftSize = 256;
      analyserRef.current.smoothingTimeConstant = 0.8;

      // Connect audio element to analyser
      sourceRef.current = audioContextRef.current.createMediaElementSource(audioElement);
      sourceRef.current.connect(analyserRef.current);
      analyserRef.current.connect(audioContextRef.current.destination);
      connectedRef.current = true;
    } catch (error) {
      console.error("Failed to initialize audio context:", error);
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [audioElement]);

  // Draw the visualizer
  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const radius = Math.min(centerX, centerY) * 0.7;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Get frequency data if playing
    let audioLevel = 0;
    if (isPlaying && analyserRef.current) {
      const bufferLength = analyserRef.current.frequencyBinCount;
      const dataArray = new Uint8Array(bufferLength);
      analyserRef.current.getByteFrequencyData(dataArray);

      // Calculate average audio level
      const sum = dataArray.reduce((a, b) => a + b, 0);
      audioLevel = sum / bufferLength / 255;
    }

    // Determine ring properties based on state
    let glowIntensity = 0.3;
    let ringWidth = 4;
    let pulseScale = 1;

    if (isPlaying) {
      glowIntensity = 0.5 + audioLevel * 0.5;
      ringWidth = 4 + audioLevel * 8;
      pulseScale = 1 + audioLevel * 0.1;
    } else if (isListening) {
      glowIntensity = 0.6;
      ringWidth = 6;
    } else if (isProcessing) {
      glowIntensity = 0.4;
      ringWidth = 5;
    }

    const currentRadius = radius * pulseScale;

    // Draw outer glow
    const gradient = ctx.createRadialGradient(
      centerX,
      centerY,
      currentRadius - ringWidth * 2,
      centerX,
      centerY,
      currentRadius + ringWidth * 3
    );
    gradient.addColorStop(0, `rgba(0, 200, 255, 0)`);
    gradient.addColorStop(0.4, `rgba(0, 200, 255, ${glowIntensity * 0.3})`);
    gradient.addColorStop(0.6, `rgba(100, 150, 255, ${glowIntensity * 0.5})`);
    gradient.addColorStop(1, `rgba(150, 100, 255, 0)`);

    ctx.beginPath();
    ctx.arc(centerX, centerY, currentRadius + ringWidth, 0, Math.PI * 2);
    ctx.fillStyle = gradient;
    ctx.fill();

    // Draw main ring with gradient
    const ringGradient = ctx.createLinearGradient(
      centerX - currentRadius,
      centerY - currentRadius,
      centerX + currentRadius,
      centerY + currentRadius
    );
    ringGradient.addColorStop(0, `rgba(0, 220, 255, ${0.8 + glowIntensity * 0.2})`);
    ringGradient.addColorStop(0.5, `rgba(100, 150, 255, ${0.9 + glowIntensity * 0.1})`);
    ringGradient.addColorStop(1, `rgba(150, 100, 255, ${0.8 + glowIntensity * 0.2})`);

    ctx.beginPath();
    ctx.arc(centerX, centerY, currentRadius, 0, Math.PI * 2);
    ctx.strokeStyle = ringGradient;
    ctx.lineWidth = ringWidth;
    ctx.lineCap = "round";
    ctx.stroke();

    // Draw particle effects when playing
    if (isPlaying && audioLevel > 0.1) {
      const particleCount = Math.floor(audioLevel * 20);
      for (let i = 0; i < particleCount; i++) {
        const angle = (Math.PI * 2 * i) / particleCount + Date.now() * 0.001;
        const particleRadius = currentRadius + ringWidth * 2 + Math.random() * 20 * audioLevel;
        const x = centerX + Math.cos(angle) * particleRadius;
        const y = centerY + Math.sin(angle) * particleRadius;
        const particleSize = 1 + Math.random() * 2 * audioLevel;

        ctx.beginPath();
        ctx.arc(x, y, particleSize, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(0, 220, 255, ${0.3 + Math.random() * 0.4})`;
        ctx.fill();
      }
    }

    // Continue animation
    animationFrameRef.current = requestAnimationFrame(draw);
  }, [isPlaying, isListening, isProcessing]);

  // Start/stop animation based on state
  useEffect(() => {
    if (isPlaying || isListening || isProcessing) {
      // Resume audio context if suspended
      if (audioContextRef.current?.state === "suspended") {
        audioContextRef.current.resume();
      }
      animationFrameRef.current = requestAnimationFrame(draw);
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [isPlaying, isListening, isProcessing, draw]);

  // Idle animation when nothing is happening
  useEffect(() => {
    if (!isPlaying && !isListening && !isProcessing) {
      // Draw idle state with subtle pulse
      const drawIdle = () => {
        const canvas = canvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        if (!ctx) return;

        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2;
        const radius = Math.min(centerX, centerY) * 0.7;
        const pulse = Math.sin(Date.now() * 0.002) * 0.05 + 1;
        const currentRadius = radius * pulse;

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Dim outer glow
        const gradient = ctx.createRadialGradient(
          centerX,
          centerY,
          currentRadius - 10,
          centerX,
          centerY,
          currentRadius + 20
        );
        gradient.addColorStop(0, "rgba(0, 200, 255, 0)");
        gradient.addColorStop(0.5, "rgba(0, 200, 255, 0.1)");
        gradient.addColorStop(1, "rgba(150, 100, 255, 0)");

        ctx.beginPath();
        ctx.arc(centerX, centerY, currentRadius + 10, 0, Math.PI * 2);
        ctx.fillStyle = gradient;
        ctx.fill();

        // Dim ring
        ctx.beginPath();
        ctx.arc(centerX, centerY, currentRadius, 0, Math.PI * 2);
        ctx.strokeStyle = "rgba(0, 200, 255, 0.4)";
        ctx.lineWidth = 3;
        ctx.stroke();

        animationFrameRef.current = requestAnimationFrame(drawIdle);
      };

      animationFrameRef.current = requestAnimationFrame(drawIdle);
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [isPlaying, isListening, isProcessing]);

  return (
    <motion.div
      className={`relative ${className}`}
      animate={isProcessing ? { rotate: 360 } : { rotate: 0 }}
      transition={
        isProcessing
          ? { duration: 2, repeat: Infinity, ease: "linear" }
          : { duration: 0 }
      }
    >
      <canvas
        ref={canvasRef}
        width={size}
        height={size}
        className="w-full h-full"
        style={{ width: size, height: size }}
      />
    </motion.div>
  );
}
