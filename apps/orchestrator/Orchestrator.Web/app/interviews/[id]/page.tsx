"use client";

import { useEffect, useState, use } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
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
  FileText,
  Trash2,
} from "lucide-react";
import Link from "next/link";
import { getInterview, deleteInterview, InterviewItem } from "../actions";

const statusConfig: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
  scheduled: { label: "Scheduled", color: "bg-blue-100 text-blue-800", icon: <Calendar className="h-3 w-3" /> },
  in_progress: { label: "In Progress", color: "bg-yellow-100 text-yellow-800", icon: <PlayCircle className="h-3 w-3" /> },
  completed: { label: "Completed", color: "bg-green-100 text-green-800", icon: <CheckCircle className="h-3 w-3" /> },
  cancelled: { label: "Cancelled", color: "bg-red-100 text-red-800", icon: <XCircle className="h-3 w-3" /> },
};

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

  const formatDate = (dateString?: string) => {
    if (!dateString) return "—";
    return new Date(dateString).toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  };

  const getDuration = (startedAt?: string, completedAt?: string) => {
    if (!startedAt || !completedAt) return "—";
    const start = new Date(startedAt);
    const end = new Date(completedAt);
    const minutes = Math.round((end.getTime() - start.getTime()) / 60000);
    return `${minutes} minutes`;
  };

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

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8 max-w-5xl">
        <Link href="/interviews">
          <Button variant="ghost" className="mb-4">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Interviews
          </Button>
        </Link>

        {/* Header */}
        <div className="flex items-start justify-between mb-8">
          <div className="flex items-center gap-6">
            {interview.agent && (
              <AgentAvatar
                imageUrl={interview.agent.profileImageUrl}
                displayName={interview.agent.displayName}
                size="xl"
              />
            )}
            <div>
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-3xl font-bold">
                  {interview.applicant?.firstName} {interview.applicant?.lastName}
                </h1>
                <Badge className={statusConfig[interview.status]?.color || "bg-gray-100"}>
                  {statusConfig[interview.status]?.icon}
                  <span className="ml-1">{statusConfig[interview.status]?.label || interview.status}</span>
                </Badge>
              </div>
              <p className="text-muted-foreground text-lg">
                {interview.job?.title || "Position"} Interview
              </p>
            </div>
          </div>
          
          {/* Delete Button */}
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive" size="sm" disabled={isDeleting}>
                {isDeleting ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <Trash2 className="h-4 w-4 mr-2" />
                )}
                Delete Interview
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete Interview?</AlertDialogTitle>
                <AlertDialogDescription>
                  Are you sure you want to delete this interview for{" "}
                  <span className="font-medium">
                    {interview.applicant?.firstName} {interview.applicant?.lastName}
                  </span>
                  ? This action cannot be undone and will permanently remove all responses and results.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  onClick={handleDelete}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                >
                  Delete
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column - Details */}
          <div className="space-y-6">
            {/* Applicant Info */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <User className="h-5 w-5" />
                  Applicant Details
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div>
                  <div className="text-sm text-muted-foreground">Name</div>
                  <div className="font-medium">
                    {interview.applicant?.firstName} {interview.applicant?.lastName}
                  </div>
                </div>
                {interview.applicant?.email && (
                  <div className="flex items-center gap-2">
                    <Mail className="h-4 w-4 text-muted-foreground" />
                    <span>{interview.applicant.email}</span>
                  </div>
                )}
                {interview.applicant?.phone && (
                  <div className="flex items-center gap-2">
                    <Phone className="h-4 w-4 text-muted-foreground" />
                    <span>{interview.applicant.phone}</span>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Interview Info */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-5 w-5" />
                  Interview Info
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div>
                  <div className="text-sm text-muted-foreground">Started</div>
                  <div>{formatDate(interview.startedAt)}</div>
                </div>
                {interview.completedAt && (
                  <div>
                    <div className="text-sm text-muted-foreground">Completed</div>
                    <div>{formatDate(interview.completedAt)}</div>
                  </div>
                )}
                <div>
                  <div className="text-sm text-muted-foreground">Duration</div>
                  <div>{getDuration(interview.startedAt, interview.completedAt)}</div>
                </div>
                <div>
                  <div className="text-sm text-muted-foreground">Interviewer</div>
                  <div>{interview.agent?.displayName || "AI Interviewer"}</div>
                </div>
              </CardContent>
            </Card>

            {/* AI Analysis */}
            {interview.result && (
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Star className="h-5 w-5" />
                    AI Analysis
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {interview.result.overallScore && (
                    <div className="text-center p-4 bg-primary/10 rounded-lg">
                      <div className="text-4xl font-bold text-primary">
                        {interview.result.overallScore}/10
                      </div>
                      <div className="text-sm text-muted-foreground">Overall Score</div>
                    </div>
                  )}
                  
                  {interview.result.recommendation && (
                    <div>
                      <div className="text-sm text-muted-foreground mb-1">Recommendation</div>
                      <Badge 
                        variant={interview.result.recommendation === "hire" ? "default" : "secondary"}
                        className="text-sm"
                      >
                        {interview.result.recommendation === "hire" ? (
                          <><ThumbsUp className="h-3 w-3 mr-1" /> Recommend Hire</>
                        ) : interview.result.recommendation === "no_hire" ? (
                          <><ThumbsDown className="h-3 w-3 mr-1" /> Do Not Recommend</>
                        ) : (
                          <>Further Review</>
                        )}
                      </Badge>
                    </div>
                  )}

                  {interview.result.strengths && (
                    <div>
                      <div className="text-sm text-muted-foreground mb-1">Strengths</div>
                      <p className="text-sm">{interview.result.strengths}</p>
                    </div>
                  )}

                  {interview.result.weaknesses && (
                    <div>
                      <div className="text-sm text-muted-foreground mb-1">Areas for Improvement</div>
                      <p className="text-sm">{interview.result.weaknesses}</p>
                    </div>
                  )}
                </CardContent>
              </Card>
            )}
          </div>

          {/* Right Column - Transcript */}
          <div className="lg:col-span-2">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Interview Transcript
                </CardTitle>
                <CardDescription>
                  {interview.responses?.length || 0} responses recorded
                </CardDescription>
              </CardHeader>
              <CardContent>
                {interview.responses?.length === 0 ? (
                  <div className="text-center py-12 text-muted-foreground">
                    <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No responses recorded yet</p>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {interview.responses
                      ?.sort((a, b) => a.responseOrder - b.responseOrder)
                      .map((response, index) => (
                        <div key={response.id} className="space-y-3">
                          <div className="flex items-start gap-3">
                            <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                              <span className="text-sm font-medium text-primary">Q</span>
                            </div>
                            <div className="flex-1">
                              <div className="text-sm text-muted-foreground mb-1">
                                Question {index + 1}
                              </div>
                              <p className="font-medium">{response.questionText}</p>
                            </div>
                          </div>
                          
                          <div className="flex items-start gap-3 ml-4">
                            <div className="w-8 h-8 rounded-full bg-secondary flex items-center justify-center flex-shrink-0">
                              <User className="h-4 w-4" />
                            </div>
                            <div className="flex-1 p-4 bg-muted/50 rounded-lg">
                              <p className="text-sm whitespace-pre-wrap">{response.transcript}</p>
                              <div className="text-xs text-muted-foreground mt-2">
                                {new Date(response.createdAt).toLocaleTimeString()}
                              </div>
                            </div>
                          </div>
                          
                          {index < (interview.responses?.length || 0) - 1 && (
                            <hr className="my-4" />
                          )}
                        </div>
                      ))}
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Summary */}
            {interview.result?.summary && (
              <Card className="mt-6">
                <CardHeader>
                  <CardTitle>Summary</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="whitespace-pre-wrap">{interview.result.summary}</p>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
