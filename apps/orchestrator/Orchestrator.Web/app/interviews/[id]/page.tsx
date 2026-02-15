"use client";

import { useEffect, useState, use, useRef } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Header } from "@/components/header";
import { AgentAvatar } from "@/components/agent-avatar";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
  ArrowLeft,
  Loader2,
  Clock,
  CheckCircle,
  XCircle,
  PlayCircle,
  Calendar,
  User,
  Mail,
  Phone,
  Star,
  MessageSquare,
  ThumbsUp,
  ThumbsDown,
  Trash2,
  Mic,
  ChevronDown,
  ChevronUp,
  Award,
  TrendingUp,
  FileText,
  CornerDownRight,
  Play,
  Pause,
} from "lucide-react";
import Link from "next/link";
import { getInterview, deleteInterview, scoreInterview, InterviewItem, InterviewResponse, QuestionScore } from "../actions";

const statusConfig: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
  scheduled: { label: "Scheduled", color: "bg-blue-100 text-blue-800", icon: <Calendar className="h-3 w-3" /> },
  pending: { label: "Pending", color: "bg-slate-100 text-slate-800", icon: <Clock className="h-3 w-3" /> },
  in_progress: { label: "In Progress", color: "bg-yellow-100 text-yellow-800", icon: <PlayCircle className="h-3 w-3" /> },
  completed: { label: "Completed", color: "bg-green-100 text-green-800", icon: <CheckCircle className="h-3 w-3" /> },
  cancelled: { label: "Cancelled", color: "bg-red-100 text-red-800", icon: <XCircle className="h-3 w-3" /> },
};

function getRecommendationBadge(recommendation?: string) {
  if (!recommendation) return null;
  const lower = recommendation.toLowerCase();
  if (lower === "hire" || lower === "strong_hire" || lower.includes("excellent")) {
    return { label: "Excellent Fit", className: "bg-emerald-100 text-emerald-800 border-emerald-200" };
  }
  if (lower === "lean_hire" || lower.includes("good")) {
    return { label: "Good Fit", className: "bg-blue-100 text-blue-800 border-blue-200" };
  }
  if (lower === "no_hire" || lower.includes("not")) {
    return { label: "Not Recommended", className: "bg-red-100 text-red-800 border-red-200" };
  }
  return { label: "Further Review", className: "bg-amber-100 text-amber-800 border-amber-200" };
}

function getScoreColor(score: number): string {
  if (score >= 80) return "text-emerald-600";
  if (score >= 60) return "text-blue-600";
  if (score >= 40) return "text-amber-600";
  return "text-red-600";
}

function getScoreGradientPosition(score: number): number {
  return Math.min(100, Math.max(0, score));
}

/** Returns color classes for a per-question score on a 0-10 scale */
function getQuestionScoreBadge(score: number, maxScore: number): { text: string; bg: string } {
  const pct = maxScore > 0 ? (score / maxScore) * 100 : 0;
  if (pct >= 80) return { text: "text-emerald-700", bg: "bg-emerald-50 border-emerald-200" };
  if (pct >= 60) return { text: "text-blue-700", bg: "bg-blue-50 border-blue-200" };
  if (pct >= 40) return { text: "text-amber-700", bg: "bg-amber-50 border-amber-200" };
  return { text: "text-red-700", bg: "bg-red-50 border-red-200" };
}

function CollapsibleText({ text, maxLines = 3 }: { text: string; maxLines?: number }) {
  const [expanded, setExpanded] = useState(false);
  const [clamped, setClamped] = useState(false);
  const ref = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    if (ref.current) {
      setClamped(ref.current.scrollHeight > ref.current.clientHeight);
    }
  }, [text]);

  return (
    <div>
      <p
        ref={ref}
        className={`text-sm whitespace-pre-wrap ${expanded ? "" : `line-clamp-${maxLines}`}`}
        style={!expanded ? { WebkitLineClamp: maxLines, display: "-webkit-box", WebkitBoxOrient: "vertical", overflow: "hidden" } : undefined}
      >
        {text}
      </p>
      {clamped && (
        <button
          onClick={() => setExpanded((prev) => !prev)}
          className="mt-1 text-xs font-medium text-primary hover:text-primary/80 flex items-center gap-1"
        >
          {expanded ? (
            <><ChevronUp className="h-3 w-3" /> Show less</>
          ) : (
            <><ChevronDown className="h-3 w-3" /> Show more</>
          )}
        </button>
      )}
    </div>
  );
}

/**
 * Maps a stored audio URL (e.g. /api/v1/interview-audio/{key})
 * to the Next.js proxy route (e.g. /api/interview-audio/{key})
 * so the browser can fetch audio through an authenticated proxy.
 */
function proxyAudioUrl(url: string): string {
  if (url.startsWith("/api/v1/interview-audio/")) {
    return url.replace("/api/v1/interview-audio/", "/api/interview-audio/");
  }
  return url;
}

function AudioPlayer({ src }: { src: string }) {
  const audioRef = useRef<HTMLAudioElement>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);

  const togglePlay = () => {
    if (!audioRef.current) return;
    if (isPlaying) {
      audioRef.current.pause();
    } else {
      audioRef.current.play();
    }
    setIsPlaying(!isPlaying);
  };

  const formatTime = (seconds: number) => {
    if (!isFinite(seconds) || isNaN(seconds)) return "—";
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, "0")}`;
  };

  // Valid, finite duration for progress calculations
  const validDuration = isFinite(duration) && duration > 0 ? duration : 0;

  return (
    <div className="flex items-center gap-3 bg-slate-50 rounded-lg px-3 py-2 mt-2">
      <audio
        ref={audioRef}
        src={proxyAudioUrl(src)}
        preload="auto"
        onTimeUpdate={(e) => {
          setCurrentTime(e.currentTarget.currentTime);
          // WebM blobs often don't report duration in metadata — pick it up during playback
          const d = e.currentTarget.duration;
          if (isFinite(d) && d > 0) setDuration(d);
        }}
        onLoadedMetadata={(e) => {
          const d = e.currentTarget.duration;
          if (isFinite(d) && d > 0) setDuration(d);
        }}
        onEnded={() => setIsPlaying(false)}
      />
      <button
        onClick={togglePlay}
        className="flex-shrink-0 w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center hover:bg-primary/90 transition-colors"
      >
        {isPlaying ? <Pause className="h-3.5 w-3.5" /> : <Play className="h-3.5 w-3.5 ml-0.5" />}
      </button>
      <div className="flex-1 min-w-0">
        <div className="w-full bg-slate-200 rounded-full h-1.5 cursor-pointer" onClick={(e) => {
          if (!audioRef.current || !validDuration) return;
          const rect = e.currentTarget.getBoundingClientRect();
          const pos = (e.clientX - rect.left) / rect.width;
          audioRef.current.currentTime = pos * validDuration;
        }}>
          <div
            className="bg-primary h-1.5 rounded-full transition-all"
            style={{ width: `${validDuration ? (currentTime / validDuration) * 100 : 0}%` }}
          />
        </div>
      </div>
      <span className="text-xs text-muted-foreground flex-shrink-0 tabular-nums">
        {formatTime(currentTime)}{validDuration ? ` / ${formatTime(validDuration)}` : ""}
      </span>
    </div>
  );
}

interface InterviewDetailPageProps {
  params: Promise<{ id: string }>;
}

export default function InterviewDetailPage({ params }: InterviewDetailPageProps) {
  const { id } = use(params);
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  
  const [interview, setInterview] = useState<InterviewItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isScoring, setIsScoring] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && id) {
      loadInterview();
    }
  }, [user, id]);

  const loadInterview = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await getInterview(id);
      setInterview(data);
    } catch (err) {
      // Re-throw Next.js redirect errors (e.g. 401 → login) so navigation works
      if (err instanceof Error && err.message.includes('NEXT_REDIRECT')) {
        throw err;
      }
      console.error("Error loading interview:", err);
      setError("Failed to load interview details.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async () => {
    try {
      setIsDeleting(true);
      await deleteInterview(id);
      router.push("/interviews");
    } catch (err) {
      console.error("Error deleting interview:", err);
      setError("Failed to delete interview.");
      setIsDeleting(false);
    }
  };

  const handleScore = async () => {
    if (!interview?.interviewConfigurationId) return;
    try {
      setIsScoring(true);
      setError(null);
      await scoreInterview(id, interview.interviewConfigurationId);
      // Reload interview to pick up the new result + scores
      const data = await getInterview(id);
      setInterview(data);
    } catch (err) {
      console.error("Error scoring interview:", err);
      setError("Failed to score interview.");
    } finally {
      setIsScoring(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return "—";
    return new Date(dateString).toLocaleDateString("en-US", {
      weekday: "short",
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  };

  const getDuration = (startedAt?: string, completedAt?: string) => {
    if (!startedAt || !completedAt) return null;
    const start = new Date(startedAt);
    const end = new Date(completedAt);
    const totalSeconds = Math.round((end.getTime() - start.getTime()) / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    if (minutes === 0) return `${seconds}s`;
    return `${minutes}m ${seconds}s`;
  };

  // Group responses: main questions + their follow-ups
  const groupResponses = (responses: InterviewResponse[]) => {
    const sorted = [...responses].sort((a, b) => a.responseOrder - b.responseOrder);
    const groups: { main: InterviewResponse; followUps: InterviewResponse[] }[] = [];
    let currentGroup: { main: InterviewResponse; followUps: InterviewResponse[] } | null = null;

    for (const resp of sorted) {
      if (!resp.isFollowUp) {
        if (currentGroup) groups.push(currentGroup);
        currentGroup = { main: resp, followUps: [] };
      } else if (currentGroup) {
        currentGroup.followUps.push(resp);
      } else {
        // Orphaned follow-up, treat as main
        currentGroup = { main: resp, followUps: [] };
      }
    }
    if (currentGroup) groups.push(currentGroup);
    return groups;
  };

  if (isUserLoading || isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) return null;

  if (error || !interview) {
    return (
      <div className="min-h-screen bg-background">
        <Header user={user} />
        <main className="container mx-auto px-4 py-8">
          <Link href="/interviews">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Interviews
            </Button>
          </Link>
          <div className="text-center py-20">
            <p className="text-destructive">{error || "Interview not found"}</p>
          </div>
        </main>
      </div>
    );
  }

  const duration = getDuration(interview.startedAt, interview.completedAt);
  const recBadge = getRecommendationBadge(interview.result?.recommendation);
  const responseGroups = interview.responses ? groupResponses(interview.responses) : [];
  const mainQuestionCount = responseGroups.length;

  // Build a lookup of per-question scores by question index
  const questionScoresByIndex = new Map<number, QuestionScore>();
  if (interview.result?.questionScores) {
    for (const qs of interview.result.questionScores) {
      questionScoresByIndex.set(qs.questionIndex, qs);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8 max-w-4xl">
        <Link href="/interviews">
          <Button variant="ghost" className="mb-4">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Interviews
          </Button>
        </Link>

        {/* 1. Header Bar */}
        <div className="flex items-start justify-between mb-6">
          <div className="flex items-center gap-4">
            <div>
              <div className="flex items-center gap-3 mb-1">
                <h1 className="text-2xl font-bold">
                  {interview.applicant?.firstName} {interview.applicant?.lastName}
                </h1>
                <Badge className={statusConfig[interview.status]?.color || "bg-gray-100"}>
                  {statusConfig[interview.status]?.icon}
                  <span className="ml-1">{statusConfig[interview.status]?.label || interview.status}</span>
                </Badge>
              </div>
              <p className="text-muted-foreground">
                {interview.job?.title || "Position"} Interview
              </p>
            </div>
          </div>
          
          <div className="flex items-center gap-2">
            {/* Re-score: only show for completed interviews that already have a result */}
            {interview.status === "completed" && interview.interviewConfigurationId && interview.result && (
              <Button
                variant="ghost"
                size="sm"
                disabled={isScoring}
                onClick={handleScore}
              >
                {isScoring ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Star className="h-4 w-4 mr-2" />}
                Re-score
              </Button>
            )}

          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="outline" size="sm" disabled={isDeleting} className="text-destructive hover:text-destructive">
                {isDeleting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Trash2 className="h-4 w-4 mr-2" />}
                Delete
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete Interview?</AlertDialogTitle>
                <AlertDialogDescription>
                  This will permanently delete this interview and all associated data.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                  Delete
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
          </div>
        </div>

        {/* 2. Interview Meta Card */}
        <Card className="mb-6">
          <CardContent className="py-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-blue-50 rounded-lg">
                  <Mic className="h-4 w-4 text-blue-600" />
                </div>
                <div>
                  <div className="text-xs text-muted-foreground">Type</div>
                  <div className="text-sm font-medium capitalize">{interview.interviewType || "Voice"}</div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <div className="p-2 bg-purple-50 rounded-lg">
                  <Calendar className="h-4 w-4 text-purple-600" />
                </div>
                <div>
                  <div className="text-xs text-muted-foreground">Date</div>
                  <div className="text-sm font-medium">{formatDate(interview.startedAt || interview.scheduledAt)}</div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <div className="p-2 bg-green-50 rounded-lg">
                  <Clock className="h-4 w-4 text-green-600" />
                </div>
                <div>
                  <div className="text-xs text-muted-foreground">Duration</div>
                  <div className="text-sm font-medium">{duration || "—"}</div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                {interview.agent && (
                  <AgentAvatar
                    imageUrl={interview.agent.profileImageUrl}
                    displayName={interview.agent.displayName}
                    size="sm"
                  />
                )}
                <div>
                  <div className="text-xs text-muted-foreground">Interviewer</div>
                  <div className="text-sm font-medium">{interview.agent?.displayName || "AI Interviewer"}</div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Applicant Contact Info */}
        {interview.applicant && (interview.applicant.email || interview.applicant.phone) && (
          <Card className="mb-6">
            <CardContent className="py-3">
              <div className="flex items-center gap-6 text-sm">
                <div className="flex items-center gap-2">
                  <User className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">
                    {interview.applicant.firstName} {interview.applicant.lastName}
                  </span>
                </div>
                {interview.applicant.email && (
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Mail className="h-3.5 w-3.5" />
                    <span>{interview.applicant.email}</span>
                  </div>
                )}
                {interview.applicant.phone && (
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Phone className="h-3.5 w-3.5" />
                    <span>{interview.applicant.phone}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        )}

        {/* 3. Summary + Score Row (only if result exists) */}
        {interview.result && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            {/* Summary Card */}
            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base flex items-center gap-2">
                    <Award className="h-4 w-4" />
                    Summary
                  </CardTitle>
                  {recBadge && (
                    <Badge variant="outline" className={recBadge.className}>
                      {recBadge.label}
                    </Badge>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                {interview.result.summary && (
                  <CollapsibleText text={interview.result.summary} maxLines={4} />
                )}
                {interview.result.strengths && (
                  <div>
                    <div className="flex items-center gap-1.5 mb-1">
                      <ThumbsUp className="h-3.5 w-3.5 text-emerald-600" />
                      <span className="text-xs font-medium text-emerald-700">Strengths</span>
                    </div>
                    <p className="text-sm text-muted-foreground">{interview.result.strengths}</p>
                  </div>
                )}
                {interview.result.areasForImprovement && (
                  <div>
                    <div className="flex items-center gap-1.5 mb-1">
                      <TrendingUp className="h-3.5 w-3.5 text-amber-600" />
                      <span className="text-xs font-medium text-amber-700">Areas for Improvement</span>
                    </div>
                    <p className="text-sm text-muted-foreground">{interview.result.areasForImprovement}</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Score Card */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base flex items-center gap-2">
                  <Star className="h-4 w-4" />
                  Overall Score
                </CardTitle>
              </CardHeader>
              <CardContent>
                {interview.result.score != null ? (
                  <div className="space-y-4">
                    <div className="text-center py-2">
                      <span className={`text-5xl font-bold ${getScoreColor(interview.result.score)}`}>
                        {interview.result.score}
                      </span>
                      <span className="text-2xl text-muted-foreground">/100</span>
                    </div>
                    <div className="relative">
                      <div className="h-3 rounded-full bg-gradient-to-r from-red-400 via-amber-400 via-blue-400 to-emerald-400" />
                      <div
                        className="absolute top-0 w-1 h-3 bg-slate-900 rounded-full"
                        style={{ left: `${getScoreGradientPosition(interview.result.score)}%`, transform: "translateX(-50%)" }}
                      />
                      <div className="flex justify-between mt-1">
                        <span className="text-[10px] text-muted-foreground">Poor</span>
                        <span className="text-[10px] text-muted-foreground">Average</span>
                        <span className="text-[10px] text-muted-foreground">Good</span>
                        <span className="text-[10px] text-muted-foreground">Excellent</span>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Star className="h-8 w-8 mx-auto mb-2 opacity-30" />
                    <p className="text-sm">Score not available</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        )}

        {/* 4. Interview Responses Section */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-4">
            <MessageSquare className="h-5 w-5" />
            <h2 className="text-lg font-semibold">Interview Responses</h2>
            <span className="text-sm text-muted-foreground">
              ({mainQuestionCount} {mainQuestionCount === 1 ? "question" : "questions"})
            </span>
          </div>

          {responseGroups.length === 0 ? (
            <Card>
              <CardContent className="py-12">
                <div className="text-center text-muted-foreground">
                  <FileText className="h-12 w-12 mx-auto mb-4 opacity-30" />
                  <p className="text-sm">No responses recorded yet</p>
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-4">
              {responseGroups.map((group, groupIndex) => {
                const qScore = questionScoresByIndex.get(group.main.responseOrder);
                const scoreBadge = qScore ? getQuestionScoreBadge(qScore.score, qScore.maxScore) : null;

                return (
                <Card key={group.main.id}>
                  <CardContent className="py-5 px-6">
                    {/* Question + Score */}
                    <div className="flex items-start gap-3 mb-3">
                      <div className="flex-shrink-0 w-7 h-7 rounded-full bg-primary/10 flex items-center justify-center">
                        <span className="text-xs font-bold text-primary">{groupIndex + 1}</span>
                      </div>
                      <div className="flex-1 flex items-start justify-between gap-2">
                        <p className="font-medium text-sm pt-0.5">{group.main.questionText}</p>
                        {qScore && scoreBadge && (
                          <div className={`flex-shrink-0 px-2.5 py-1 rounded-md border text-xs font-semibold ${scoreBadge.bg} ${scoreBadge.text}`}>
                            {Number(qScore.score).toFixed(1)}/{Number(qScore.maxScore).toFixed(0)}
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Candidate Response */}
                    <div className="ml-10">
                      <div className="bg-muted/50 rounded-lg p-4">
                        <p className="text-sm whitespace-pre-wrap">{group.main.transcript || "No transcript available"}</p>
                        {group.main.audioUrl && (
                          <AudioPlayer src={group.main.audioUrl} />
                        )}
                        {group.main.durationSeconds != null && (
                          <div className="mt-2 text-xs text-muted-foreground">
                            Duration: {Math.floor(group.main.durationSeconds / 60)}m {group.main.durationSeconds % 60}s
                          </div>
                        )}
                      </div>

                      {/* Per-question AI Analysis */}
                      {group.main.aiAnalysis && (
                        <div className="mt-3 p-3 bg-blue-50 rounded-lg border border-blue-100">
                          <div className="text-xs font-medium text-blue-700 mb-1">AI Analysis</div>
                          <p className="text-sm text-blue-900">{group.main.aiAnalysis}</p>
                        </div>
                      )}

                      {/* Per-question Score Feedback */}
                      {qScore && qScore.feedback && !group.main.aiAnalysis && (
                        <div className="mt-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
                          <div className="text-xs font-medium text-slate-600 mb-1">Score Feedback</div>
                          <p className="text-sm text-slate-700">{qScore.feedback}</p>
                        </div>
                      )}
                    </div>

                    {/* Follow-up Questions */}
                    {group.followUps.map((followUp, fuIndex) => (
                      <div key={followUp.id} className="mt-4 ml-10">
                        <div className="flex items-start gap-2 mb-2">
                          <CornerDownRight className="h-4 w-4 text-muted-foreground flex-shrink-0 mt-0.5" />
                          <div>
                            <div className="text-xs text-muted-foreground mb-0.5">Follow-up {fuIndex + 1}</div>
                            <p className="font-medium text-sm">{followUp.questionText}</p>
                          </div>
                        </div>
                        <div className="ml-6">
                          <div className="bg-muted/50 rounded-lg p-4">
                            <p className="text-sm whitespace-pre-wrap">{followUp.transcript || "No transcript available"}</p>
                            {followUp.audioUrl && (
                              <AudioPlayer src={followUp.audioUrl} />
                            )}
                          </div>
                          {followUp.aiAnalysis && (
                            <div className="mt-2 p-3 bg-blue-50 rounded-lg border border-blue-100">
                              <div className="text-xs font-medium text-blue-700 mb-1">AI Analysis</div>
                              <p className="text-sm text-blue-900">{followUp.aiAnalysis}</p>
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </CardContent>
                </Card>
                );
              })}
            </div>
          )}
        </div>

        {/* 5. Overall Summary Card (bottom) */}
        {interview.result?.summary && (
          <Card className="mb-8">
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Overall Summary</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm whitespace-pre-wrap">{interview.result.summary}</p>
            </CardContent>
          </Card>
        )}
      </main>
    </div>
  );
}
