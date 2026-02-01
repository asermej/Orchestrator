"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Save, BookOpen, Trash2, AlertTriangle, Volume2 } from "lucide-react";
import { Slider } from "@/components/ui/slider";
import Link from "next/link";
import { fetchPersonaById, updatePersona, deletePersona } from "../../actions";
import { ImageUpload } from "@/components/image-upload";
import { PersonaAvatar } from "@/components/persona-avatar";
import { uploadImage } from "@/lib/upload-image";
import { useServerAction } from "@/lib/use-server-action";
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

export default function EditPersona() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const personaId = params.id as string;
  const isOnboarding = searchParams.get("onboarding") === "true";

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  // Profile state
  const [displayName, setDisplayName] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");
  
  // Voice settings state
  const [elevenLabsVoiceId, setElevenLabsVoiceId] = useState("");
  const [voiceStability, setVoiceStability] = useState(0.5);
  const [voiceSimilarityBoost, setVoiceSimilarityBoost] = useState(0.75);

  // Server actions with error handling
  const { execute: executeUpdate, isLoading: isSaving } = useServerAction(
    async () => {
      // Validation
      if (!displayName.trim()) {
        throw new Error("Display name is required");
      }

      await updatePersona(personaId, {
        displayName: displayName.trim(),
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        profileImageUrl: profileImageUrl.trim() || null,
        elevenLabsVoiceId: elevenLabsVoiceId.trim() || null,
        voiceStability: voiceStability,
        voiceSimilarityBoost: voiceSimilarityBoost,
      });
    },
    {
      successMessage: "Profile updated successfully!",
      onSuccess: () => {
        if (isOnboarding) {
          setTimeout(() => {
            router.push(`/my-personas/${personaId}/general-training?onboarding=true`);
          }, 1000);
        }
      },
    }
  );

  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    () => deletePersona(personaId),
    {
      successMessage: "Persona deleted successfully!",
      onSuccess: () => router.push("/my-personas"),
      onError: () => setDeleteDialogOpen(false),
    }
  );

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
      setIsLoading(true);
      setError(null);
      
      const persona = await fetchPersonaById(personaId);
      
      setDisplayName(persona.displayName);
      setFirstName(persona.firstName || "");
      setLastName(persona.lastName || "");
      setProfileImageUrl(persona.profileImageUrl || "");
      setElevenLabsVoiceId(persona.elevenLabsVoiceId || "");
      setVoiceStability(persona.voiceStability ?? 0.5);
      setVoiceSimilarityBoost(persona.voiceSimilarityBoost ?? 0.75);
    } catch (err) {
      console.error("Error loading persona:", err);
      setError("Failed to load persona. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeUpdate();
  };

  const handleDelete = async () => {
    await executeDelete();
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

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      {/* Hero Header */}
      <div className="border-b bg-gradient-to-r from-primary/5 via-primary/10 to-primary/5">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6">
              <Link href="/my-personas">
                <Button variant="ghost" size="icon" className="rounded-full">
                  <ArrowLeft className="h-5 w-5" />
                </Button>
              </Link>
              <div className="flex items-center gap-4">
                <PersonaAvatar
                  imageUrl={profileImageUrl}
                  displayName={displayName}
                  size="xl"
                  shape="square"
                />
                <div>
                  <h1 className="text-3xl font-bold">{displayName || "Edit Persona"}</h1>
                  <p className="text-muted-foreground mt-1">Basic Profile Information</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto space-y-6">
          {/* Onboarding Progress */}
          {isOnboarding && (
            <Card className="border-2 border-primary">
              <CardContent className="p-6">
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-semibold">
                      1
                    </div>
                    <span className="font-medium">Profile</span>
                  </div>
                  <div className="flex-1 h-px bg-border" />
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-muted text-muted-foreground flex items-center justify-center font-semibold">
                      2
                    </div>
                    <span className="text-muted-foreground">General Training</span>
                  </div>
                  <div className="flex-1 h-px bg-border" />
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-muted text-muted-foreground flex items-center justify-center font-semibold">
                      3
                    </div>
                    <span className="text-muted-foreground">Topics</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Error Message */}
          {error && (
            <div className="p-4 bg-destructive/10 text-destructive rounded-lg border border-destructive/20">
              {error}
            </div>
          )}

          {/* Profile Edit Card */}
          <Card>
            <CardHeader>
              <CardTitle>Profile Information</CardTitle>
              <CardDescription>
                Update the basic information for this persona
              </CardDescription>
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
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    required
                    disabled={isSaving}
                  />
                  <p className="text-sm text-muted-foreground">
                    This is how the persona will be identified.
                  </p>
                </div>

                {/* First Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input
                    id="firstName"
                    name="firstName"
                    placeholder="Optional - e.g., John"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    disabled={isSaving}
                  />
                </div>

                {/* Last Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input
                    id="lastName"
                    name="lastName"
                    placeholder="Optional - e.g., Smith"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    disabled={isSaving}
                  />
                </div>

                {/* Profile Image (Optional) */}
                <div className="space-y-4">
                  <div className="flex items-center gap-4">
                    <div>
                      <Label>Profile Image</Label>
                      <p className="text-sm text-muted-foreground mt-1">
                        Upload an image or leave blank for default avatar
                      </p>
                    </div>
                    {profileImageUrl && (
                      <PersonaAvatar
                        imageUrl={profileImageUrl}
                        displayName={displayName || "Preview"}
                        size="lg"
                        shape="square"
                      />
                    )}
                  </div>
                  <ImageUpload
                    value={profileImageUrl}
                    onChange={setProfileImageUrl}
                    onRemove={() => setProfileImageUrl("")}
                    maxSizeMB={10}
                    maxWidthOrHeight={800}
                    disabled={isSaving}
                    uploadAction={uploadImage}
                  />
                </div>

                {/* Voice Settings */}
                <div className="space-y-4 pt-4 border-t">
                  <div className="flex items-center gap-2">
                    <Volume2 className="h-5 w-5" />
                    <h3 className="font-medium">Voice Settings</h3>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    Configure the voice for this persona when using voice conversation mode.
                  </p>
                  
                  {/* ElevenLabs Voice ID */}
                  <div className="space-y-2">
                    <Label htmlFor="elevenLabsVoiceId">ElevenLabs Voice ID</Label>
                    <Input
                      id="elevenLabsVoiceId"
                      name="elevenLabsVoiceId"
                      placeholder="e.g., 21m00Tcm4TlvDq8ikWAM"
                      value={elevenLabsVoiceId}
                      onChange={(e) => setElevenLabsVoiceId(e.target.value)}
                      disabled={isSaving}
                    />
                    <p className="text-xs text-muted-foreground">
                      Leave empty to use the default voice. Find voice IDs at{" "}
                      <a
                        href="https://elevenlabs.io/voice-library"
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-primary underline"
                      >
                        ElevenLabs Voice Library
                      </a>
                    </p>
                  </div>

                  {/* Voice Stability */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <Label>Voice Stability</Label>
                      <span className="text-sm text-muted-foreground">
                        {(voiceStability * 100).toFixed(0)}%
                      </span>
                    </div>
                    <Slider
                      value={[voiceStability]}
                      onValueChange={(value) => setVoiceStability(value[0])}
                      min={0}
                      max={1}
                      step={0.05}
                      disabled={isSaving}
                      className="w-full"
                    />
                    <p className="text-xs text-muted-foreground">
                      Higher stability makes the voice more consistent. Lower values add more variation and emotion.
                    </p>
                  </div>

                  {/* Similarity Boost */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <Label>Similarity Boost</Label>
                      <span className="text-sm text-muted-foreground">
                        {(voiceSimilarityBoost * 100).toFixed(0)}%
                      </span>
                    </div>
                    <Slider
                      value={[voiceSimilarityBoost]}
                      onValueChange={(value) => setVoiceSimilarityBoost(value[0])}
                      min={0}
                      max={1}
                      step={0.05}
                      disabled={isSaving}
                      className="w-full"
                    />
                    <p className="text-xs text-muted-foreground">
                      Higher values make the voice sound more like the original. Lower values allow more variation.
                    </p>
                  </div>
                </div>

                {/* Action Buttons */}
                <div className="flex flex-col sm:flex-row gap-3 pt-4">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => router.push("/my-personas")}
                    disabled={isSaving}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" disabled={isSaving} className="flex-1">
                    {isSaving ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : isOnboarding ? (
                      <>
                        <Save className="mr-2 h-4 w-4" />
                        Save & Continue
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
            </CardContent>
          </Card>

          {/* Quick Links */}
          {!isOnboarding && (
            <Card className="bg-muted/50">
              <CardContent className="p-6">
                <h3 className="font-semibold mb-4">Quick Links</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <Link href={`/my-personas/${personaId}/general-training`}>
                    <Button variant="outline" className="w-full justify-start">
                      <BookOpen className="mr-2 h-4 w-4" />
                      General Training
                    </Button>
                  </Link>
                  <Link href={`/my-personas/${personaId}/training`}>
                    <Button variant="outline" className="w-full justify-start">
                      <BookOpen className="mr-2 h-4 w-4" />
                      Topic Training
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Danger Zone */}
          {!isOnboarding && (
            <Card className="border-destructive">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <AlertTriangle className="h-5 w-5 text-destructive" />
                  <CardTitle className="text-destructive">Danger Zone</CardTitle>
                </div>
                <CardDescription>
                  Irreversible and destructive actions
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between p-4 border border-destructive/20 rounded-lg bg-destructive/5">
                  <div>
                    <h4 className="font-semibold text-sm">Delete this persona</h4>
                    <p className="text-sm text-muted-foreground mt-1">
                      Once you delete a persona, there is no going back. All training data will be lost.
                    </p>
                  </div>
                  <Button
                    variant="destructive"
                    onClick={() => setDeleteDialogOpen(true)}
                    disabled={isDeleting}
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Delete
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </main>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete <strong>{displayName}</strong> and all associated training data.
              This action cannot be undone.
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
                <>
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete Persona
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
