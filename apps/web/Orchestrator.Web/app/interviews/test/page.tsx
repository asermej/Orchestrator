"use client";

import { useEffect, useState, useCallback } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Play, CheckCircle, XCircle, AlertCircle, Volume2 } from "lucide-react";
import Link from "next/link";
import { 
  createTestInterview, 
  scoreTestInterview, 
  saveTestInterviewResponse,
  warmupTestInterviewAudio,
  TestInterview,
  TestInterviewResult 
} from "./actions";
import { 
  fetchInterviewConfigurationById, 
  InterviewConfigurationItem 
} from "../../interview-configurations/actions";
import { AgentAvatar } from "@/components/agent-avatar";
import { InterviewExperience, InterviewQuestion } from "@/components/interview-experience";

type PageState = "loading" | "ready" | "interview" | "completed" | "scoring" | "results";

export default function TestInterviewPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const searchParams = useSearchParams();
  const configId = searchParams.get("configId");

  const [pageState, setPageState] = useState<PageState>("loading");
  const [configuration, setConfiguration] = useState<InterviewConfigurationItem | null>(null);
  const [interview, setInterview] = useState<TestInterview | null>(null);
  const [result, setResult] = useState<TestInterviewResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && configId) {
      loadConfiguration();
    }
  }, [user, configId]);

  const loadConfiguration = async () => {
    try {
      setPageState("loading");
      const data = await fetchInterviewConfigurationById(configId!);
      setConfiguration(data);
      setPageState("ready");
    } catch (err) {
      console.error("Error loading configuration:", err);
      setError("Failed to load interview configuration");
    }
  };

  const startTestInterview = async () => {
    if (!configuration) return;
    
    try {
      setError(null);
      const testInterview = await createTestInterview(configuration.id, user?.name || "Test User");
      setInterview(testInterview);
      
      // Warmup audio cache for all questions (non-blocking)
      if (testInterview?.id) {
        warmupTestInterviewAudio(testInterview.id).then(() => {
          console.log("Audio warmup complete");
        }).catch(err => {
          console.warn("Audio warmup failed (non-blocking):", err);
        });
      }
      
      setPageState("interview");
    } catch (err) {
      console.error("Error starting test interview:", err);
      setError("Failed to start test interview");
      setPageState("ready");
    }
  };

  const handleSaveResponse = useCallback(async (
    questionId: string,
    questionText: string,
    transcript: string,
    order: number
  ) => {
    if (!interview) return;
    
    try {
      await saveTestInterviewResponse(
        interview.id,
        questionId,
        questionText,
        transcript,
        order
      );
    } catch (err) {
      // Log error but don't fail - the interview can continue
      console.error("Failed to save response:", err);
    }
  }, [interview]);

  const handleComplete = useCallback(async () => {
    setPageState("completed");
  }, []);

  const handleExit = useCallback(() => {
    if (pageState === "interview") {
      setPageState("completed");
    } else {
      setPageState("ready");
    }
  }, [pageState]);

  const handleCompleteInterview = async () => {
    if (!interview || !configuration) return;
    
    try {
      setPageState("scoring");
      const interviewResult = await scoreTestInterview(interview.id, configuration.id);
      setResult(interviewResult);
      setPageState("results");
    } catch (err) {
      console.error("Error scoring interview:", err);
      setError("Failed to score interview");
      setPageState("completed");
    }
  };

  const getScoreColor = (score: number | null | undefined) => {
    if (score === null || score === undefined) return "text-muted-foreground";
    if (score >= 70) return "text-green-600";
    if (score >= 50) return "text-yellow-600";
    return "text-red-600";
  };

  const getRecommendationBadge = (recommendation: string | null | undefined) => {
    if (!recommendation) return null;
    
    const lower = recommendation.toLowerCase();
    if (lower.includes("strong")) {
      return <Badge className="bg-green-600">Strong Recommend</Badge>;
    } else if (lower.includes("recommend") && !lower.includes("not")) {
      return <Badge className="bg-blue-600">Recommend</Badge>;
    } else if (lower.includes("consider")) {
      return <Badge className="bg-yellow-600">Consider</Badge>;
    } else {
      return <Badge variant="destructive">Not Recommended</Badge>;
    }
  };

  if (isUserLoading || pageState === "loading") {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) {
    return null;
  }

  if (!configId) {
    return (
      <div className="min-h-screen bg-background">
        <Header user={user} />
        <main className="container mx-auto px-4 py-8">
          <div className="text-center py-20">
            <AlertCircle className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-xl font-semibold mb-2">No Configuration Selected</h3>
            <p className="text-muted-foreground mb-6">
              Please select an interview configuration to run a test interview.
            </p>
            <Link href="/interview-configurations">
              <Button>Browse Configurations</Button>
            </Link>
          </div>
        </main>
      </div>
    );
  }

  // Full-screen interview experience
  if (pageState === "interview" && configuration) {
    const questions: InterviewQuestion[] = configuration.questions.map((q) => ({
      id: q.id,
      text: q.question,
    }));

    return (
      <InterviewExperience
        questions={questions}
        agentId={configuration.agent?.id}
        agentName={configuration.agent?.displayName || "AI Interviewer"}
        agentImageUrl={configuration.agent?.profileImageUrl}
        onSaveResponse={handleSaveResponse}
        onComplete={handleComplete}
        onExit={handleExit}
      />
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <Link href={`/interview-configurations/${configId}`}>
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Configuration
            </Button>
          </Link>
          
          <h1 className="text-3xl font-bold">Test Interview</h1>
          <p className="text-muted-foreground mt-2">
            Run a test interview to preview the experience and scoring
          </p>
        </div>

        {error && (
          <div className="mb-6 p-4 bg-destructive/10 text-destructive rounded-lg">
            {error}
          </div>
        )}

        <div className="max-w-3xl mx-auto space-y-6">
          {configuration && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-4">
                  {configuration.agent && (
                    <AgentAvatar
                      imageUrl={configuration.agent.profileImageUrl}
                      displayName={configuration.agent.displayName}
                      size="lg"
                    />
                  )}
                  <div>
                    <CardTitle>{configuration.name}</CardTitle>
                    {configuration.agent && (
                      <CardDescription>
                        Interviewer: {configuration.agent.displayName}
                      </CardDescription>
                    )}
                    <div className="flex items-center gap-2 mt-2">
                      <Badge variant="outline">
                        {configuration.questions.length} questions
                      </Badge>
                      <Badge variant={configuration.isActive ? "default" : "secondary"}>
                        {configuration.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </div>
                  </div>
                </div>
              </CardHeader>
            </Card>
          )}

          {pageState === "ready" && (
            <Card>
              <CardContent className="pt-6">
                <div className="text-center py-8">
                  <Play className="mx-auto h-16 w-16 text-primary mb-4" />
                  <h3 className="text-xl font-semibold mb-2">Ready to Start</h3>
                  <p className="text-muted-foreground mb-6">
                    Click the button below to begin the test interview.
                    You'll be asked {configuration?.questions.length || 0} questions.
                  </p>
                  <Button size="lg" onClick={startTestInterview}>
                    <Play className="mr-2 h-5 w-5" />
                    Start Test Interview
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}

          {pageState === "completed" && (
            <Card>
              <CardContent className="pt-6">
                <div className="text-center py-8">
                  <CheckCircle className="mx-auto h-16 w-16 text-green-600 mb-4" />
                  <h3 className="text-xl font-semibold mb-2">Interview Complete!</h3>
                  <p className="text-muted-foreground mb-6">
                    All questions have been answered. Click below to score the interview.
                  </p>
                  <Button size="lg" onClick={handleCompleteInterview}>
                    <Volume2 className="mr-2 h-5 w-5" />
                    Score Interview
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}

          {pageState === "scoring" && (
            <Card>
              <CardContent className="pt-6">
                <div className="text-center py-8">
                  <Loader2 className="mx-auto h-16 w-16 animate-spin text-primary mb-4" />
                  <h3 className="text-xl font-semibold mb-2">Scoring Interview...</h3>
                  <p className="text-muted-foreground">
                    Analyzing responses and generating scores...
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          {pageState === "results" && result && (
            <>
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle>Interview Results</CardTitle>
                    {getRecommendationBadge(result.recommendation)}
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-6">
                    <div className="text-center py-4">
                      <div className={`text-6xl font-bold ${getScoreColor(result.score)}`}>
                        {result.score ?? "N/A"}
                      </div>
                      <p className="text-muted-foreground mt-2">Overall Score (out of 100)</p>
                    </div>

                    {result.summary && (
                      <div>
                        <h4 className="font-semibold mb-2">Summary</h4>
                        <p className="text-muted-foreground">{result.summary}</p>
                      </div>
                    )}

                    {result.strengths && (
                      <div>
                        <h4 className="font-semibold mb-2 flex items-center gap-2">
                          <CheckCircle className="h-4 w-4 text-green-600" />
                          Strengths
                        </h4>
                        <p className="text-muted-foreground">{result.strengths}</p>
                      </div>
                    )}

                    {result.areasForImprovement && (
                      <div>
                        <h4 className="font-semibold mb-2 flex items-center gap-2">
                          <XCircle className="h-4 w-4 text-red-600" />
                          Areas for Improvement
                        </h4>
                        <p className="text-muted-foreground">{result.areasForImprovement}</p>
                      </div>
                    )}

                    {result.recommendation && (
                      <div>
                        <h4 className="font-semibold mb-2">Recommendation</h4>
                        <p className="text-muted-foreground">{result.recommendation}</p>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>

              <div className="flex justify-center gap-4">
                <Button variant="outline" onClick={() => {
                  setPageState("ready");
                  setInterview(null);
                  setResult(null);
                }}>
                  Run Another Test
                </Button>
                <Link href={`/interview-configurations/${configId}`}>
                  <Button>Back to Configuration</Button>
                </Link>
              </div>
            </>
          )}
        </div>
      </main>
    </div>
  );
}
