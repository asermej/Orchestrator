"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ArrowLeft, Loader2, Save, GraduationCap, AlertCircle, X, Tag as TagIcon, Globe, Lock } from "lucide-react";
import Link from "next/link";
import { PersonaAvatar } from "@/components/persona-avatar";
import {
  updateTopicDetails,
  updateTopicTags,
  savePersonaTopicTraining,
  createTopicAndAddToPersona,
  type TopicDetails,
  type TagItem,
  type CategoryItem,
  type TopicTrainingContent,
} from "./actions";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Switch } from "@/components/ui/switch";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { VoiceInputToggle } from "@/components/voice-input-toggle";
import { useServerAction } from "@/lib/use-server-action";

const MAX_TRAINING_LENGTH = 50000;

interface TrainClientProps {
  personaId: string;
  topicId: string;
  displayName: string;
  profileImageUrl: string;
  categories: CategoryItem[];
  allTags: TagItem[];
  topicDetails?: TopicDetails;
  topicTags?: TagItem[];
  topicTraining?: TopicTrainingContent;
}

export function TrainClient({
  personaId,
  topicId,
  displayName,
  profileImageUrl,
  categories,
  allTags,
  topicDetails,
  topicTags: initialTopicTags = [],
  topicTraining,
}: TrainClientProps) {
  const router = useRouter();
  const isNewTopic = topicId === "new";

  // Topic details
  const [topicName, setTopicName] = useState(topicDetails?.name || "");
  const [topicDescription, setTopicDescription] = useState(topicDetails?.description || "");
  const [topicCategoryId, setTopicCategoryId] = useState(topicDetails?.categoryId || "");

  // Tags
  const [topicTags, setTopicTags] = useState<TagItem[]>(initialTopicTags);
  const [tagInput, setTagInput] = useState("");
  const [showTagPopover, setShowTagPopover] = useState(false);

  // Training content
  const [trainingContent, setTrainingContent] = useState(topicTraining?.trainingContent || "");
  const [trainingContentTouched, setTrainingContentTouched] = useState(false);
  const [contributionNotes, setContributionNotes] = useState(topicTraining?.contributionNotes || "");

  // Track original values for change detection
  const [originalValues, setOriginalValues] = useState({
    topicName: topicDetails?.name || "",
    topicDescription: topicDetails?.description || "",
    topicCategoryId: topicDetails?.categoryId || "",
    topicTags: initialTopicTags,
    trainingContent: topicTraining?.trainingContent || "",
    contributionNotes: topicTraining?.contributionNotes || "",
  });

  // Server action for saving (handles both create and update)
  const { execute: executeSave, isLoading: isSaving } = useServerAction(
    async (returnToHub: boolean) => {
      // Validate required fields
      if (!topicName.trim()) {
        throw new Error("Topic name is required");
      }
      if (!topicCategoryId) {
        throw new Error("Please select a category");
      }
      if (!trainingContent.trim()) {
        throw new Error("Training content is required");
      }

      if (isNewTopic) {
        // Create new topic with all data
        const tagNames = topicTags.map((t) => t.name);
        await createTopicAndAddToPersona(
          personaId,
          topicName,
          topicCategoryId,
          topicDescription,
          tagNames,
          trainingContent,
          contributionNotes
        );

        // Clear draft from localStorage
        const draftKey = `topic-draft-${personaId}-new`;
        if (typeof window !== "undefined") {
          localStorage.removeItem(draftKey);
        }

        return { isNew: true, returnToHub };
      } else {
        // Update existing topic
        const topicChanged =
          topicName !== originalValues.topicName ||
          topicDescription !== originalValues.topicDescription ||
          topicCategoryId !== originalValues.topicCategoryId ||
          contributionNotes !== originalValues.contributionNotes;

        const tagNames = topicTags.map((t) => t.name).sort();
        const originalTagNames = originalValues.topicTags.map((t) => t.name).sort();
        const tagsChanged = JSON.stringify(tagNames) !== JSON.stringify(originalTagNames);

        const trainingContentChanged = trainingContent !== originalValues.trainingContent;

        // Update topic metadata if changed (includes contribution notes)
        if (topicChanged) {
          await updateTopicDetails(
            topicId,
            topicName,
            topicCategoryId,
            topicDescription,
            contributionNotes
          );
        }

        // Update training content if changed
        if (trainingContentChanged) {
          await savePersonaTopicTraining(
            personaId,
            topicId,
            trainingContent
          );
        }

        // Update tags if changed
        if (tagsChanged) {
          await updateTopicTags(topicId, tagNames, personaId);
        }

        // Clear draft from localStorage
        const draftKey = `topic-draft-${personaId}-${topicId}`;
        if (typeof window !== "undefined") {
          localStorage.removeItem(draftKey);
        }

        // Update original values
        setOriginalValues({
          topicName,
          topicDescription,
          topicCategoryId,
          topicTags,
          trainingContent,
          contributionNotes,
        });

        return { isNew: false, returnToHub };
      }
    },
    {
      successMessage: (result) => result?.isNew ? "Topic created successfully!" : "Topic saved successfully!",
      onSuccess: (result) => {
        if (result?.isNew || result?.returnToHub) {
          setTimeout(() => {
            router.push(`/my-personas/${personaId}/training`);
          }, 500);
        }
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

  // Auto-save to localStorage
  useEffect(() => {
    if (typeof window !== "undefined" && trainingContent !== originalValues.trainingContent) {
      const draftKey = `topic-draft-${personaId}-${topicId}`;
      localStorage.setItem(draftKey, trainingContent);
    }
  }, [trainingContent, personaId, topicId, originalValues.trainingContent]);

  // Check for draft on mount (only for existing topics)
  useEffect(() => {
    if (!isNewTopic && typeof window !== "undefined" && topicTraining) {
      const draftKey = `topic-draft-${personaId}-${topicId}`;
      const draft = localStorage.getItem(draftKey);
      if (draft && draft !== topicTraining.trainingContent) {
        const useDraft = confirm(
          "Found unsaved changes from a previous session. Would you like to restore them?"
        );
        if (useDraft) {
          setTrainingContent(draft);
        } else {
          localStorage.removeItem(draftKey);
        }
      }
    }
  }, []); // Only run once on mount

  const handleSave = async () => {
    await executeSave(false);
  };

  const handleSaveAndReturn = async () => {
    await executeSave(true);
  };

  const handleAddTag = (tag: TagItem) => {
    if (!topicTags.find((t) => t.id === tag.id)) {
      setTopicTags([...topicTags, tag]);
    }
    setTagInput("");
    setShowTagPopover(false);
  };

  const handleRemoveTag = (tagId: string) => {
    setTopicTags(topicTags.filter((t) => t.id !== tagId));
  };

  const hasChanges =
    topicName !== originalValues.topicName ||
    topicDescription !== originalValues.topicDescription ||
    topicCategoryId !== originalValues.topicCategoryId ||
    trainingContent !== originalValues.trainingContent ||
    contributionNotes !== originalValues.contributionNotes ||
    JSON.stringify(topicTags.map((t) => t.name).sort()) !==
      JSON.stringify(originalValues.topicTags.map((t) => t.name).sort());

  // For new topics, just check if required fields are filled
  // For existing topics, check if anything changed AND fields are valid
  const canSave = (isNewTopic || hasChanges) && 
    topicName.trim() !== "" && 
    topicCategoryId !== "" && 
    trainingContent.trim() !== "" && 
    trainingContent.length <= MAX_TRAINING_LENGTH;

  const filteredTags = allTags.filter(
    (tag) =>
      tag.name.toLowerCase().includes(tagInput.toLowerCase()) &&
      !topicTags.find((t) => t.id === tag.id)
  );

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b bg-gradient-to-r from-primary/5 via-primary/10 to-primary/5">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6">
              <Link href={`/my-personas/${personaId}/training`}>
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
                  <p className="text-muted-foreground mt-1">
                    {isNewTopic ? "Create New Topic" : "Train Topic"}
                  </p>
                </div>
              </div>
            </div>
            <div className="flex gap-2">
              <Button onClick={handleSave} disabled={isSaving || !canSave}>
                {isSaving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="mr-2 h-4 w-4" />
                    Save
                  </>
                )}
              </Button>
              <Button onClick={handleSaveAndReturn} disabled={isSaving || !canSave} variant="outline">
                Save & Return
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto space-y-6">
          {/* Two Column Layout: Topic Details (Left) and Training Content (Right) */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Topic Details Card - Left Side (Smaller) */}
            <div className="lg:col-span-1">
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Topic Details</CardTitle>
                  <CardDescription className="text-sm">Define the topic metadata</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Topic Name */}
                  <div className="space-y-2">
                    <Label htmlFor="topic-name" className="text-sm">
                      Topic Name <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="topic-name"
                      placeholder="e.g., JavaScript Fundamentals"
                      value={topicName}
                      onChange={(e) => setTopicName(e.target.value)}
                    />
                  </div>

                  {/* Topic Description */}
                  <div className="space-y-2">
                    <Label htmlFor="topic-description" className="text-sm">Description (Optional)</Label>
                    <Textarea
                      id="topic-description"
                      placeholder="Brief description..."
                      value={topicDescription}
                      onChange={(e) => setTopicDescription(e.target.value)}
                      rows={2}
                    />
                  </div>

                  {/* Category */}
                  <div className="space-y-2">
                    <Label htmlFor="category" className="text-sm">
                      Category <span className="text-destructive">*</span>
                    </Label>
                    <Select value={topicCategoryId} onValueChange={setTopicCategoryId}>
                      <SelectTrigger>
                        <SelectValue placeholder="Select a category" />
                      </SelectTrigger>
                      <SelectContent>
                        {categories.map((category) => (
                          <SelectItem key={category.id} value={category.id}>
                            {category.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Tags */}
                  <div className="space-y-2">
                    <Label className="text-sm">Tags</Label>
                    <div className="flex flex-wrap gap-2 mb-2">
                      {topicTags.map((tag) => (
                        <Badge key={tag.id} variant="secondary" className="gap-1">
                          {tag.name}
                          <X
                            className="h-3 w-3 cursor-pointer"
                            onClick={() => handleRemoveTag(tag.id)}
                          />
                        </Badge>
                      ))}
                    </div>
                    <Popover open={showTagPopover} onOpenChange={setShowTagPopover}>
                      <PopoverTrigger asChild>
                        <Button variant="outline" size="sm" className="gap-2 w-full">
                          <TagIcon className="h-4 w-4" />
                          Add Tags
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-[300px] p-0" align="start">
                        <div className="flex flex-col">
                          <div className="flex items-center border-b px-3">
                            <Input
                              placeholder="Search tags..."
                              value={tagInput}
                              onChange={(e) => setTagInput(e.target.value)}
                              className="border-0 focus-visible:ring-0 focus-visible:ring-offset-0"
                            />
                          </div>
                          <div className="max-h-[200px] overflow-auto p-1">
                            {filteredTags.length === 0 ? (
                              <div className="py-6 text-center text-sm text-muted-foreground">
                                No tags found.
                              </div>
                            ) : (
                              filteredTags.slice(0, 50).map((tag) => (
                                <button
                                  key={tag.id}
                                  type="button"
                                  onClick={() => handleAddTag(tag)}
                                  className="w-full text-left px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground cursor-pointer transition-colors"
                                >
                                  {tag.name}
                                </button>
                              ))
                            )}
                          </div>
                        </div>
                      </PopoverContent>
                    </Popover>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Training Content Card - Right Side (Main Content) */}
            <div className="lg:col-span-2">
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div>
                      <CardTitle>Training Content</CardTitle>
                      <CardDescription>
                        Teach the persona about this topic. The more detail you provide, the better the
                        responses.
                      </CardDescription>
                    </div>
                    {isSupported && (
                      <VoiceInputToggle
                        isRecording={isRecording}
                        isSupported={isSupported}
                        onToggle={toggleRecording}
                        error={voiceError}
                      />
                    )}
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <Label htmlFor="training-content">
                        Content <span className="text-destructive">*</span>
                      </Label>
                      <span
                        className={`text-xs ${
                          trainingContent.length > MAX_TRAINING_LENGTH
                            ? "text-destructive font-semibold"
                            : "text-muted-foreground"
                        }`}
                      >
                        {trainingContent.length.toLocaleString()} / {MAX_TRAINING_LENGTH.toLocaleString()}{" "}
                        characters
                      </span>
                    </div>
                    <Textarea
                      id="training-content"
                      placeholder="Enter training content here. You can use bullet points, examples, explanations, etc."
                      value={trainingContent}
                      onChange={(e) => setTrainingContent(e.target.value)}
                      onBlur={() => setTrainingContentTouched(true)}
                      rows={20}
                      className="font-mono text-sm"
                      required
                    />
                    {/* Show required error when field is empty and touched */}
                    {trainingContentTouched && !trainingContent.trim() && (
                      <div className="flex items-center gap-2 text-destructive text-sm">
                        <AlertCircle className="h-4 w-4" />
                        Training content is required
                      </div>
                    )}
                    {/* Show length error when exceeding maximum */}
                    {trainingContent.length > MAX_TRAINING_LENGTH && (
                      <div className="flex items-center gap-2 text-destructive text-sm">
                        <AlertCircle className="h-4 w-4" />
                        Content exceeds maximum length. Please reduce by{" "}
                        {(trainingContent.length - MAX_TRAINING_LENGTH).toLocaleString()} characters.
                      </div>
                    )}
                  </div>

                  {/* Contribution Notes */}
                  <div className="space-y-2">
                    <Label htmlFor="contribution-notes">
                      Contribution Notes (Optional)
                    </Label>
                    <Textarea
                      id="contribution-notes"
                      placeholder="Add any notes about the source of this information, why it's relevant, or how it should be used..."
                      value={contributionNotes}
                      onChange={(e) => setContributionNotes(e.target.value)}
                      rows={3}
                    />
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-end gap-2">
            <Link href={`/my-personas/${personaId}/training`}>
              <Button variant="outline">Cancel</Button>
            </Link>
            <Button onClick={handleSave} disabled={isSaving || !canSave}>
              {isSaving ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="mr-2 h-4 w-4" />
                  Save
                </>
              )}
            </Button>
            <Button onClick={handleSaveAndReturn} disabled={isSaving || !canSave}>
              {isSaving ? "Saving..." : "Save & Return to Hub"}
            </Button>
          </div>
        </div>
      </main>
    </div>
  );
}

