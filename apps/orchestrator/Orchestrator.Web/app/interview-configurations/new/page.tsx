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
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Plus, Trash2, GripVertical } from "lucide-react";
import Link from "next/link";
import { createInterviewConfiguration, fetchAgentsForConfiguration, AgentItem } from "../actions";
import { useServerAction } from "@/lib/use-server-action";
import { AgentAvatar } from "@/components/agent-avatar";

interface QuestionInput {
  id: string;
  question: string;
  scoringWeight: number;
  scoringGuidance: string;
}

export default function NewInterviewConfigurationPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isLoadingAgents, setIsLoadingAgents] = useState(true);
  
  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [agentId, setAgentId] = useState("");
  const [scoringRubric, setScoringRubric] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [questions, setQuestions] = useState<QuestionInput[]>([
    { id: crypto.randomUUID(), question: "", scoringWeight: 1, scoringGuidance: "" }
  ]);

  const selectedAgent = agents.find(a => a.id === agentId);

  const { execute: executeCreate, isLoading: isCreating } = useServerAction(
    async () => {
      if (!selectedAgent) {
        throw new Error("Please select an agent");
      }
      const config = await createInterviewConfiguration({
        organizationId: selectedAgent.organizationId,
        agentId,
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
        createdBy: user?.sub || undefined
      });
      return config;
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
          
          <h1 className="text-3xl font-bold">New Interview Configuration</h1>
          <p className="text-muted-foreground mt-2">
            Create a new interview configuration with questions and scoring criteria
          </p>
        </div>

        <form onSubmit={handleSubmit} className="max-w-4xl space-y-8">
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
                  rows={12}
                  disabled={isCreating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="agent">AI Agent *</Label>
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
                  disabled={isCreating}
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
                    Add questions that will be asked during the interview
                  </CardDescription>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  onClick={addQuestion}
                  disabled={isCreating}
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
                          disabled={isCreating}
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
                            disabled={isCreating}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Scoring Guidance</Label>
                          <Input
                            value={q.scoringGuidance}
                            onChange={(e) => updateQuestion(q.id, "scoringGuidance", e.target.value)}
                            placeholder="How to score this question..."
                            disabled={isCreating}
                          />
                        </div>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeQuestion(q.id)}
                      disabled={questions.length === 1 || isCreating}
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
                disabled={isCreating}
              />
            </CardContent>
          </Card>

          {/* Submit */}
          <div className="flex justify-end gap-4">
            <Link href="/interview-configurations">
              <Button type="button" variant="outline" disabled={isCreating}>
                Cancel
              </Button>
            </Link>
            <Button type="submit" disabled={isCreating || !name || !agentId}>
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
      </main>
    </div>
  );
}
