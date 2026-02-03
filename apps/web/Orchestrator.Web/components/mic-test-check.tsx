"use client";

import React, { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Mic, CheckCircle2, XCircle, AlertCircle } from "lucide-react";
import { useMicTest } from "@/hooks/use-mic-test";
import { motion, AnimatePresence } from "framer-motion";

interface MicTestCheckProps {
  onTestComplete?: (passed: boolean) => void;
  onRequestAccommodation?: () => void;
}

/**
 * Mic test component that displays mic test UI with input level meter.
 * Shows pass/fail status with guidance and handles permission errors.
 */
export function MicTestCheck({
  onTestComplete,
  onRequestAccommodation,
}: MicTestCheckProps) {
  const { isTesting, inputLevel, hasPermission, error, status, startTest, stopTest } = useMicTest();

  const handleStartTest = async () => {
    await startTest();
  };

  const handleTestComplete = (passed: boolean) => {
    if (onTestComplete) {
      onTestComplete(passed);
    }
  };

  // Notify parent when test completes
  useEffect(() => {
    if (status === "passing" || status === "failed") {
      handleTestComplete(status === "passing");
    }
  }, [status]);

  return (
    <div className="w-full space-y-3">
      {/* Test Button / Status */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Mic className="w-5 h-5 text-white/70" />
          <span className="text-white/90 font-medium">Microphone Test</span>
        </div>

        <AnimatePresence mode="wait">
          {status === "pending" && (
            <motion.div
              key="pending"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
            >
              <Button
                onClick={handleStartTest}
                size="sm"
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
              >
                Test Mic
              </Button>
            </motion.div>
          )}

          {status === "testing" && (
            <motion.div
              key="testing"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-2"
            >
              <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
              <span className="text-white/70 text-sm">Testing...</span>
              <Button
                onClick={stopTest}
                variant="ghost"
                size="sm"
                className="text-white/60 hover:text-white/80"
              >
                Cancel
              </Button>
            </motion.div>
          )}

          {status === "passing" && (
            <motion.div
              key="passing"
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-2 text-emerald-400"
            >
              <CheckCircle2 className="w-5 h-5" />
              <span className="text-sm font-medium">Passed</span>
            </motion.div>
          )}

          {status === "failed" && (
            <motion.div
              key="failed"
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-2 text-red-400"
            >
              <XCircle className="w-5 h-5" />
              <span className="text-sm font-medium">Failed</span>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Input Level Meter (shown during testing) */}
      {isTesting && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: "auto" }}
          exit={{ opacity: 0, height: 0 }}
          className="space-y-2"
        >
          <div className="h-2 bg-white/10 rounded-full overflow-hidden">
            <motion.div
              className="h-full bg-emerald-500 rounded-full"
              initial={{ width: 0 }}
              animate={{ width: `${Math.min(inputLevel * 100, 100)}%` }}
              transition={{ duration: 0.1 }}
            />
          </div>
          <p className="text-xs text-white/50 text-center">
            Speak into your microphone...
          </p>
        </motion.div>
      )}

      {/* Error Message */}
      {error && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-3 bg-red-500/10 border border-red-500/30 rounded-lg space-y-2"
        >
          <div className="flex items-start gap-2">
            <AlertCircle className="w-4 h-4 text-red-400 mt-0.5 flex-shrink-0" />
            <div className="flex-1 space-y-2">
              <p className="text-sm text-red-300">{error}</p>
              
              {hasPermission === false && (
                <div className="text-xs text-red-200/80 space-y-1">
                  <p className="font-medium">How to enable microphone:</p>
                  <ol className="list-decimal list-inside space-y-0.5 ml-2">
                    <li>Click the lock/info icon in your browser's address bar</li>
                    <li>Find "Microphone" in the permissions list</li>
                    <li>Change it to "Allow"</li>
                    <li>Refresh this page</li>
                  </ol>
                </div>
              )}

              {onRequestAccommodation && (
                <Button
                  onClick={onRequestAccommodation}
                  variant="outline"
                  size="sm"
                  className="w-full mt-2 border-red-500/50 text-red-300 hover:bg-red-500/20"
                >
                  Request Accommodation
                </Button>
              )}
            </div>
          </div>
        </motion.div>
      )}

      {/* Success Message */}
      {status === "passing" && !isTesting && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-3 bg-emerald-500/10 border border-emerald-500/30 rounded-lg"
        >
          <p className="text-sm text-emerald-300">
            ✓ Microphone is working correctly
          </p>
        </motion.div>
      )}

      {/* Failed Message (non-permission errors) */}
      {status === "failed" && !error && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-3 bg-yellow-500/10 border border-yellow-500/30 rounded-lg space-y-2"
        >
          <p className="text-sm text-yellow-300">
            Microphone input level too low. Please check:
          </p>
          <ul className="text-xs text-yellow-200/80 list-disc list-inside space-y-0.5 ml-2">
            <li>Your microphone is connected and not muted</li>
            <li>You're speaking clearly into the microphone</li>
            <li>Microphone volume is turned up</li>
          </ul>
          {onRequestAccommodation && (
            <Button
              onClick={onRequestAccommodation}
              variant="outline"
              size="sm"
              className="w-full mt-2 border-yellow-500/50 text-yellow-300 hover:bg-yellow-500/20"
            >
              Request Accommodation
            </Button>
          )}
        </motion.div>
      )}
    </div>
  );
}
