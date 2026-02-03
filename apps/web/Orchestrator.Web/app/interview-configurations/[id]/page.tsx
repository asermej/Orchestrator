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
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Plus, Trash2, GripVertical, Play, Save } from "lucide-react";
import Link from "next/link";
import { 
  fetchInterviewConfigurationById, 
  updateInterviewConfiguration, 
  deleteInterviewConfiguration,
  fetchAgentsForConfiguration, 
  AgentItem,
  InterviewConfigurationItem 
} from "../actions";
import { useServerAction } from "@/lib/use-server-action";
import { AgentAvatar } from "@/components/agent-avatar";

interface QuestionInput {
  id: string;
  question: string;
  scoringWeight: number;
  scoringGuidance: string;
}

export default function EditInterviewConfigurationPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const configId = params.id as string;
  
  const [configuration, setConfiguration] = useState<InterviewConfigurationItem | null>(null);
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingAgents, setIsLoadingAgents] = useState(true);
  
  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [agentId, setAgentId] = useState("");
  const [scoringRubric, setScoringRubric] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [questions, setQuestions] = useState<QuestionInput[]>([]);

  const { execute: executeUpdate, isLoading: isUpdating } = useServerAction(
    async () => {
      await updateInterviewConfiguration(configId, {
        name,
        description: description || null,
        scoringRubric: scoringRubric || null,
        isActive,
        questions: questions
          .filter(q => q.question.trim() !== "")
          .map((q, index) => ({
            question: q.question,
            displayOrder: index,
            scoringWeight: q.scoringWeight,
            scoringGuidance: q.scoringGuidance || null
          })),
        updatedBy: user?.sub || undefined
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
      setAgentId(data.agentId);
      setScoringRubric(data.scoringRubric || "");
      setIsActive(data.isActive);
      setQuestions(
        data.questions?.length > 0
          ? data.questions.map(q => ({
              id: q.id,
              question: q.question,
              scoringWeight: q.scoringWeight,
              scoringGuidance: q.scoringGuidance || ""
            }))
          : [{ id: crypto.randomUUID(), question: "", scoringWeight: 1, scoringGuidance: "" }]
      );
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

  const addQuestion = () => {
    setQuestions([
      ...questions,
      { id: crypto.randomUUID(), question: "", scoringWeight: 1, scoringGuidance: "" }
    ]);
  };

  const removeQuestion = (id: string) => {
    if (questions.length > 1) {
      setQuestions(questions.filter(q => q.id !== id));
    }
  };

  const updateQuestion = (id: string, field: keyof QuestionInput, value: string | number) => {
    setQuestions(questions.map(q => 
      q.id === id ? { ...q, [field]: value } : q
    ));
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

  const selectedAgent = agents.find(a => a.id === agentId);

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
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <Link href="/interview-configurations">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Configurations
            </Button>
          </Link>
          
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-3">
                <h1 className="text-3xl font-bold">{configuration.name}</h1>
                <Badge variant={configuration.isActive ? "default" : "secondary"}>
                  {configuration.isActive ? "Active" : "Inactive"}
                </Badge>
              </div>
              <p className="text-muted-foreground mt-2">
                Edit interview configuration and questions
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

        <form onSubmit={handleSubmit} className="max-w-4xl space-y-8">
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
                  rows={12}
                  disabled={isUpdating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="agent">AI Agent</Label>
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
                {selectedAgent && (
                  <div className="flex items-center gap-2 mt-2 p-2 bg-muted rounded-lg">
                    <AgentAvatar
                      imageUrl={selectedAgent.profileImageUrl}
                      displayName={selectedAgent.displayName}
                      size="md"
                    />
                    <span className="font-medium">{selectedAgent.displayName}</span>
                  </div>
                )}
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

          {/* Questions */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Interview Questions</CardTitle>
                  <CardDescription>
                    Manage questions that will be asked during the interview
                  </CardDescription>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  onClick={addQuestion}
                  disabled={isUpdating}
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Question
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {questions.map((q, index) => (
                <div key={q.id} className="border rounded-lg p-4 space-y-4">
                  <div className="flex items-start gap-4">
                    <div className="flex items-center gap-2 text-muted-foreground pt-2">
                      <GripVertical className="h-4 w-4" />
                      <span className="font-medium">{index + 1}</span>
                    </div>
                    <div className="flex-1 space-y-4">
                      <div className="space-y-2">
                        <Label>Question *</Label>
                        <Textarea
                          value={q.question}
                          onChange={(e) => updateQuestion(q.id, "question", e.target.value)}
                          placeholder="Enter your interview question..."
                          rows={2}
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label>Scoring Weight</Label>
                          <Input
                            type="number"
                            min="0"
                            step="0.1"
                            value={q.scoringWeight}
                            onChange={(e) => updateQuestion(q.id, "scoringWeight", parseFloat(e.target.value) || 1)}
                            disabled={isUpdating}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Scoring Guidance</Label>
                          <Input
                            value={q.scoringGuidance}
                            onChange={(e) => updateQuestion(q.id, "scoringGuidance", e.target.value)}
                            placeholder="How to score this question..."
                            disabled={isUpdating}
                          />
                        </div>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeQuestion(q.id)}
                      disabled={questions.length === 1 || isUpdating}
                      className="text-destructive hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Scoring Rubric */}
          <Card>
            <CardHeader>
              <CardTitle>Scoring Rubric</CardTitle>
              <CardDescription>
                Define how interview responses should be evaluated
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Textarea
                value={scoringRubric}
                onChange={(e) => setScoringRubric(e.target.value)}
                placeholder="Describe the scoring criteria and guidelines for evaluating interview responses..."
                rows={6}
                disabled={isUpdating}
              />
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
              <Button type="submit" disabled={isUpdating || !name}>
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
      </main>
    </div>
  );
}
