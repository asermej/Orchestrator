"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Loader2, Play, Save, Trash2, BookOpen, MessageSquare, ExternalLink } from "lucide-react";
import Link from "next/link";
import {
  fetchInterviewConfigurationById,
  updateInterviewConfiguration,
  deleteInterviewConfiguration,
  fetchAgentsForConfiguration,
  fetchGuidesForConfiguration,
  AgentItem,
  InterviewGuideItem,
  InterviewConfigurationItem,
} from "../actions";
import { useServerAction } from "@/lib/use-server-action";
import { AgentAvatar } from "@/components/agent-avatar";

export default function EditInterviewConfigurationPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const configId = params.id as string;

  const [configuration, setConfiguration] = useState<InterviewConfigurationItem | null>(null);
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [guides, setGuides] = useState<InterviewGuideItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingAgents, setIsLoadingAgents] = useState(true);
  const [isLoadingGuides, setIsLoadingGuides] = useState(true);

  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [interviewGuideId, setInterviewGuideId] = useState("");
  const [agentId, setAgentId] = useState("");
  const [isActive, setIsActive] = useState(true);

  const { execute: executeUpdate, isLoading: isUpdating } = useServerAction(
    async () => {
      await updateInterviewConfiguration(configId, {
        name,
        description: description || null,
        interviewGuideId,
        agentId,
        isActive,
        updatedBy: user?.sub || undefined,
      });
    },
    {
      successMessage: "Interview configuration updated successfully!",
    }
  );

  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    async () => {
      await deleteInterviewConfiguration(configId);
    },
    {
      successMessage: "Interview configuration deleted successfully!",
      onSuccess: () => router.push("/interview-configurations"),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && configId) {
      loadConfiguration();
      loadAgents();
      loadGuides();
    }
  }, [user, configId]);

  const loadConfiguration = async () => {
    try {
      setIsLoading(true);
      const data = await fetchInterviewConfigurationById(configId);
      setConfiguration(data);

      // Populate form
      setName(data.name);
      setDescription(data.description || "");
      setInterviewGuideId(data.interviewGuideId);
      setAgentId(data.agentId);
      setIsActive(data.isActive);
    } catch (err) {
      console.error("Error loading configuration:", err);
      router.push("/interview-configurations");
    } finally {
      setIsLoading(false);
    }
  };

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
    await executeUpdate();
  };

  const handleDelete = async () => {
    if (confirm("Are you sure you want to delete this configuration? This action cannot be undone.")) {
      await executeDelete();
    }
  };

  const selectedAgent = agents.find((a) => a.id === agentId);
  const selectedGuide = guides.find((g) => g.id === interviewGuideId);

  if (isUserLoading || isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user || !configuration) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-3">
                <h1 className="text-3xl font-bold">{configuration.name}</h1>
                <Badge variant={configuration.isActive ? "default" : "secondary"}>
                  {configuration.isActive ? "Active" : "Inactive"}
                </Badge>
              </div>
              <p className="text-muted-foreground mt-2">
                Edit interview configuration
              </p>
            </div>
            <div className="flex gap-2">
              <Link href={`/interviews/test?configId=${configId}`}>
                <Button variant="outline" className="gap-2">
                  <Play className="h-4 w-4" />
                  Test Interview
                </Button>
              </Link>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="max-w-3xl space-y-8">
          {/* Basic Information */}
          <Card>
            <CardHeader>
              <CardTitle>Basic Information</CardTitle>
              <CardDescription>
                Update the name and description for this configuration
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
                  disabled={isUpdating}
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
                  disabled={isUpdating}
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
                  disabled={isUpdating}
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
                ) : (
                  <Select value={interviewGuideId} onValueChange={setInterviewGuideId} disabled={isUpdating}>
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
                        Edit Guide
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
                  <Select value={agentId} onValueChange={setAgentId} disabled={isUpdating}>
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

          {/* Actions */}
          <div className="flex justify-between">
            <Button
              type="button"
              variant="destructive"
              onClick={handleDelete}
              disabled={isUpdating || isDeleting}
            >
              {isDeleting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                <>
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete Configuration
                </>
              )}
            </Button>

            <div className="flex gap-4">
              <Link href="/interview-configurations">
                <Button type="button" variant="outline" disabled={isUpdating}>
                  Cancel
                </Button>
              </Link>
              <Button type="submit" disabled={isUpdating || !name || !agentId || !interviewGuideId}>
                {isUpdating ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="mr-2 h-4 w-4" />
                    Save Changes
                  </>
                )}
              </Button>
            </div>
          </div>
        </form>
    </div>
  );
}
