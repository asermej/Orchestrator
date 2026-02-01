"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { 
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Header } from "@/components/header";
import { ArrowLeft, Loader2, Save, BookOpen, AlertCircle, Plus, X, Edit, Trash2, BookMarked, Check, AlertTriangle } from "lucide-react";
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
import Link from "next/link";
import { fetchPersonaById } from "../edit/actions";
import { 
  fetchPersonaTopics,
  fetchAllTopics,
  updatePersonaTopicContent,
  removeTopicFromPersona,
  fetchPersonaTopicContent,
  createTopic,
  updateTopic,
  deleteTopic,
  type TopicItem
} from "./actions";
import { PersonaItem } from "../../actions";
import { useVoiceInput } from "@/hooks/use-voice-input";
import { VoiceInputToggle } from "@/components/voice-input-toggle";
import { useServerAction } from "@/lib/use-server-action";

const MAX_TOPIC_TRAINING_LENGTH = 50000;

export default function TrainPersona() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const personaId = params.id as string;

  // Tab state
  const [activeTab, setActiveTab] = useState("persona-topics");

  // Persona state
  const [persona, setPersona] = useState<PersonaItem | null>(null);
  const [isLoadingPersona, setIsLoadingPersona] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Topic training state
  const [personaTopics, setPersonaTopics] = useState<TopicItem[]>([]);
  const [availableTopics, setAvailableTopics] = useState<TopicItem[]>([]);
  const [isLoadingTopics, setIsLoadingTopics] = useState(true);
  const [showAddTopicForm, setShowAddTopicForm] = useState(false);
  const [selectedTopicId, setSelectedTopicId] = useState<string>("");
  const [topicTrainingContent, setTopicTrainingContent] = useState<string>("");
  const [topicContributionNotes, setTopicContributionNotes] = useState<string>("");
  const [editingTopicId, setEditingTopicId] = useState<string | null>(null);

  // Create/Edit topic state (for My Topics tab)
  const [showTopicForm, setShowTopicForm] = useState(false);
  const [editingTopicData, setEditingTopicData] = useState<TopicItem | null>(null);
  const [topicFormName, setTopicFormName] = useState<string>("");
  const [topicFormDescription, setTopicFormDescription] = useState<string>("");
  const [topicFormContent, setTopicFormContent] = useState<string>("");
  const [topicFormContentTouched, setTopicFormContentTouched] = useState(false);

  // Search/filter state for My Topics
  const [topicSearchQuery, setTopicSearchQuery] = useState<string>("");

  // Delete topic dialog state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [topicToDelete, setTopicToDelete] = useState<TopicItem | null>(null);

  // Server actions for mutations
  const { execute: executeUpdatePersonaTopic, isLoading: isSubmittingTopic } = useServerAction(
    async () => {
      if (!editingTopicId) {
        throw new Error("Topics can no longer be added from existing topics. Please create a new topic in the 'My Topics' tab.");
      }
      await updatePersonaTopicContent(
        personaId,
        editingTopicId,
        topicTrainingContent,
        topicContributionNotes
      );
    },
    {
      successMessage: "Topic training saved successfully!",
      onSuccess: async () => {
        handleCancelTopicForm();
        await loadTopics();
      },
    }
  );

  const { execute: executeRemoveTopic, isLoading: isRemovingTopic } = useServerAction(
    async (topicId: string) => {
      await removeTopicFromPersona(personaId, topicId);
    },
    {
      successMessage: "Topic removed successfully!",
      onSuccess: () => loadTopics(),
    }
  );

  const { execute: executeSaveTopic, isLoading: isSubmittingTopicForm } = useServerAction(
    async () => {
      if (!topicFormName.trim()) {
        throw new Error("Topic name is required");
      }
      
      if (!editingTopicData && !topicFormContent.trim()) {
        throw new Error("Training content is required");
      }

      const defaultCategoryId = "00000000-0000-0000-0000-000000000001";

      if (editingTopicData) {
        await updateTopic(
          editingTopicData.id,
          topicFormName.trim(),
          editingTopicData.categoryId || defaultCategoryId,
          editingTopicData.personaId,
          topicFormContent.trim() || undefined,  // Optional content for updates
          topicFormDescription.trim() || undefined,
          editingTopicData.contributionNotes
        );
      } else {
        await createTopic(
          topicFormName.trim(),
          defaultCategoryId,
          personaId,
          topicFormContent.trim(),  // Required training content for new topics
          topicFormDescription.trim() || undefined,
          ""
        );
      }
    },
    {
      successMessage: (data) => `Topic ${editingTopicData ? 'updated' : 'created'} successfully!`,
      onSuccess: async () => {
        await loadTopics();
        handleCancelTopicFormClick();
      },
    }
  );

  const { execute: executeDeleteTopic, isLoading: isDeletingTopic } = useServerAction(
    async () => {
      if (!topicToDelete) {
        throw new Error("No topic selected for deletion");
      }
      await deleteTopic(topicToDelete.id);
    },
    {
      successMessage: "Topic deleted successfully!",
      onSuccess: async () => {
        await loadTopics();
        setDeleteDialogOpen(false);
        setTopicToDelete(null);
      },
    }
  );

  // Voice input for topic training content
  const {
    isRecording,
    isSupported,
    error: voiceError,
    toggleRecording,
  } = useVoiceInput({
    onTranscript: (text) => {
      setTopicTrainingContent((prev) => {
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
      loadTopics();
    }
  }, [user, personaId]);

  const loadPersona = async () => {
    try {
      setIsLoadingPersona(true);
      
      // Load persona details
      const personaData = await fetchPersonaById(personaId);
      setPersona(personaData);
    } catch (err) {
      console.error("Error loading data:", err);
      setError("Failed to load persona data. Please try again.");
    } finally {
      setIsLoadingPersona(false);
    }
  };

  const loadTopics = async () => {
    try {
      setIsLoadingTopics(true);
      
      // Load persona topics and all available topics in parallel
      const [personaTopicsData, allTopicsData] = await Promise.all([
        fetchPersonaTopics(personaId),
        fetchAllTopics()
      ]);
      
      setPersonaTopics(personaTopicsData);
      setAvailableTopics(allTopicsData);
    } catch (err) {
      console.error("Error loading topics:", err);
    } finally {
      setIsLoadingTopics(false);
    }
  };

  // Persona Topics Handlers
  const handleAddTopicClick = () => {
    setShowAddTopicForm(true);
    setEditingTopicId(null);
    setSelectedTopicId("");
    setTopicTrainingContent("");
    setTopicContributionNotes("");
  };

  const handleCancelTopicForm = () => {
    setShowAddTopicForm(false);
    setEditingTopicId(null);
    setSelectedTopicId("");
    setTopicTrainingContent("");
    setTopicContributionNotes("");
  };

  const handleEditPersonaTopic = async (topicId: string) => {
    try {
      const content = await fetchPersonaTopicContent(personaId, topicId);
      const topic = personaTopics.find(t => t.id === topicId);
      
      setEditingTopicId(topicId);
      setSelectedTopicId(topicId);
      setTopicTrainingContent(content.trainingContent || "");
      setTopicContributionNotes(topic?.contributionNotes || "");
      setShowAddTopicForm(true);
    } catch (err) {
      console.error("Error loading topic content:", err);
    }
  };

  const handleDeletePersonaTopic = async (topicId: string) => {
    const topic = availableTopics.find(t => t.id === topicId);
    const topicName = topic?.name || "this topic";
    
    if (!confirm(`Are you sure you want to remove ${topicName} from this persona? This cannot be undone.`)) {
      return;
    }

    await executeRemoveTopic(topicId);
  };

  const handleSubmitPersonaTopic = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    await executeUpdatePersonaTopic();
  };

  // My Topics Handlers
  const handleCreateTopicClick = () => {
    setShowTopicForm(true);
    setEditingTopicData(null);
    setTopicFormName("");
    setTopicFormDescription("");
    setTopicFormContent("");
    setTopicFormContentTouched(false);
  };

  const handleEditTopicClick = (topic: TopicItem) => {
    setShowTopicForm(true);
    setEditingTopicData(topic);
    setTopicFormName(topic.name);
    setTopicFormDescription(topic.description || "");
    setTopicFormContent(""); // Clear content for editing - will show as optional
    setTopicFormContentTouched(false);
  };

  const handleCancelTopicFormClick = () => {
    setShowTopicForm(false);
    setEditingTopicData(null);
    setTopicFormName("");
    setTopicFormDescription("");
    setTopicFormContent("");
    setTopicFormContentTouched(false);
  };

  const handleSubmitTopicForm = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    
    // Prevent submission if validation fails
    if (!topicFormName.trim() || (!editingTopicData && !topicFormContent.trim()) || topicFormContent.length > MAX_TOPIC_TRAINING_LENGTH) {
      return;
    }
    
    await executeSaveTopic();
  };

  const handleDeleteTopicClick = (topic: TopicItem) => {
    setTopicToDelete(topic);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await executeDeleteTopic();
  };

  const handleAddTopicToPersonaClick = async (topicId: string) => {
    // Switch to Persona Topics tab and open the add form with this topic selected
    setActiveTab("persona-topics");
    setShowAddTopicForm(true);
    setEditingTopicId(null);
    setSelectedTopicId(topicId);
    setTopicTrainingContent("");
    setTopicContributionNotes("");
  };

  // Character count calculations for topic training
  const topicCharacterCount = topicTrainingContent.length;
  const isTopicNearLimit = topicCharacterCount > MAX_TOPIC_TRAINING_LENGTH * 0.9;
  const isTopicOverLimit = topicCharacterCount > MAX_TOPIC_TRAINING_LENGTH;

  // Get topics that haven't been added to this persona yet
  const unaddedTopics = availableTopics.filter(
    topic => !personaTopics.some(pt => pt.id === topic.id)
  );

  // Filter topics for My Topics tab
  const filteredTopics = availableTopics.filter(topic => 
    topic.name.toLowerCase().includes(topicSearchQuery.toLowerCase()) ||
    (topic.description && topic.description.toLowerCase().includes(topicSearchQuery.toLowerCase()))
  );

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
            <Link href={`/personas/${personaId}/edit`}>
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div>
              <h1 className="text-2xl font-semibold">Add Topic Knowledge</h1>
              <p className="text-sm text-muted-foreground mt-1">
                Add specific topics {persona.displayName} is knowledgeable about
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="persona-topics">Persona Topics</TabsTrigger>
              <TabsTrigger value="my-topics">My Topics</TabsTrigger>
            </TabsList>

            {/* Persona Topics Tab */}
            <TabsContent value="persona-topics" className="space-y-4">
              <Card>
                <CardHeader>
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <BookMarked className="h-6 w-6 text-primary mt-1" />
                      <div className="flex-1">
                        <CardTitle>Persona Topics</CardTitle>
                        <CardDescription className="mt-2">
                          Train {persona.displayName} on specific topics that can be selectively loaded into conversations. 
                          Each topic can contain up to 50,000 characters of detailed knowledge.
                        </CardDescription>
                      </div>
                    </div>
                    {!showAddTopicForm && unaddedTopics.length > 0 && (
                      <Button onClick={handleAddTopicClick} size="sm">
                        <Plus className="h-4 w-4 mr-2" />
                        Add Topic
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent className="space-y-6">
                  {isLoadingTopics ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin" />
                    </div>
                  ) : (
                    <>
                      {/* Add/Edit Topic Form */}
                      {showAddTopicForm && (
                        <form onSubmit={handleSubmitPersonaTopic} className="space-y-4 border rounded-lg p-4 bg-muted/20">
                          <div className="flex items-center justify-between">
                            <h3 className="font-semibold">
                              {editingTopicId ? "Edit Topic Training" : "Add Topic to Persona"}
                            </h3>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              onClick={handleCancelTopicForm}
                              disabled={isSubmittingTopic}
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          </div>

                          {/* Topic Selection */}
                          {!editingTopicId && (
                            <div className="space-y-2">
                              <Label htmlFor="topicId">Select Topic</Label>
                              <Select value={selectedTopicId} onValueChange={setSelectedTopicId}>
                                <SelectTrigger>
                                  <SelectValue placeholder="Choose a topic..." />
                                </SelectTrigger>
                                <SelectContent>
                                  {unaddedTopics.map((topic) => (
                                    <SelectItem key={topic.id} value={topic.id}>
                                      {topic.name}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            </div>
                          )}

                          {/* Training Content */}
                          <div className="space-y-2">
                            <div className="flex items-center justify-between">
                              <Label htmlFor="topicTrainingContent">Training Content</Label>
                              <VoiceInputToggle
                                isRecording={isRecording}
                                isSupported={isSupported}
                                error={voiceError}
                                onToggle={toggleRecording}
                                disabled={isSubmittingTopic}
                              />
                            </div>
                            <Textarea
                              id="topicTrainingContent"
                              placeholder="Enter detailed knowledge, facts, and information about this topic..."
                              value={topicTrainingContent}
                              onChange={(e) => setTopicTrainingContent(e.target.value)}
                              disabled={isSubmittingTopic}
                              rows={12}
                              className="resize-none font-mono text-sm"
                            />
                            <div className="flex items-center justify-between text-sm">
                              <p className="text-muted-foreground">
                                Maximum {MAX_TOPIC_TRAINING_LENGTH.toLocaleString()} characters (~12,500 tokens)
                              </p>
                              <p className={`font-medium ${
                                isTopicOverLimit 
                                  ? "text-destructive" 
                                  : isTopicNearLimit 
                                    ? "text-yellow-600 dark:text-yellow-400" 
                                    : "text-muted-foreground"
                              }`}>
                                {topicCharacterCount.toLocaleString()} / {MAX_TOPIC_TRAINING_LENGTH.toLocaleString()}
                                {isTopicOverLimit && " (over limit)"}
                              </p>
                            </div>
                          </div>

                          {/* Contribution Notes */}
                          <div className="space-y-2">
                            <Label htmlFor="contributionNotes">
                              Notes <span className="text-muted-foreground">(optional)</span>
                            </Label>
                            <Input
                              id="contributionNotes"
                              placeholder="Add notes about this training..."
                              value={topicContributionNotes}
                              onChange={(e) => setTopicContributionNotes(e.target.value)}
                              disabled={isSubmittingTopic}
                            />
                          </div>

                          {/* Form Actions */}
                          <div className="flex gap-2 justify-end">
                            <Button
                              type="button"
                              variant="outline"
                              onClick={handleCancelTopicForm}
                              disabled={isSubmittingTopic}
                            >
                              Cancel
                            </Button>
                            <Button 
                              type="submit" 
                              disabled={isSubmittingTopic || isTopicOverLimit || (!editingTopicId && !selectedTopicId)}
                            >
                              {isSubmittingTopic ? (
                                <>
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                  Saving...
                                </>
                              ) : (
                                <>
                                  <Save className="mr-2 h-4 w-4" />
                                  {editingTopicId ? "Update Topic" : "Add Topic"}
                                </>
                              )}
                            </Button>
                          </div>
                        </form>
                      )}

                      {/* Existing Topics List */}
                      {personaTopics.length === 0 ? (
                        <div className="text-center py-12 text-muted-foreground">
                          <BookMarked className="h-12 w-12 mx-auto mb-4 opacity-50" />
                          <p className="mb-2">No topics added yet</p>
                          <p className="text-sm">
                            Add topics to train this persona with specialized knowledge
                          </p>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {personaTopics.map((topic) => {
                            return (
                              <div
                                key={topic.id}
                                className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                              >
                                <div className="flex-1">
                                  <h4 className="font-medium">{topic.name}</h4>
                                  {topic.contributionNotes && (
                                    <p className="text-sm text-muted-foreground mt-1">
                                      {topic.contributionNotes}
                                    </p>
                                  )}
                                </div>
                                <div className="flex gap-2">
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => handleEditPersonaTopic(topic.id)}
                                    disabled={isSubmittingTopic}
                                  >
                                    <Edit className="h-4 w-4" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => handleDeletePersonaTopic(topic.id)}
                                    disabled={isSubmittingTopic}
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      )}

                      {/* Topic Training Tips */}
                      <div className="bg-muted/50 rounded-lg p-4 space-y-2">
                        <h3 className="font-semibold text-sm">💡 Topic Training Tips</h3>
                        <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside">
                          <li>Topics allow for 10x more content than general training (50,000 vs 5,000 chars)</li>
                          <li>Use topics for specialized domains like &quot;Medical Knowledge&quot; or &quot;Historical Events&quot;</li>
                          <li>Topics can be selectively included in conversations based on context</li>
                          <li>Break large knowledge bases into multiple focused topics</li>
                          <li>Keep each topic focused on a single domain or subject area</li>
                        </ul>
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* My Topics Tab */}
            <TabsContent value="my-topics" className="space-y-4">
              <Card>
                <CardHeader>
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <BookMarked className="h-6 w-6 text-primary mt-1" />
                      <div className="flex-1">
                        <CardTitle>My Topics</CardTitle>
                        <CardDescription className="mt-2">
                          Manage all your topics. Create new topics, edit existing ones, or delete topics you no longer need.
                        </CardDescription>
                      </div>
                    </div>
                    {!showTopicForm && (
                      <Button onClick={handleCreateTopicClick} size="sm">
                        <Plus className="h-4 w-4 mr-2" />
                        Create Topic
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent className="space-y-6">
                  {isLoadingTopics ? (
                    <div className="flex items-center justify-center py-12">
                      <Loader2 className="h-8 w-8 animate-spin" />
                    </div>
                  ) : (
                    <>
                      {/* Create/Edit Topic Form */}
                      {showTopicForm && (
                        <form onSubmit={handleSubmitTopicForm} className="space-y-4 border rounded-lg p-4 bg-muted/20">
                          <div className="flex items-center justify-between">
                            <h3 className="font-semibold">
                              {editingTopicData ? "Edit Topic" : "Create New Topic"}
                            </h3>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              onClick={handleCancelTopicFormClick}
                              disabled={isSubmittingTopicForm}
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          </div>

                          {/* Topic Name */}
                          <div className="space-y-2">
                            <Label htmlFor="topicFormName">
                              Topic Name <span className="text-red-500">*</span>
                            </Label>
                            <Input
                              id="topicFormName"
                              placeholder="e.g., Star Wars Lore, Medical Knowledge, Historical Facts"
                              value={topicFormName}
                              onChange={(e) => setTopicFormName(e.target.value)}
                              disabled={isSubmittingTopicForm}
                              required
                              maxLength={200}
                            />
                            <p className="text-xs text-muted-foreground">
                              Maximum 200 characters
                            </p>
                          </div>

                          {/* Topic Description */}
                          <div className="space-y-2">
                            <Label htmlFor="topicFormDescription">
                              Description <span className="text-muted-foreground">(optional)</span>
                            </Label>
                            <Textarea
                              id="topicFormDescription"
                              placeholder="Describe what this topic covers..."
                              value={topicFormDescription}
                              onChange={(e) => setTopicFormDescription(e.target.value)}
                              disabled={isSubmittingTopicForm}
                              rows={3}
                              maxLength={1000}
                            />
                            <p className="text-xs text-muted-foreground">
                              Maximum 1000 characters
                            </p>
                          </div>

                          {/* Training Content */}
                          <div className="space-y-2">
                            <Label htmlFor="topicFormContent">
                              Training Content {!editingTopicData && <span className="text-red-500">*</span>}
                              {editingTopicData && <span className="text-muted-foreground">(optional - leave empty to keep existing)</span>}
                            </Label>
                            <Textarea
                              id="topicFormContent"
                              placeholder="Enter training content here. You can use bullet points, examples, explanations, etc."
                              value={topicFormContent}
                              onChange={(e) => setTopicFormContent(e.target.value)}
                              onBlur={() => setTopicFormContentTouched(true)}
                              disabled={isSubmittingTopicForm}
                              rows={10}
                              maxLength={MAX_TOPIC_TRAINING_LENGTH}
                              className="font-mono text-sm"
                              required={!editingTopicData}
                            />
                            <div className="flex items-center justify-between text-xs">
                              <span className="text-muted-foreground">
                                Maximum {MAX_TOPIC_TRAINING_LENGTH.toLocaleString()} characters
                              </span>
                              <span className={topicFormContent.length > MAX_TOPIC_TRAINING_LENGTH ? "text-destructive font-semibold" : "text-muted-foreground"}>
                                {topicFormContent.length.toLocaleString()} / {MAX_TOPIC_TRAINING_LENGTH.toLocaleString()}
                              </span>
                            </div>
                            {/* Show required error when creating and field is empty and touched */}
                            {!editingTopicData && topicFormContentTouched && !topicFormContent.trim() && (
                              <div className="flex items-center gap-2 text-destructive text-sm">
                                <AlertCircle className="h-4 w-4" />
                                Training content is required
                              </div>
                            )}
                            {/* Show length error when exceeding maximum */}
                            {topicFormContent.length > MAX_TOPIC_TRAINING_LENGTH && (
                              <div className="flex items-center gap-2 text-destructive text-sm">
                                <AlertCircle className="h-4 w-4" />
                                Content exceeds maximum length
                              </div>
                            )}
                          </div>

                          {/* Form Actions */}
                          <div className="flex gap-2 justify-end">
                            <Button
                              type="button"
                              variant="outline"
                              onClick={handleCancelTopicFormClick}
                              disabled={isSubmittingTopicForm}
                            >
                              Cancel
                            </Button>
                            <Button 
                              type="submit" 
                              disabled={isSubmittingTopicForm || !topicFormName.trim() || (!editingTopicData && !topicFormContent.trim()) || topicFormContent.length > MAX_TOPIC_TRAINING_LENGTH}
                            >
                              {isSubmittingTopicForm ? (
                                <>
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                  {editingTopicData ? "Updating..." : "Creating..."}
                                </>
                              ) : (
                                <>
                                  <Save className="mr-2 h-4 w-4" />
                                  {editingTopicData ? "Update Topic" : "Create Topic"}
                                </>
                              )}
                            </Button>
                          </div>
                        </form>
                      )}

                      {/* Search Bar */}
                      {!showTopicForm && availableTopics.length > 0 && (
                        <div className="space-y-2">
                          <Input
                            placeholder="Search topics..."
                            value={topicSearchQuery}
                            onChange={(e) => setTopicSearchQuery(e.target.value)}
                            className="max-w-md"
                          />
                        </div>
                      )}

                      {/* Topics List */}
                      {availableTopics.length === 0 ? (
                        <div className="text-center py-12 text-muted-foreground">
                          <BookMarked className="h-12 w-12 mx-auto mb-4 opacity-50" />
                          <p className="mb-2">No topics created yet</p>
                          <p className="text-sm">
                            Create your first topic to get started
                          </p>
                        </div>
                      ) : filteredTopics.length === 0 ? (
                        <div className="text-center py-12 text-muted-foreground">
                          <p>No topics found matching &quot;{topicSearchQuery}&quot;</p>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {filteredTopics.map((topic) => {
                            const isUsedInPersona = personaTopics.some(pt => pt.id === topic.id);
                            return (
                              <div
                                key={topic.id}
                                className="flex items-start justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                              >
                                <div className="flex-1 min-w-0 pr-4">
                                  <div className="flex items-center gap-2">
                                    <h4 className="font-medium">{topic.name}</h4>
                                    {isUsedInPersona && (
                                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs">
                                        <Check className="h-3 w-3" />
                                        On Persona
                                      </span>
                                    )}
                                  </div>
                                  {topic.description && (
                                    <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                                      {topic.description}
                                    </p>
                                  )}
                                </div>
                                <div className="flex gap-2 flex-shrink-0">
                                  {!isUsedInPersona && (
                                    <Button
                                      variant="outline"
                                      size="sm"
                                      onClick={() => handleAddTopicToPersonaClick(topic.id)}
                                      disabled={isSubmittingTopicForm}
                                    >
                                      <Plus className="h-4 w-4 mr-1" />
                                      Add
                                    </Button>
                                  )}
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => handleEditTopicClick(topic)}
                                    disabled={isSubmittingTopicForm}
                                  >
                                    <Edit className="h-4 w-4" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => handleDeleteTopicClick(topic)}
                                    disabled={isSubmittingTopicForm}
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      )}
                    </>
                  )}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>
      </main>

      {/* Delete Topic Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-start gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-destructive/10">
                <AlertTriangle className="h-6 w-6 text-destructive" />
              </div>
              <div className="flex-1">
              <AlertDialogTitle className="text-xl">Delete Topic?</AlertDialogTitle>
              <AlertDialogDescription className="mt-2 text-base">
                  {topicToDelete && personaTopics.some(pt => pt.id === topicToDelete.id) ? (
                    <>
                      <span className="font-semibold text-foreground">&quot;{topicToDelete.name}&quot;</span> is currently used by this persona.
                      <br />
                      <br />
                      Deleting it will remove it from <span className="font-semibold">all personas</span> and <span className="font-semibold text-destructive">cannot be undone</span>.
                    </>
                  ) : (
                    <>
                      Are you sure you want to delete <span className="font-semibold text-foreground">&quot;{topicToDelete?.name}&quot;</span>?
                      <br />
                      <br />
                      This action <span className="font-semibold text-destructive">cannot be undone</span>.
                    </>
                  )}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <AlertDialogFooter className="mt-4">
            <AlertDialogCancel disabled={isDeletingTopic}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              disabled={isDeletingTopic}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeletingTopic ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                <>
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete Topic
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
