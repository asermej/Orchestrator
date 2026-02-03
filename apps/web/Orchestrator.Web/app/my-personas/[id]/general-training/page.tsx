"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Save, BookOpen } from "lucide-react";
import Link from "next/link";
import { fetchAgentById } from "../../actions";
import { AgentAvatar } from "@/components/agent-avatar";
import { 
  fetchAgentTraining, 
  updateAgentTraining 
} from "../../../agents/[id]/train/actions";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { VoiceInputToggle } from "@/components/voice-input-toggle";
import { useServerAction } from "@/lib/use-server-action";

const MAX_GENERAL_TRAINING_LENGTH = 5000;

export default function GeneralTraining() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const agentId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");

  // Training state
  const [trainingContent, setTrainingContent] = useState("");
  const [isLoadingTraining, setIsLoadingTraining] = useState(true);

  // Server action for saving training
  const { execute: executeSave, isLoading: isSavingTraining } = useServerAction(
    () => updateAgentTraining(agentId, trainingContent),
    {
      successMessage: "Training data saved successfully!",
    }
  );

  // Voice input
  const {
    isRecording,
    isSupported,
    error: voiceError,
    toggleRecording,
  } = useVoiceInput({
    onTranscript: (text) => {
      setTrainingContent((prev) => {
        // Add space if there's existing content
        const separator = prev.trim() ? " " : "";
        return prev + separator + text;
      });
    },
    onError: (error) => {
      console.error("Voice input error:", error);
    },
  });

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && agentId) {
      loadAgent();
    }
  }, [user, agentId]);

  const loadAgent = async () => {
    try {
      setIsLoading(true);
      setIsLoadingTraining(true);
      setError(null);
      
      // Load agent details and training data in parallel
      const [agent, trainingData] = await Promise.all([
        fetchAgentById(agentId),
        fetchAgentTraining(agentId)
      ]);
      
      setDisplayName(agent.displayName);
      setProfileImageUrl(agent.profileImageUrl || "");
      setTrainingContent(trainingData.trainingContent || "");
    } catch (err) {
      console.error("Error loading agent:", err);
      setError("Failed to load agent. Please try again.");
    } finally {
      setIsLoading(false);
      setIsLoadingTraining(false);
    }
  };

  const handleTrainingSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeSave();
  };

  const handleClearTraining = () => {
    if (confirm("Are you sure you want to clear all training data? This cannot be undone.")) {
      setTrainingContent("");
    }
  };

  // Character count calculations
  const characterCount = trainingContent.length;
  const isNearLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH * 0.9;
  const isOverLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH;

  if (isUserLoading || isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      {/* Hero Header with Agent Info */}
      <div className="border-b bg-gradient-to-r from-green-50 via-emerald-50 to-green-50 dark:from-green-950/20 dark:via-emerald-950/20 dark:to-green-950/20">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center gap-6">
            <Link href="/my-personas">
              <Button variant="ghost" size="icon" className="rounded-full">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div className="flex items-center gap-4">
              <AgentAvatar
                imageUrl={profileImageUrl}
                displayName={displayName}
                size="xl"
                shape="square"
              />
              <div>
                <h1 className="text-3xl font-bold">{displayName || "AI Interviewer"}</h1>
                <p className="text-muted-foreground mt-1">Configure System Prompt</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Error Message */}
          {error && (
            <div className="p-4 bg-destructive/10 text-destructive rounded-lg border border-destructive/20">
              {error}
            </div>
          )}

          {/* General Training Card */}
          <Card className="shadow-lg">
            <CardHeader className="bg-gradient-to-r from-green-100 via-emerald-100 to-green-100 dark:from-green-900/20 dark:via-emerald-900/20 dark:to-green-900/20 border-b">
              <div className="flex items-start gap-3">
                <div className="p-2 rounded-lg bg-green-200 dark:bg-green-800/30">
                  <BookOpen className="h-6 w-6 text-green-700 dark:text-green-400" />
                </div>
                <div className="flex-1">
                  <CardTitle className="text-2xl">System Prompt</CardTitle>
                  <CardDescription className="mt-2 text-base">
                    Define how {displayName || "this AI interviewer"} should behave during interviews. 
                    Include personality, tone, company context, and interview guidelines.
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6">
              {isLoadingTraining ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin" />
                </div>
              ) : (
                <form onSubmit={handleTrainingSubmit} className="space-y-6">
                  {/* Training Content */}
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <Label htmlFor="trainingContent">
                        System Prompt
                      </Label>
                      <VoiceInputToggle
                        isRecording={isRecording}
                        isSupported={isSupported}
                        error={voiceError}
                        onToggle={toggleRecording}
                        disabled={isSavingTraining}
                      />
                    </div>
                    <Textarea
                      id="trainingContent"
                      name="trainingContent"
                      placeholder="Example: You are a friendly and professional AI recruiting assistant for a healthcare company. Your role is to conduct initial screening interviews for nursing positions. Be warm and encouraging while gathering information about candidates' experience, certifications, and availability. Ask follow-up questions when answers are vague. Keep the conversation focused but natural..."
                      value={trainingContent}
                      onChange={(e) => setTrainingContent(e.target.value)}
                      disabled={isSavingTraining}
                      rows={16}
                      className="resize-none font-mono text-sm"
                    />
                    <div className="flex items-center justify-between text-sm">
                      <p className="text-muted-foreground">
                        Maximum {MAX_GENERAL_TRAINING_LENGTH.toLocaleString()} characters (~1,250 tokens)
                      </p>
                      <p className={`font-medium ${
                        isOverLimit 
                          ? "text-destructive" 
                          : isNearLimit 
                            ? "text-yellow-600 dark:text-yellow-400" 
                            : "text-muted-foreground"
                      }`}>
                        {characterCount.toLocaleString()} / {MAX_GENERAL_TRAINING_LENGTH.toLocaleString()}
                        {isOverLimit && " (over limit)"}
                      </p>
                    </div>
                  </div>

                  {/* Training Tips */}
                  <div className="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-950/20 dark:to-emerald-950/20 rounded-lg p-5 border border-green-200 dark:border-green-800">
                    <h3 className="font-semibold text-sm mb-3 flex items-center gap-2">
                      <BookOpen className="h-4 w-4" />
                      💡 Training Tips for AI Interviewers
                    </h3>
                    <ul className="text-sm text-muted-foreground space-y-2 list-disc list-inside">
                      <li>Define the interviewer&apos;s personality and tone (professional, friendly, formal)</li>
                      <li>Specify the company/industry context they represent</li>
                      <li>Include guidance on how to handle unclear or incomplete answers</li>
                      <li>Add instructions for maintaining focus on relevant topics</li>
                      <li>Describe how to transition between interview sections</li>
                    </ul>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex flex-col sm:flex-row justify-between gap-4">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={handleClearTraining}
                      disabled={isSavingTraining || !trainingContent}
                    >
                      Clear All
                    </Button>
                    
                    <Button 
                      type="submit" 
                      className="flex-1 sm:w-auto" 
                      disabled={isSavingTraining || isOverLimit}
                      size="lg"
                    >
                      {isSavingTraining ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Saving...
                        </>
                      ) : (
                        <>
                          <Save className="mr-2 h-4 w-4" />
                          Save Training
                        </>
                      )}
                    </Button>
                  </div>
                </form>
              )}
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
