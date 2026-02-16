"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Loader2, BookOpen, MessageSquare, ExternalLink } from "lucide-react";
import Link from "next/link";
import { createInterviewConfiguration, fetchAgentsForConfiguration, fetchGuidesForConfiguration, AgentItem, InterviewGuideItem } from "../actions";
import { useServerAction } from "@/lib/use-server-action";
import { AgentAvatar } from "@/components/agent-avatar";

export default function NewInterviewConfigurationPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [guides, setGuides] = useState<InterviewGuideItem[]>([]);
  const [isLoadingAgents, setIsLoadingAgents] = useState(true);
  const [isLoadingGuides, setIsLoadingGuides] = useState(true);

  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [interviewGuideId, setInterviewGuideId] = useState("");
  const [agentId, setAgentId] = useState("");
  const [isActive, setIsActive] = useState(true);

  const selectedAgent = agents.find((a) => a.id === agentId);
  const selectedGuide = guides.find((g) => g.id === interviewGuideId);

  const { execute: executeCreate, isLoading: isCreating } = useServerAction(
    async () => {
      if (!agentId) {
        throw new Error("Please select an agent");
      }
      if (!interviewGuideId) {
        throw new Error("Please select an interview guide");
      }
      await createInterviewConfiguration({
        interviewGuideId,
        agentId,
        name,
        description: description || null,
        isActive,
        createdBy: user?.sub || undefined,
      });
    },
    {
      successMessage: "Interview configuration created successfully!",
      onSuccess: () => router.push("/interview-configurations"),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user) {
      loadAgents();
      loadGuides();
    }
  }, [user]);

  const loadAgents = async () => {
    try {
      setIsLoadingAgents(true);
      const data = await fetchAgentsForConfiguration();
      setAgents(data);
    } catch (err) {
      console.error("Error loading agents:", err);
    } finally {
      setIsLoadingAgents(false);
    }
  };

  const loadGuides = async () => {
    try {
      setIsLoadingGuides(true);
      const data = await fetchGuidesForConfiguration();
      setGuides(data);
    } catch (err) {
      console.error("Error loading interview guides:", err);
    } finally {
      setIsLoadingGuides(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeCreate();
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
    <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold">New Interview Configuration</h1>
          <p className="text-muted-foreground mt-2">
            Combine an interview guide with an AI agent to create a ready-to-use interview configuration
          </p>
        </div>

        <form onSubmit={handleSubmit} className="max-w-3xl space-y-8">
          {/* Basic Information */}
          <Card>
            <CardHeader>
              <CardTitle>Basic Information</CardTitle>
              <CardDescription>
                Set up the name and description for this configuration
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Configuration Name *</Label>
                <Input
                  id="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., Caregiver Interview"
                  required
                  disabled={isCreating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Describe what this interview configuration is for..."
                  rows={4}
                  disabled={isCreating}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="isActive">Active</Label>
                  <p className="text-sm text-muted-foreground">
                    Enable this configuration for use in interviews
                  </p>
                </div>
                <Switch
                  id="isActive"
                  checked={isActive}
                  onCheckedChange={setIsActive}
                  disabled={isCreating}
                />
              </div>
            </CardContent>
          </Card>

          {/* Interview Guide */}
          <Card>
            <CardHeader>
              <CardTitle>Interview Guide</CardTitle>
              <CardDescription>
                Select the interview guide that defines the questions, scoring rubric, and opening/closing templates
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="guide">Interview Guide *</Label>
                {isLoadingGuides ? (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Loading guides...
                  </div>
                ) : guides.length === 0 ? (
                  <div className="p-4 border border-dashed rounded-lg text-center">
                    <BookOpen className="mx-auto h-8 w-8 text-muted-foreground mb-2" />
                    <p className="text-sm text-muted-foreground mb-2">No interview guides available</p>
                    <Link href="/interview-guides/new">
                      <Button type="button" variant="outline" size="sm">
                        Create Interview Guide
                      </Button>
                    </Link>
                  </div>
                ) : (
                  <Select value={interviewGuideId} onValueChange={setInterviewGuideId} disabled={isCreating}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select an interview guide" />
                    </SelectTrigger>
                    <SelectContent>
                      {guides.map((guide) => (
                        <SelectItem key={guide.id} value={guide.id}>
                          <div className="flex items-center gap-2">
                            <BookOpen className="h-4 w-4 text-muted-foreground" />
                            {guide.name}
                            <span className="text-muted-foreground">({guide.questionCount} questions)</span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>

              {selectedGuide && (
                <div className="p-4 bg-muted/50 rounded-lg space-y-3">
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2">
                      <BookOpen className="h-5 w-5 text-primary" />
                      <span className="font-medium">{selectedGuide.name}</span>
                    </div>
                    <Link href={`/interview-guides/${selectedGuide.id}`}>
                      <Button type="button" variant="ghost" size="sm" className="gap-1 text-xs">
                        <ExternalLink className="h-3 w-3" />
                        View Guide
                      </Button>
                    </Link>
                  </div>
                  {selectedGuide.description && (
                    <p className="text-sm text-muted-foreground">{selectedGuide.description}</p>
                  )}
                  <div className="flex items-center gap-4 text-sm text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <MessageSquare className="h-4 w-4" />
                      <span>{selectedGuide.questionCount} questions</span>
                    </div>
                    {selectedGuide.openingTemplate && (
                      <Badge variant="outline" className="text-xs">Has Opening</Badge>
                    )}
                    {selectedGuide.closingTemplate && (
                      <Badge variant="outline" className="text-xs">Has Closing</Badge>
                    )}
                    {selectedGuide.scoringRubric && (
                      <Badge variant="outline" className="text-xs">Has Rubric</Badge>
                    )}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* AI Agent */}
          <Card>
            <CardHeader>
              <CardTitle>AI Agent</CardTitle>
              <CardDescription>
                Select the AI agent that will conduct interviews using this configuration
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="agent">Agent *</Label>
                {isLoadingAgents ? (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Loading agents...
                  </div>
                ) : (
                  <Select value={agentId} onValueChange={setAgentId} disabled={isCreating}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select an AI agent" />
                    </SelectTrigger>
                    <SelectContent>
                      {agents.map((agent) => (
                        <SelectItem key={agent.id} value={agent.id}>
                          <div className="flex items-center gap-2">
                            <AgentAvatar
                              imageUrl={agent.profileImageUrl}
                              displayName={agent.displayName}
                              size="sm"
                            />
                            {agent.displayName}
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>

              {selectedAgent && (
                <div className="flex items-center gap-3 p-3 bg-muted/50 rounded-lg">
                  <AgentAvatar
                    imageUrl={selectedAgent.profileImageUrl}
                    displayName={selectedAgent.displayName}
                    size="lg"
                  />
                  <span className="font-medium">{selectedAgent.displayName}</span>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Submit */}
          <div className="flex justify-end gap-4">
            <Link href="/interview-configurations">
              <Button type="button" variant="outline" disabled={isCreating}>
                Cancel
              </Button>
            </Link>
            <Button type="submit" disabled={isCreating || !name || !agentId || !interviewGuideId}>
              {isCreating ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                "Create Configuration"
              )}
            </Button>
          </div>
        </form>
    </div>
  );
}
