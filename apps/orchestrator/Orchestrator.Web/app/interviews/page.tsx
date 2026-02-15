"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Header } from "@/components/header";
import { AgentAvatar } from "@/components/agent-avatar";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  ArrowLeft,
  Loader2,
  Video,
  Clock,
  CheckCircle,
  XCircle,
  PlayCircle,
  Calendar,
  Mic,
  MessageSquare,
} from "lucide-react";
import Link from "next/link";
import { fetchInterviews, InterviewItem } from "./actions";
import { fetchInterviewConfigurations, InterviewConfigurationItem } from "../interview-configurations/actions";

const statusConfig: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
  scheduled: { 
    label: "Scheduled", 
    color: "bg-blue-100 text-blue-800", 
    icon: <Calendar className="h-3 w-3" /> 
  },
  in_progress: { 
    label: "In Progress", 
    color: "bg-yellow-100 text-yellow-800", 
    icon: <PlayCircle className="h-3 w-3" /> 
  },
  completed: { 
    label: "Completed", 
    color: "bg-green-100 text-green-800", 
    icon: <CheckCircle className="h-3 w-3" /> 
  },
  cancelled: { 
    label: "Cancelled", 
    color: "bg-red-100 text-red-800", 
    icon: <XCircle className="h-3 w-3" /> 
  },
};

export default function InterviewsPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  
  const [interviews, setInterviews] = useState<InterviewItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("all");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(20);
  const [error, setError] = useState<string | null>(null);
  
  // State for test interview dialog
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [configurations, setConfigurations] = useState<InterviewConfigurationItem[]>([]);
  const [isLoadingConfigs, setIsLoadingConfigs] = useState(false);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user) {
      loadInterviews();
    }
  }, [user, currentPage, statusFilter]);

  const loadInterviews = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchInterviews(currentPage, pageSize, statusFilter);
      setInterviews(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      // Re-throw Next.js redirect errors (e.g. 401 → login) so navigation works
      if (err instanceof Error && err.message.includes('NEXT_REDIRECT')) {
        throw err;
      }
      console.error("Error loading interviews:", err);
      setError("Failed to load interviews. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const loadConfigurations = async () => {
    try {
      setIsLoadingConfigs(true);
      const data = await fetchInterviewConfigurations(1, 50, undefined, undefined, true);
      setConfigurations(data.items);
    } catch (err) {
      console.error("Error loading configurations:", err);
    } finally {
      setIsLoadingConfigs(false);
    }
  };

  const handleOpenDialog = () => {
    setIsDialogOpen(true);
    loadConfigurations();
  };

  const handleSelectConfiguration = (configId: string) => {
    setIsDialogOpen(false);
    router.push(`/interviews/test?configId=${configId}`);
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  const formatDate = (dateString?: string) => {
    if (!dateString) return "—";
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "short",
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
    return `${minutes} min`;
  };

  if (isUserLoading) {
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

      <main className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <Link href="/">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
          
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold">Interviews</h1>
              <p className="text-muted-foreground mt-2">
                View and manage AI interview sessions
              </p>
            </div>
            
            <div className="flex flex-col md:flex-row gap-4">
              <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
                <DialogTrigger asChild>
                  <Button onClick={handleOpenDialog} className="gap-2">
                    <Mic className="h-4 w-4" />
                    Run Test Interview
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-2xl max-h-[80vh] overflow-hidden flex flex-col">
                  <DialogHeader>
                    <DialogTitle>Select Interview Configuration</DialogTitle>
                    <DialogDescription>
                      Choose an interview configuration to start a voice interview experience.
                    </DialogDescription>
                  </DialogHeader>
                  <div className="flex-1 overflow-y-auto py-4">
                    {isLoadingConfigs ? (
                      <div className="flex justify-center py-8">
                        <Loader2 className="h-8 w-8 animate-spin text-primary" />
                      </div>
                    ) : configurations.length === 0 ? (
                      <div className="text-center py-8">
                        <MessageSquare className="mx-auto h-10 w-10 text-muted-foreground mb-3" />
                        <p className="text-muted-foreground mb-4">No active configurations found</p>
                        <Link href="/interview-configurations/new">
                          <Button variant="outline" size="sm">
                            Create Configuration
                          </Button>
                        </Link>
                      </div>
                    ) : (
                      <div className="space-y-3">
                        {configurations.map((config) => (
                          <Card
                            key={config.id}
                            className="cursor-pointer hover:border-primary hover:shadow-md transition-all"
                            onClick={() => handleSelectConfiguration(config.id)}
                          >
                            <CardContent className="p-4">
                              <div className="flex items-center gap-4">
                                {config.agent && (
                                  <AgentAvatar
                                    imageUrl={config.agent.profileImageUrl}
                                    displayName={config.agent.displayName}
                                    size="md"
                                  />
                                )}
                                <div className="flex-1 min-w-0">
                                  <h4 className="font-semibold truncate">{config.name}</h4>
                                  {config.agent && (
                                    <p className="text-sm text-muted-foreground">
                                      {config.agent.displayName}
                                    </p>
                                  )}
                                  {config.description && (
                                    <p className="text-sm text-muted-foreground line-clamp-1 mt-1">
                                      {config.description}
                                    </p>
                                  )}
                                </div>
                                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                  <MessageSquare className="h-4 w-4" />
                                  <span>{config.questionCount || config.questions?.length || 0}</span>
                                </div>
                              </div>
                            </CardContent>
                          </Card>
                        ))}
                      </div>
                    )}
                  </div>
                </DialogContent>
              </Dialog>
              
              <div className="flex gap-2 flex-wrap">
                {["all", "scheduled", "in_progress", "completed", "cancelled"].map((status) => (
                  <Button
                    key={status}
                    variant={statusFilter === status ? "default" : "outline"}
                    size="sm"
                    onClick={() => {
                      setStatusFilter(status);
                      setCurrentPage(1);
                    }}
                  >
                    {status === "all" ? "All" : statusConfig[status]?.label || status}
                  </Button>
                ))}
              </div>
            </div>
          </div>
        </div>

        {error && (
          <div className="mb-6 p-4 bg-destructive/10 text-destructive rounded-lg">
            {error}
          </div>
        )}

        {isLoading ? (
          <div className="flex justify-center items-center py-20">
            <Loader2 className="h-12 w-12 animate-spin text-primary" />
          </div>
        ) : interviews.length === 0 ? (
          <div className="text-center py-20">
            <Video className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-xl font-semibold mb-2">No interviews found</h3>
            <p className="text-muted-foreground mb-6">
              {statusFilter !== "all"
                ? "Try selecting a different status filter"
                : "Run a test interview to try the voice experience, or wait for applicants to complete their interviews."}
            </p>
            {statusFilter === "all" && (
              <Button onClick={handleOpenDialog} className="gap-2">
                <Mic className="h-4 w-4" />
                Run Test Interview
              </Button>
            )}
          </div>
        ) : (
          <>
            <div className="space-y-4 mb-8">
              {interviews.map((interview) => (
                <Link key={interview.id} href={`/interviews/${interview.id}`}>
                  <Card className="overflow-hidden hover:shadow-md transition-shadow cursor-pointer">
                    <CardContent className="p-6">
                      <div className="flex items-center gap-6">
                        {/* Agent Avatar */}
                        {interview.agent && (
                          <AgentAvatar
                            imageUrl={interview.agent.profileImageUrl}
                            displayName={interview.agent.displayName}
                            size="lg"
                          />
                        )}

                        {/* Interview Details */}
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-3 mb-2">
                            <h3 className="font-semibold text-lg truncate">
                              {interview.applicant?.firstName} {interview.applicant?.lastName}
                            </h3>
                            <Badge className={statusConfig[interview.status]?.color || "bg-gray-100"}>
                              {statusConfig[interview.status]?.icon}
                              <span className="ml-1">
                                {statusConfig[interview.status]?.label || interview.status}
                              </span>
                            </Badge>
                          </div>
                          
                          <div className="text-sm text-muted-foreground">
                            <span className="font-medium">
                              {interview.job?.title || "Position"}
                            </span>
                            {interview.agent && (
                              <span className="ml-2">
                                • Interviewed by {interview.agent.displayName}
                              </span>
                            )}
                          </div>
                        </div>

                        {/* Stats */}
                        <div className="flex items-center gap-6 text-sm text-muted-foreground">
                          <div className="text-center">
                            <div className="font-medium text-foreground">
                              {interview.responses?.length || 0}
                            </div>
                            <div>Responses</div>
                          </div>
                          
                          <div className="text-center">
                            <div className="font-medium text-foreground">
                              {getDuration(interview.startedAt, interview.completedAt)}
                            </div>
                            <div>Duration</div>
                          </div>

                          {interview.result?.overallScore && (
                            <div className="text-center">
                              <div className="font-medium text-foreground">
                                {interview.result.overallScore}/10
                              </div>
                              <div>Score</div>
                            </div>
                          )}
                          
                          <div className="text-right min-w-32">
                            <div className="text-xs">
                              {formatDate(interview.completedAt || interview.startedAt || interview.scheduledAt)}
                            </div>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </Link>
              ))}
            </div>

            {totalPages > 1 && (
              <div className="flex justify-center items-center gap-2">
                <Button
                  variant="outline"
                  onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
                  disabled={currentPage === 1}
                >
                  Previous
                </Button>
                
                <div className="flex items-center gap-2 px-4">
                  <span className="text-sm text-muted-foreground">
                    Page {currentPage} of {totalPages}
                  </span>
                  <span className="text-sm text-muted-foreground">
                    ({totalCount} total)
                  </span>
                </div>
                
                <Button
                  variant="outline"
                  onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
                  disabled={currentPage === totalPages}
                >
                  Next
                </Button>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}
