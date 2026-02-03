"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Save, Trash2, GripVertical, Plus, AlertTriangle } from "lucide-react";
import Link from "next/link";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { getJobType, updateJobType, deleteJobType, InterviewQuestion } from "../actions";

export default function JobTypeDetailPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const jobTypeId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [questions, setQuestions] = useState<InterviewQuestion[]>([]);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && jobTypeId) {
      loadJobType();
    }
  }, [user, jobTypeId]);

  const loadJobType = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await getJobType(jobTypeId);
      setName(data.name);
      setDescription(data.description || "");
      setQuestions(data.questions || []);
    } catch (err) {
      console.error("Error loading job type:", err);
      setError("Failed to load job type. It may not exist.");
    } finally {
      setIsLoading(false);
    }
  };

  const addQuestion = () => {
    setQuestions([
      ...questions,
      {
        questionText: "",
        questionOrder: questions.length,
        isRequired: true,
        maxFollowUps: 1,
      },
    ]);
  };

  const removeQuestion = (index: number) => {
    const updated = questions.filter((_, i) => i !== index);
    setQuestions(updated.map((q, i) => ({ ...q, questionOrder: i })));
  };

  const updateQuestion = (index: number, field: keyof InterviewQuestion, value: any) => {
    const updated = [...questions];
    updated[index] = { ...updated[index], [field]: value };
    setQuestions(updated);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!name.trim()) {
      setError("Job type name is required");
      return;
    }

    try {
      setIsSaving(true);
      setError(null);

      const filteredQuestions = questions
        .filter((q) => q.questionText.trim())
        .map((q, i) => ({ ...q, questionOrder: i }));

      await updateJobType(jobTypeId, {
        name: name.trim(),
        description: description.trim() || undefined,
        questions: filteredQuestions,
      });

      router.push("/job-types");
    } catch (err) {
      console.error("Error updating job type:", err);
      setError("Failed to save changes. Please try again.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    try {
      setIsDeleting(true);
      await deleteJobType(jobTypeId);
      router.push("/job-types");
    } catch (err) {
      console.error("Error deleting job type:", err);
      setError("Failed to delete job type. Please try again.");
      setShowDeleteDialog(false);
    } finally {
      setIsDeleting(false);
    }
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

  if (error && !name) {
    return (
      <div className="min-h-screen bg-background">
        <Header user={user} />
        <main className="container mx-auto px-4 py-8 max-w-3xl">
          <Link href="/job-types">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Job Types
            </Button>
          </Link>
          <Card>
            <CardContent className="pt-6">
              <div className="text-center py-12">
                <AlertTriangle className="mx-auto h-12 w-12 text-destructive mb-4" />
                <h3 className="text-xl font-semibold mb-2">Job Type Not Found</h3>
                <p className="text-muted-foreground mb-6">{error}</p>
                <Link href="/job-types">
                  <Button>Back to Job Types</Button>
                </Link>
              </div>
            </CardContent>
          </Card>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8 max-w-3xl">
        <Link href="/job-types">
          <Button variant="ghost" className="mb-4">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Job Types
          </Button>
        </Link>

        <form onSubmit={handleSubmit}>
          <Card className="mb-6">
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Edit Job Type</CardTitle>
                  <CardDescription>
                    Update the interview template for this position
                  </CardDescription>
                </div>
                <Button
                  type="button"
                  variant="destructive"
                  size="sm"
                  onClick={() => setShowDeleteDialog(true)}
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-6">
              {error && (
                <div className="p-4 bg-destructive/10 text-destructive rounded-lg">
                  {error}
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="name">Job Type Name *</Label>
                <Input
                  id="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., Retail Sales Associate"
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Brief description of this job type and its interview focus..."
                  rows={3}
                />
              </div>
            </CardContent>
          </Card>

          <Card className="mb-6">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Interview Questions</CardTitle>
                <Button type="button" variant="outline" onClick={addQuestion} className="gap-2">
                  <Plus className="h-4 w-4" />
                  Add Question
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {questions.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">
                  <p>No questions added yet. Click "Add Question" to create interview questions.</p>
                </div>
              ) : (
                questions.map((question, index) => (
                  <div key={index} className="flex gap-3 items-start p-4 border rounded-lg bg-muted/30">
                    <div className="flex items-center text-muted-foreground mt-2">
                      <GripVertical className="h-5 w-5" />
                      <span className="w-6 text-center font-medium">{index + 1}</span>
                    </div>

                    <div className="flex-1 space-y-3">
                      <Textarea
                        value={question.questionText}
                        onChange={(e) => updateQuestion(index, "questionText", e.target.value)}
                        placeholder="Enter your interview question..."
                        rows={2}
                      />

                      <div className="flex gap-4 items-center text-sm">
                        <label className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={question.isRequired}
                            onChange={(e) => updateQuestion(index, "isRequired", e.target.checked)}
                            className="rounded"
                          />
                          Required
                        </label>

                        <label className="flex items-center gap-2">
                          Max follow-ups:
                          <Input
                            type="number"
                            min={0}
                            max={5}
                            value={question.maxFollowUps}
                            onChange={(e) => updateQuestion(index, "maxFollowUps", parseInt(e.target.value) || 0)}
                            className="w-16 h-8"
                          />
                        </label>
                      </div>
                    </div>

                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeQuestion(index)}
                      className="text-destructive hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))
              )}
            </CardContent>
          </Card>

          <div className="flex justify-end gap-3">
            <Link href="/job-types">
              <Button type="button" variant="outline">Cancel</Button>
            </Link>
            <Button type="submit" disabled={isSaving}>
              {isSaving ? (
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
        </form>
      </main>

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Job Type?</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{name}"? This action cannot be undone.
              All interview questions associated with this job type will also be deleted.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                "Delete"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
