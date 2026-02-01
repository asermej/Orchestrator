"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Save, BookOpen, Mic, Volume2, Search } from "lucide-react";
import Link from "next/link";
import { fetchPersonaById } from "../../actions";
import { PersonaAvatar } from "@/components/persona-avatar";
import { 
  fetchPersonaTraining, 
  updatePersonaTraining 
} from "../../../personas/[id]/train/actions";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { VoiceInputToggle } from "@/components/voice-input-toggle";
import { useServerAction } from "@/lib/use-server-action";
import {
  getStockVoices,
  selectPersonaVoice,
  type VoiceItem,
  type AvailableVoicesResponse,
} from "../../voice-actions";
import { CreateMyVoiceWizard } from "@/components/create-my-voice-wizard";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";

const MAX_GENERAL_TRAINING_LENGTH = 5000;

export default function GeneralTraining() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const personaId = params.id as string;
  const isOnboarding = searchParams.get("onboarding") === "true";

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");

  // Training state
  const [trainingContent, setTrainingContent] = useState("");
  const [isLoadingTraining, setIsLoadingTraining] = useState(true);

  // Voice state (from persona)
  const [voiceProvider, setVoiceProvider] = useState<string | null>(null);
  const [voiceType, setVoiceType] = useState<string | null>(null);
  const [voiceName, setVoiceName] = useState<string | null>(null);
  const [voiceCreatedAt, setVoiceCreatedAt] = useState<string | null>(null);
  const [chooseVoiceOpen, setChooseVoiceOpen] = useState(false);
  const [createVoiceOpen, setCreateVoiceOpen] = useState(false);
  const [testVoiceOpen, setTestVoiceOpen] = useState(false);
  const [voices, setVoices] = useState<VoiceItem[]>([]);
  const [voicesLoading, setVoicesLoading] = useState(false);
  const [voiceSearch, setVoiceSearch] = useState("");
  const [voiceTagFilter, setVoiceTagFilter] = useState<string | null>(null);
  const [previewingVoiceId, setPreviewingVoiceId] = useState<string | null>(null);
  const [selectingVoiceId, setSelectingVoiceId] = useState<string | null>(null);
  const [testVoiceText, setTestVoiceText] = useState("Hey — I'm your Surrova persona voice.");
  const [testVoiceLoading, setTestVoiceLoading] = useState(false);

  // Server action for saving training
  const { execute: executeSave, isLoading: isSavingTraining } = useServerAction(
    (continueOnboarding: boolean) => updatePersonaTraining(personaId, trainingContent),
    {
      successMessage: "Training data saved successfully!",
      onSuccess: () => {
        // Handled by continueOnboarding parameter
      },
    }
  );

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
      setIsLoading(true);
      setIsLoadingTraining(true);
      setError(null);
      
      // Load persona details and training data in parallel
      const [persona, trainingData] = await Promise.all([
        fetchPersonaById(personaId),
        fetchPersonaTraining(personaId)
      ]);
      
      setDisplayName(persona.displayName);
      setProfileImageUrl(persona.profileImageUrl || "");
      setTrainingContent(trainingData.trainingContent || "");
      setVoiceProvider(persona.voiceProvider ?? null);
      setVoiceType(persona.voiceType ?? null);
      setVoiceName(persona.voiceName ?? null);
      setVoiceCreatedAt(persona.voiceCreatedAt ?? null);
    } catch (err) {
      console.error("Error loading persona:", err);
      setError("Failed to load persona. Please try again.");
    } finally {
      setIsLoading(false);
      setIsLoadingTraining(false);
    }
  };

  const handleTrainingSubmit = async (e: React.FormEvent, continueOnboarding: boolean = false) => {
    e.preventDefault();
    
    await executeSave(continueOnboarding);
    
    if (continueOnboarding) {
      // Redirect to training hub with onboarding flag after a short delay
      setTimeout(() => {
        router.push(`/my-personas/${personaId}/training?onboarding=true`);
      }, 1000);
    }
  };

  const handleClearTraining = () => {
    if (confirm("Are you sure you want to clear all training data? This cannot be undone.")) {
      setTrainingContent("");
    }
  };

  const voiceSourceText = !voiceProvider || !voiceName
    ? "Default voice"
    : voiceType === "user_cloned"
      ? "Custom"
      : "Stock";
  const voiceLastUpdatedText = voiceType === "user_cloned" && voiceCreatedAt
    ? new Date(voiceCreatedAt).toLocaleDateString(undefined, { dateStyle: "short" })
    : "—";
  const voiceStatusText =
    !voiceProvider || !voiceName
      ? "Default voice"
      : voiceType === "user_cloned"
        ? `Custom voice: ${voiceName}`
        : `Selected voice: ${voiceName}`;

  const openChooseVoice = async () => {
    setChooseVoiceOpen(true);
    setVoicesLoading(true);
    setVoiceSearch("");
    setVoiceTagFilter(null);
    try {
      const res: AvailableVoicesResponse = await getStockVoices();
      setVoices(res.curatedPrebuiltVoices ?? []);
    } catch (err) {
      console.error("Failed to load voices", err);
      toast.error("Failed to load voices");
    } finally {
      setVoicesLoading(false);
    }
  };

  const allTags = Array.from(
    new Set(voices.flatMap((v) => v.tags ?? []))
  ).filter(Boolean).sort();
  const filteredVoices = voices.filter((voice) => {
    const matchSearch =
      !voiceSearch.trim() ||
      voice.name.toLowerCase().includes(voiceSearch.toLowerCase()) ||
      (voice.description?.toLowerCase().includes(voiceSearch.toLowerCase()));
    const matchTag =
      !voiceTagFilter ||
      (voice.tags && voice.tags.includes(voiceTagFilter));
    return matchSearch && matchTag;
  });

  const handlePreviewVoice = async (voice: VoiceItem) => {
    setPreviewingVoiceId(voice.id);
    try {
      const previewText = voice.previewText ?? "Hey — I'm your Surrova persona voice.";
      const res = await fetch("/api/voice/preview", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ voiceId: voice.id, text: previewText }),
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        toast.error(data.error ?? "Preview failed");
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const audio = new Audio(url);
      await audio.play();
      audio.onended = () => URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Preview error", err);
      toast.error("Failed to play preview");
    } finally {
      setPreviewingVoiceId(null);
    }
  };

  const handleSelectVoice = async (voice: VoiceItem) => {
    setSelectingVoiceId(voice.id);
    try {
      await selectPersonaVoice(personaId, "elevenlabs", voice.voiceType || "prebuilt", voice.id, voice.name);
      toast.success(`Voice "${voice.name}" selected`);
      setChooseVoiceOpen(false);
      await loadPersona();
    } catch (err) {
      console.error("Select voice error", err);
      toast.error("Failed to select voice");
    } finally {
      setSelectingVoiceId(null);
    }
  };

  const handleTestVoice = async () => {
    setTestVoiceLoading(true);
    try {
      const res = await fetch(`/api/personas/${personaId}/voice/test`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text: testVoiceText || "Hey — I'm your Surrova persona voice." }),
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        toast.error(data.error ?? "Test failed");
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const audio = new Audio(url);
      await audio.play();
      audio.onended = () => URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Test voice error", err);
      toast.error("Failed to play test");
    } finally {
      setTestVoiceLoading(false);
    }
  };

  // Character count calculations
  const characterCount = trainingContent.length;
  const isNearLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH * 0.9;
  const isOverLimit = characterCount > MAX_GENERAL_TRAINING_LENGTH;

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

      {/* Hero Header with Persona Info */}
      <div className="border-b bg-gradient-to-r from-green-50 via-emerald-50 to-green-50 dark:from-green-950/20 dark:via-emerald-950/20 dark:to-green-950/20">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center gap-6">
            <Link href={isOnboarding ? `/my-personas/${personaId}/edit?onboarding=true` : `/my-personas/${personaId}/training`}>
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
                <h1 className="text-3xl font-bold">{displayName || "Persona"}</h1>
                <p className="text-muted-foreground mt-1">General Training</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Onboarding Progress */}
          {isOnboarding && (
            <Card className="border-2 border-primary">
              <CardContent className="p-6">
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-green-500 text-white flex items-center justify-center font-semibold">
                      ✓
                    </div>
                    <span className="text-muted-foreground">Profile</span>
                  </div>
                  <div className="flex-1 h-px bg-border" />
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-semibold">
                      2
                    </div>
                    <span className="font-medium">General Training</span>
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

          {/* Voice Card */}
          <Card className="shadow-lg">
            <CardHeader className="bg-gradient-to-r from-violet-100 via-purple-100 to-violet-100 dark:from-violet-950/20 dark:via-purple-950/20 dark:to-violet-950/20 border-b">
              <div className="flex items-start gap-3">
                <div className="p-2 rounded-lg bg-violet-200 dark:bg-violet-800/30">
                  <Mic className="h-6 w-6 text-violet-700 dark:text-violet-400" />
                </div>
                <div className="flex-1">
                  <CardTitle className="text-2xl">Voice</CardTitle>
                  <CardDescription className="mt-2 text-base">
                    Choose a prebuilt voice or create your own. This voice is used when the persona speaks.
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6 space-y-4">
              <div className="space-y-1">
                <p className="text-sm font-medium text-muted-foreground">
                  Current: {voiceStatusText}
                </p>
                <p className="text-xs text-muted-foreground">
                  Source: {voiceSourceText} · Last updated: {voiceLastUpdatedText}
                </p>
              </div>
              <div className="flex flex-wrap gap-3">
                <Button type="button" variant="default" onClick={openChooseVoice}>
                  Choose a voice
                </Button>
                <Button type="button" variant="outline" onClick={() => setCreateVoiceOpen(true)}>
                  Create my voice
                </Button>
                <Button type="button" variant="outline" onClick={() => setTestVoiceOpen(true)}>
                  Test voice
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* General Training Card */}
          <Card className="shadow-lg">
            <CardHeader className="bg-gradient-to-r from-green-100 via-emerald-100 to-green-100 dark:from-green-900/20 dark:via-emerald-900/20 dark:to-green-900/20 border-b">
              <div className="flex items-start gap-3">
                <div className="p-2 rounded-lg bg-green-200 dark:bg-green-800/30">
                  <BookOpen className="h-6 w-6 text-green-700 dark:text-green-400" />
                </div>
                <div className="flex-1">
                  <CardTitle className="text-2xl">General Training</CardTitle>
                  <CardDescription className="mt-2 text-base">
                    Provide background information, personality traits, knowledge, and characteristics 
                    for {displayName || "this persona"}. This training will be included in every conversation.
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6">
              {isLoadingTraining ? (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin" />
                </div>
              ) : (
                <form onSubmit={(e) => handleTrainingSubmit(e, false)} className="space-y-6">
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
                      rows={16}
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
                  <div className="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-950/20 dark:to-emerald-950/20 rounded-lg p-5 border border-green-200 dark:border-green-800">
                    <h3 className="font-semibold text-sm mb-3 flex items-center gap-2">
                      <BookOpen className="h-4 w-4" />
                      💡 Training Tips
                    </h3>
                    <ul className="text-sm text-muted-foreground space-y-2 list-disc list-inside">
                      <li>Describe their personality, mannerisms, and speaking style</li>
                      <li>Include their background, history, and life experiences</li>
                      <li>Mention their knowledge areas, expertise, and interests</li>
                      <li>Add specific phrases or expressions they commonly use</li>
                      <li>Keep it concise - this data is loaded in every conversation</li>
                    </ul>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex flex-col sm:flex-row justify-between gap-4">
                    {!isOnboarding && (
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleClearTraining}
                        disabled={isSavingTraining || !trainingContent}
                      >
                        Clear All
                      </Button>
                    )}
                    
                    <div className={`flex gap-3 ${isOnboarding ? 'flex-1' : 'flex-1 sm:flex-none'}`}>
                      {!isOnboarding && (
                        <Button 
                          type="submit" 
                          className="flex-1 sm:w-auto" 
                          disabled={isSavingTraining || isOverLimit}
                          size="lg"
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
                      )}
                      {isOnboarding && (
                        <>
                          <Button
                            type="button"
                            variant="outline"
                            onClick={() => router.push(`/my-personas/${personaId}/training`)}
                            disabled={isSavingTraining}
                            className="flex-1"
                          >
                            Skip for Now
                          </Button>
                          <Button 
                            type="button"
                            onClick={(e) => handleTrainingSubmit(e, true)}
                            className="flex-1" 
                            disabled={isSavingTraining || isOverLimit}
                            size="lg"
                          >
                            {isSavingTraining ? (
                              <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                Saving...
                              </>
                            ) : (
                              <>
                                <Save className="mr-2 h-4 w-4" />
                                Save & Continue
                              </>
                            )}
                          </Button>
                        </>
                      )}
                    </div>
                  </div>
                </form>
              )}
            </CardContent>
          </Card>
        </div>
      </main>

      {/* Create my voice wizard */}
      <CreateMyVoiceWizard
        open={createVoiceOpen}
        onOpenChange={setCreateVoiceOpen}
        personaId={personaId}
        personaName={displayName || "Persona"}
        onSuccess={loadPersona}
      />

      {/* Choose voice modal */}
      <Dialog open={chooseVoiceOpen} onOpenChange={setChooseVoiceOpen}>
        <DialogContent className="max-w-lg max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Choose a voice</DialogTitle>
          </DialogHeader>
          {voicesLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin" />
            </div>
          ) : (
            <>
              <div className="space-y-2">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search by name or description..."
                    value={voiceSearch}
                    onChange={(e) => setVoiceSearch(e.target.value)}
                    className="pl-9"
                  />
                </div>
                {allTags.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant={voiceTagFilter === null ? "default" : "outline"}
                      size="sm"
                      onClick={() => setVoiceTagFilter(null)}
                    >
                      All
                    </Button>
                    {allTags.map((tag) => (
                      <Button
                        key={tag}
                        type="button"
                        variant={voiceTagFilter === tag ? "default" : "outline"}
                        size="sm"
                        onClick={() => setVoiceTagFilter(tag)}
                      >
                        {tag}
                      </Button>
                    ))}
                  </div>
                )}
              </div>
              <ul className="space-y-2 mt-4">
                {filteredVoices.map((voice) => (
                  <li
                    key={voice.id}
                    className="flex items-center justify-between gap-4 rounded-lg border p-3"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">{voice.name}</p>
                      {voice.description && (
                        <p className="text-xs text-muted-foreground truncate">
                          {voice.description}
                        </p>
                      )}
                      {voice.tags && voice.tags.length > 0 && (
                        <p className="text-xs text-muted-foreground mt-0.5">
                          {voice.tags.join(" · ")}
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2 shrink-0">
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => handlePreviewVoice(voice)}
                        disabled={previewingVoiceId !== null}
                      >
                        {previewingVoiceId === voice.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Volume2 className="h-4 w-4" />
                        )}
                        <span className="sr-only">Preview</span>
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        onClick={() => handleSelectVoice(voice)}
                        disabled={selectingVoiceId !== null}
                      >
                        {selectingVoiceId === voice.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          "Select"
                        )}
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
              {filteredVoices.length === 0 && !voicesLoading && (
                <p className="text-sm text-muted-foreground text-center py-4">
                  No voices match your search.
                </p>
              )}
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Test voice modal */}
      <Dialog open={testVoiceOpen} onOpenChange={setTestVoiceOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Test voice</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Enter a short phrase to hear the persona&apos;s current voice.
          </p>
          <div className="space-y-2">
            <Label htmlFor="test-voice-text">Phrase</Label>
            <Textarea
              id="test-voice-text"
              value={testVoiceText}
              onChange={(e) => setTestVoiceText(e.target.value)}
              placeholder="Hey — I'm your Surrova persona voice."
              rows={3}
              className="resize-none"
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setTestVoiceOpen(false)}>
              Close
            </Button>
            <Button
              type="button"
              onClick={handleTestVoice}
              disabled={testVoiceLoading}
            >
              {testVoiceLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Generating...
                </>
              ) : (
                <>
                  <Volume2 className="mr-2 h-4 w-4" />
                  Generate & play
                </>
              )}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

