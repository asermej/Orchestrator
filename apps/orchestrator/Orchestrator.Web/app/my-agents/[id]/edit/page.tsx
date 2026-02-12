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
import { ArrowLeft, Loader2, Save, BookOpen, Trash2, AlertTriangle, Mic, Volume2, Search } from "lucide-react";
import Link from "next/link";
import { fetchAgentById, updateAgent, deleteAgent } from "../../actions";
import { ImageUpload } from "@/components/image-upload";
import { AgentAvatar } from "@/components/agent-avatar";
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  getStockVoices,
  selectAgentVoice,
  type VoiceItem,
  type AvailableVoicesResponse,
} from "../../voice-actions";
import { toast } from "sonner";

export default function EditAgent() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const agentId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  // Profile state
  const [displayName, setDisplayName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");

  // Voice state
  const [voiceName, setVoiceName] = useState<string | null>(null);
  const [chooseVoiceOpen, setChooseVoiceOpen] = useState(false);
  const [testVoiceOpen, setTestVoiceOpen] = useState(false);
  const [voices, setVoices] = useState<VoiceItem[]>([]);
  const [voicesLoading, setVoicesLoading] = useState(false);
  const [voiceSearch, setVoiceSearch] = useState("");
  const [voiceTagFilter, setVoiceTagFilter] = useState<string | null>(null);
  const [previewingVoiceId, setPreviewingVoiceId] = useState<string | null>(null);
  const [selectingVoiceId, setSelectingVoiceId] = useState<string | null>(null);
  const [testVoiceText, setTestVoiceText] = useState("Hello! I'm your AI interviewer. How are you today?");
  const [testVoiceLoading, setTestVoiceLoading] = useState(false);

  // Server actions with error handling
  const { execute: executeUpdate, isLoading: isSaving } = useServerAction(
    async () => {
      // Validation
      if (!displayName.trim()) {
        throw new Error("Display name is required");
      }

      await updateAgent(agentId, {
        displayName: displayName.trim(),
        profileImageUrl: profileImageUrl.trim() || null,
      });
    },
    {
      successMessage: "Profile updated successfully!",
    }
  );

  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    () => deleteAgent(agentId),
    {
      successMessage: "Agent deleted successfully!",
      onSuccess: () => router.push("/my-agents"),
      onError: () => setDeleteDialogOpen(false),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && agentId) {
      loadAgent();
    }
  }, [user, agentId]);

  const loadAgent = async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      const agent = await fetchAgentById(agentId);
      
      setDisplayName(agent.displayName);
      setProfileImageUrl(agent.profileImageUrl || "");
      setVoiceName(agent.voiceName ?? null);
    } catch (err) {
      console.error("Error loading agent:", err);
      setError("Failed to load agent. Please try again.");
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

  // Voice helper functions
  const voiceStatusText = voiceName ? `Selected voice: ${voiceName}` : "Default voice";

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
      const previewText = voice.previewText ?? "Hello! I'm your AI interviewer.";
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
      await selectAgentVoice(agentId, "elevenlabs", voice.voiceType || "prebuilt", voice.id, voice.name);
      toast.success(`Voice "${voice.name}" selected`);
      setChooseVoiceOpen(false);
      await loadAgent();
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
      const res = await fetch(`/api/agents/${agentId}/voice/test`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text: testVoiceText || "Hello! I'm your AI interviewer." }),
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
              <Link href="/my-agents">
                <Button variant="ghost" size="icon" className="rounded-full">
                  <ArrowLeft className="h-5 w-5" />
                </Button>
              </Link>
              <div className="flex items-center gap-4">
                <AgentAvatar
                  imageUrl={profileImageUrl}
                  displayName={displayName}
                  size="xl"
                  shape="square"
                />
                <div>
                  <h1 className="text-3xl font-bold">{displayName || "Edit Agent"}</h1>
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
                Update the basic information for this agent
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
                    placeholder="e.g., Alex, Jordan, or Sam"
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    required
                    disabled={isSaving}
                  />
                  <p className="text-sm text-muted-foreground">
                    This is how the agent will be identified.
                  </p>
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
                      <AgentAvatar
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

                {/* Action Buttons */}
                <div className="flex flex-col sm:flex-row gap-3 pt-4">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => router.push("/my-agents")}
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
                    Choose a voice for this agent. This voice is used when the agent speaks.
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6 space-y-4">
              <div className="space-y-1">
                <p className="text-sm font-medium text-muted-foreground">
                  Current: {voiceStatusText}
                </p>
              </div>
              <div className="flex flex-wrap gap-3">
                <Button type="button" variant="default" onClick={openChooseVoice}>
                  Choose a voice
                </Button>
                <Button type="button" variant="outline" onClick={() => setTestVoiceOpen(true)}>
                  Test voice
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Quick Links */}
          <Card className="bg-muted/50">
            <CardContent className="p-6">
              <h3 className="font-semibold mb-4">Quick Links</h3>
              <Link href={`/my-agents/${agentId}/general-training`}>
                <Button variant="outline" className="w-full justify-start">
                  <BookOpen className="mr-2 h-4 w-4" />
                  General Training
                </Button>
              </Link>
            </CardContent>
          </Card>

          {/* Danger Zone */}
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
                    <h4 className="font-semibold text-sm">Delete this agent</h4>
                    <p className="text-sm text-muted-foreground mt-1">
                      Once you delete an agent, there is no going back. All training data will be lost.
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
                  Delete Agent
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

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
            Enter a short phrase to hear the agent&apos;s current voice.
          </p>
          <div className="space-y-2">
            <Label htmlFor="test-voice-text">Phrase</Label>
            <Textarea
              id="test-voice-text"
              value={testVoiceText}
              onChange={(e) => setTestVoiceText(e.target.value)}
              placeholder="Hello! I'm your AI interviewer. How are you today?"
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
