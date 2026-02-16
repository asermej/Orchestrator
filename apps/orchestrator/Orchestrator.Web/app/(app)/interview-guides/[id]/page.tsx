"use client";

import { useEffect, useState, use } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Loader2, Plus, Trash2, GripVertical, Info } from "lucide-react";
import Link from "next/link";
import { fetchInterviewGuideById, updateInterviewGuide, deleteInterviewGuide, InterviewGuideItem } from "../actions";
import { useServerAction } from "@/lib/use-server-action";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

interface QuestionInput {
  id: string;
  question: string;
  scoringWeight: number;
  scoringGuidance: string;
  followUpsEnabled: boolean;
  maxFollowUps: number;
}

export default function EditInterviewGuidePage({ params }: { params: Promise<{ id: string }> }) {
  const { id: guideId } = use(params);
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [guide, setGuide] = useState<InterviewGuideItem | null>(null);
  const [isLoadingGuide, setIsLoadingGuide] = useState(true);

  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [openingTemplate, setOpeningTemplate] = useState("");
  const [closingTemplate, setClosingTemplate] = useState("");
  const [scoringRubric, setScoringRubric] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [questions, setQuestions] = useState<QuestionInput[]>([]);

  const { execute: executeUpdate, isLoading: isUpdating } = useServerAction(
    async () => {
      const updatedGuide = await updateInterviewGuide(guideId, {
        name,
        description: description || null,
        openingTemplate: openingTemplate || null,
        closingTemplate: closingTemplate || null,
        scoringRubric: scoringRubric || null,
        isActive,
        questions: questions
          .filter(q => q.question.trim() !== "")
          .map((q, index) => ({
            question: q.question,
            displayOrder: index,
            scoringWeight: q.scoringWeight,
            scoringGuidance: q.scoringGuidance || null,
            followUpsEnabled: q.followUpsEnabled,
            maxFollowUps: q.maxFollowUps
          })),
        updatedBy: user?.sub || undefined
      });
      return updatedGuide;
    },
    {
      successMessage: "Interview guide updated successfully!",
      onSuccess: () => router.push("/interview-guides"),
    }
  );

  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    async () => {
      await deleteInterviewGuide(guideId);
    },
    {
      successMessage: "Interview guide deleted successfully!",
      onSuccess: () => router.push("/interview-guides"),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && guideId) {
      loadGuide();
    }
  }, [user, guideId]);

  const loadGuide = async () => {
    try {
      setIsLoadingGuide(true);
      const data = await fetchInterviewGuideById(guideId);
      setGuide(data);
      setName(data.name);
      setDescription(data.description || "");
      setOpeningTemplate(data.openingTemplate || "");
      setClosingTemplate(data.closingTemplate || "");
      setScoringRubric(data.scoringRubric || "");
      setIsActive(data.isActive);
      setQuestions(
        data.questions && data.questions.length > 0
          ? data.questions.map(q => ({
              id: q.id,
              question: q.question,
              scoringWeight: q.scoringWeight,
              scoringGuidance: q.scoringGuidance || "",
              followUpsEnabled: q.followUpsEnabled,
              maxFollowUps: q.maxFollowUps
            }))
          : [{ id: crypto.randomUUID(), question: "", scoringWeight: 1, scoringGuidance: "", followUpsEnabled: true, maxFollowUps: 2 }]
      );
    } catch (err) {
      console.error("Error loading interview guide:", err);
    } finally {
      setIsLoadingGuide(false);
    }
  };

  const addQuestion = () => {
    setQuestions([
      ...questions,
      { id: crypto.randomUUID(), question: "", scoringWeight: 1, scoringGuidance: "", followUpsEnabled: true, maxFollowUps: 2 }
    ]);
  };

  const removeQuestion = (id: string) => {
    if (questions.length > 1) {
      setQuestions(questions.filter(q => q.id !== id));
    }
  };

  const updateQuestion = (id: string, field: keyof QuestionInput, value: string | number | boolean) => {
    setQuestions(questions.map(q =>
      q.id === id ? { ...q, [field]: value } : q
    ));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeUpdate();
  };

  const handleDelete = async () => {
    if (confirm("Are you sure you want to delete this interview guide? This action cannot be undone.")) {
      await executeDelete();
    }
  };

  if (isUserLoading || isLoadingGuide) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user || !guide) {
    return null;
  }

  const templateVariableHint = "Available variables: {{applicantName}}, {{agentName}}, {{jobTitle}}";

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <div className="flex items-center gap-4">
            <h1 className="text-3xl font-bold">Edit Interview Guide</h1>
            <Badge variant={isActive ? "default" : "secondary"}>
              {isActive ? "Active" : "Inactive"}
            </Badge>
          </div>
          <p className="text-muted-foreground mt-2">
            Update this interview guide&apos;s questions, templates, and scoring criteria
          </p>
        </div>

        <form onSubmit={handleSubmit} className="max-w-4xl space-y-8">
          {/* Basic Information */}
          <Card>
            <CardHeader>
              <CardTitle>Basic Information</CardTitle>
              <CardDescription>
                Update the name and description for this guide
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Guide Name *</Label>
                <Input
                  id="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., Caregiver Interview Guide"
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
                  placeholder="Describe what this interview guide is for..."
                  rows={3}
                  disabled={isUpdating}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="isActive">Active</Label>
                  <p className="text-sm text-muted-foreground">
                    Enable this guide for use in interview configurations
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

          {/* Opening & Closing Templates */}
          <Card>
            <CardHeader>
              <CardTitle>Opening & Closing Templates</CardTitle>
              <CardDescription>
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <span className="inline-flex items-center gap-1 cursor-help">
                        Customize the opening greeting and closing message
                        <Info className="h-4 w-4" />
                      </span>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>{templateVariableHint}</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="openingTemplate">Opening Template</Label>
                <Textarea
                  id="openingTemplate"
                  value={openingTemplate}
                  onChange={(e) => setOpeningTemplate(e.target.value)}
                  placeholder={`e.g., Hello {{applicantName}}! I'm {{agentName}}, and I'll be conducting your interview for the {{jobTitle}} position today.`}
                  rows={3}
                  disabled={isUpdating}
                />
                <p className="text-xs text-muted-foreground">{templateVariableHint}</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="closingTemplate">Closing Template</Label>
                <Textarea
                  id="closingTemplate"
                  value={closingTemplate}
                  onChange={(e) => setClosingTemplate(e.target.value)}
                  placeholder={`e.g., Thank you for completing this interview, {{applicantName}}! We appreciate your time and will be in touch soon.`}
                  rows={3}
                  disabled={isUpdating}
                />
                <p className="text-xs text-muted-foreground">{templateVariableHint}</p>
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
                    Manage the questions that will be asked during the interview
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
                      <div className="flex items-center gap-4">
                        <div className="flex items-center gap-2">
                          <Switch
                            checked={q.followUpsEnabled}
                            onCheckedChange={(val) => updateQuestion(q.id, "followUpsEnabled", val)}
                            disabled={isUpdating}
                          />
                          <Label className="text-sm">Follow-ups enabled</Label>
                        </div>
                        {q.followUpsEnabled && (
                          <div className="flex items-center gap-2">
                            <Label className="text-sm">Max follow-ups:</Label>
                            <Input
                              type="number"
                              min="1"
                              max="5"
                              value={q.maxFollowUps}
                              onChange={(e) => updateQuestion(q.id, "maxFollowUps", parseInt(e.target.value) || 2)}
                              className="w-16"
                              disabled={isUpdating}
                            />
                          </div>
                        )}
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
                "Delete Guide"
              )}
            </Button>
            <div className="flex gap-4">
              <Link href="/interview-guides">
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
                  "Save Changes"
                )}
              </Button>
            </div>
          </div>
        </form>
    </div>
  );
}
