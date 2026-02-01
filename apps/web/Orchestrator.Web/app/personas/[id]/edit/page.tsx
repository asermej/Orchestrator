"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ImageUpload } from "@/components/image-upload";
import { ArrowLeft, Loader2, BookOpen, BookMarked, Save, AlertCircle } from "lucide-react";
import Link from "next/link";
import { fetchPersonaById, updatePersona } from "./actions";
import { PersonaItem } from "../../actions";
import { uploadImage } from "@/lib/upload-image";
import { 
  fetchPersonaTraining, 
  updatePersonaTraining 
} from "../train/actions";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { VoiceInputToggle } from "@/components/voice-input-toggle";
import { useServerAction } from "@/lib/use-server-action";

export default function EditPersona() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const personaId = params.id as string;

  const [persona, setPersona] = useState<PersonaItem | null>(null);
  const [isLoadingPersona, setIsLoadingPersona] = useState(true);
  const [profileImageUrl, setProfileImageUrl] = useState<string>("");

  // Training state
  const [trainingContent, setTrainingContent] = useState("");
  const [isLoadingTraining, setIsLoadingTraining] = useState(true);

  // Server actions with error handling
  const { execute: executeProfileUpdate, isLoading: isSubmitting } = useServerAction(
    async (formData: FormData) => {
      if (profileImageUrl) {
        formData.set("profileImageUrl", profileImageUrl);
      }
      await updatePersona(personaId, formData);
    },
    {
      successMessage: "Persona profile updated successfully!",
      onSuccess: () => router.push("/personas"),
    }
  );

  const { execute: executeTrainingUpdate, isLoading: isSavingTraining } = useServerAction(
    () => updatePersonaTraining(personaId, trainingContent),
    {
      successMessage: "Training data saved successfully!",
    }
  );

  const MAX_GENERAL_TRAINING_LENGTH = 5000;

  // Voice input
  const {
    isRecording,
    isSupported,
    error: voiceError,
    toggleRecording,
  } = useVoiceInput({
    onTranscript: (text) => {
      setTrainingContent((prev) => {
        // Add space if there's existing content
        const separator = prev.trim() ? " " : "";
        return prev + separator + text;
      });
    },
    onError: (error) => {
      console.error("Voice input error:", error);
    },
  });

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && personaId) {
      loadPersona();
    }
  }, [user, personaId]);

  const loadPersona = async () => {
    try {
      setIsLoadingPersona(true);
      setIsLoadingTraining(true);
      
      // Load persona details and training data in parallel
      const [data, trainingData] = await Promise.all([
        fetchPersonaById(personaId),
        fetchPersonaTraining(personaId)
      ]);
      
      setPersona(data);
      setProfileImageUrl(data.profileImageUrl || "");
      setTrainingContent(trainingData.trainingContent || "");
    } catch (err) {
      console.error("Error loading persona:", err);
      setError("Failed to load persona. Please try again.");
    } finally {
      setIsLoadingPersona(false);
      setIsLoadingTraining(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    await executeProfileUpdate(formData);
  };

  const handleTrainingSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeTrainingUpdate();
  };

  const handleClearTraining = () => {
    if (confirm("Are you sure you want to clear all training data? This cannot be undone.")) {
      setTrainingContent("");
    }
  };

  // Character count calculations
  const characterCount = trainingContent.length;
  const isNearLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH * 0.9;
  const isOverLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH;

  if (isUserLoading || isLoadingPersona) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user || !persona) {
    return null;
  }

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      {/* Breadcrumb Header */}
      <div className="border-b bg-muted/20">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-4">
            <Link href="/personas">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <h1 className="text-2xl font-semibold">Edit Persona</h1>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          {/* Topic Knowledge Card */}
          <Card className="mb-6 bg-gradient-to-br from-primary/5 to-primary/10 border-primary/20">
            <CardContent className="pt-6">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <BookMarked className="h-5 w-5 text-primary" />
                    <h3 className="font-semibold text-lg">Add Topic Knowledge</h3>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    Add specific topics {persona.displayName} is knowledgeable about that others can discuss with them.
                  </p>
                </div>
                <Link href={`/personas/${personaId}/train`}>
                  <Button className="whitespace-nowrap">
                    <BookMarked className="mr-2 h-4 w-4" />
                    Add Topics
                  </Button>
                </Link>
              </div>
            </CardContent>
          </Card>

          {/* General Training Card */}
          <Card className="mb-6">
            <CardHeader>
              <div className="flex items-start gap-3">
                <BookOpen className="h-6 w-6 text-primary mt-1" />
                <div className="flex-1">
                  <CardTitle>General Training</CardTitle>
                  <CardDescription className="mt-2">
                    Provide background information, personality traits, knowledge, and characteristics 
                    for {persona.displayName}. This training will be included in every conversation.
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              {isLoadingTraining ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin" />
                </div>
              ) : (
                <form onSubmit={handleTrainingSubmit} className="space-y-6">
                  {/* Training Content */}
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <Label htmlFor="trainingContent">
                        Training Content
                      </Label>
                      <VoiceInputToggle
                        isRecording={isRecording}
                        isSupported={isSupported}
                        error={voiceError}
                        onToggle={toggleRecording}
                        disabled={isSavingTraining}
                      />
                    </div>
                    <Textarea
                      id="trainingContent"
                      name="trainingContent"
                      placeholder="Example: You are a wise Jedi Master with 900 years of experience. You speak in riddles and backwards sentences..."
                      value={trainingContent}
                      onChange={(e) => setTrainingContent(e.target.value)}
                      disabled={isSavingTraining}
                      rows={12}
                      className="resize-none font-mono text-sm"
                    />
                    <div className="flex items-center justify-between text-sm">
                      <p className="text-muted-foreground">
                        Maximum {MAX_GENERAL_TRAINING_LENGTH.toLocaleString()} characters (~1,250 tokens)
                      </p>
                      <p className={`font-medium ${
                        isOverLimit 
                          ? "text-destructive" 
                          : isNearLimit 
                            ? "text-yellow-600 dark:text-yellow-400" 
                            : "text-muted-foreground"
                      }`}>
                        {characterCount.toLocaleString()} / {MAX_GENERAL_TRAINING_LENGTH.toLocaleString()}
                        {isOverLimit && " (over limit)"}
                      </p>
                    </div>
                  </div>

                  {/* Training Tips */}
                  <div className="bg-muted/50 rounded-lg p-4 space-y-2">
                    <h3 className="font-semibold text-sm">💡 Training Tips</h3>
                    <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside">
                      <li>Describe their personality, mannerisms, and speaking style</li>
                      <li>Include their background, history, and life experiences</li>
                      <li>Mention their knowledge areas, expertise, and interests</li>
                      <li>Add specific phrases or expressions they commonly use</li>
                      <li>Keep it concise - this data is loaded in every conversation</li>
                    </ul>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex flex-col sm:flex-row justify-between gap-4">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={handleClearTraining}
                      disabled={isSavingTraining || !trainingContent}
                    >
                      Clear All
                    </Button>
                    
                    <Button 
                      type="submit" 
                      className="w-full sm:w-auto" 
                      disabled={isSavingTraining || isOverLimit}
                    >
                      {isSavingTraining ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Saving...
                        </>
                      ) : (
                        <>
                          <Save className="mr-2 h-4 w-4" />
                          Save Training
                        </>
                      )}
                    </Button>
                  </div>
                </form>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Edit {persona.displayName}</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-6">
                {/* Display Name (Required) */}
                <div className="space-y-2">
                  <Label htmlFor="displayName">
                    Display Name <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="displayName"
                    name="displayName"
                    placeholder="e.g., Yoda, John Smith, or Madonna"
                    defaultValue={persona.displayName}
                    required
                    disabled={isSubmitting}
                  />
                  <p className="text-sm text-muted-foreground">
                    This is how the persona will be identified. Must be unique.
                  </p>
                </div>

                {/* First Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input
                    id="firstName"
                    name="firstName"
                    placeholder="Optional - e.g., John"
                    defaultValue={persona.firstName || ""}
                    disabled={isSubmitting}
                  />
                  <p className="text-sm text-muted-foreground">
                    Optional. Leave blank for single-name personas.
                  </p>
                </div>

                {/* Last Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input
                    id="lastName"
                    name="lastName"
                    placeholder="Optional - e.g., Smith"
                    defaultValue={persona.lastName || ""}
                    disabled={isSubmitting}
                  />
                </div>

                {/* Profile Image Upload */}
                <div className="space-y-2">
                  <Label>Profile Image</Label>
                  <ImageUpload
                    value={profileImageUrl}
                    onChange={setProfileImageUrl}
                    onRemove={() => setProfileImageUrl("")}
                    disabled={isSubmitting}
                    uploadAction={uploadImage}
                  />
                  <p className="text-sm text-muted-foreground">
                    Optional. Upload a new image to replace the current one, or remove it.
                  </p>
                  <input 
                    type="hidden" 
                    name="profileImageUrl" 
                    value={profileImageUrl} 
                  />
                </div>

                {/* Save Button */}
                <div className="flex justify-end gap-4">
                  <Link href="/personas">
                    <Button type="button" variant="outline" disabled={isSubmitting}>
                      Cancel
                    </Button>
                  </Link>
                  <Button type="submit" className="w-full sm:w-auto" disabled={isSubmitting}>
                    {isSubmitting ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      "Save Changes"
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}

