"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { 
  Loader2, 
  BookMarked, 
  Plus, 
  Search, 
  Grid3x3, 
  List,
  BookOpen,
  Trash2,
  GraduationCap,
  X,
  Tag as TagIcon
} from "lucide-react";
import Link from "next/link";
import { PersonaAvatar } from "@/components/persona-avatar";
import {
  removeTopicFromPersona,
  type TopicWithDetails,
  type CategoryItem,
  type TagItem,
} from "./actions";
import { useServerAction } from "@/lib/use-server-action";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
import { Badge } from "@/components/ui/badge";

type ViewMode = "grid" | "list";

interface TrainingHubClientProps {
  personaId: string;
  displayName: string;
  profileImageUrl: string;
  initialTopics: TopicWithDetails[];
  categories: CategoryItem[];
  allTags: TagItem[];
  isOnboarding: boolean;
}

export function TrainingHubClient({
  personaId,
  displayName,
  profileImageUrl,
  initialTopics,
  categories,
  allTags,
  isOnboarding,
}: TrainingHubClientProps) {
  const router = useRouter();

  // Topics and data
  const [topics, setTopics] = useState<TopicWithDetails[]>(initialTopics);

  // Filters and view
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>("all");
  const [selectedTagIds, setSelectedTagIds] = useState<string[]>([]);
  const [tagFilterInput, setTagFilterInput] = useState("");
  const [viewMode, setViewMode] = useState<ViewMode>(() => {
    // Load view preference from localStorage
    if (typeof window !== "undefined") {
      const savedView = localStorage.getItem("training-view-mode");
      if (savedView === "grid" || savedView === "list") {
        return savedView;
      }
    }
    return "grid";
  });

  // Delete confirmation
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [topicToDelete, setTopicToDelete] = useState<TopicWithDetails | null>(null);

  // Server action for deleting topic
  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    async () => {
      if (!topicToDelete) {
        throw new Error("No topic selected for deletion");
      }
      await removeTopicFromPersona(personaId, topicToDelete.id);
      return topicToDelete.id;
    },
    {
      successMessage: "Topic removed successfully!",
      onSuccess: (deletedTopicId) => {
        setTopics(topics.filter((t) => t.id !== deletedTopicId));
        setDeleteDialogOpen(false);
        setTopicToDelete(null);
      },
    }
  );

  const handleViewModeChange = (mode: ViewMode) => {
    setViewMode(mode);
    if (typeof window !== "undefined") {
      localStorage.setItem("training-view-mode", mode);
    }
  };

  const handleDeleteClick = (topic: TopicWithDetails) => {
    setTopicToDelete(topic);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await executeDelete();
  };

  // Filter topics
  const filteredTopics = topics.filter((topic) => {
    const matchesSearch = topic.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = selectedCategoryId === "all" || topic.categoryId === selectedCategoryId;
    const matchesTags =
      selectedTagIds.length === 0 ||
      selectedTagIds.some((tagId) => topic.tags.some((t) => t.id === tagId));
    return matchesSearch && matchesCategory && matchesTags;
  });

  // Get category badge color
  const getCategoryColor = (categoryId: string) => {
    const category = categories.find((c) => c.id === categoryId);
    if (!category) return "bg-gray-500";
    
    const colors = [
      "bg-blue-500",
      "bg-green-500",
      "bg-purple-500",
      "bg-orange-500",
      "bg-pink-500",
      "bg-teal-500",
      "bg-indigo-500",
      "bg-rose-500",
    ];
    
    const index = categories.indexOf(category) % colors.length;
    return colors[index];
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Hero Header */}
      <div className="border-b bg-gradient-to-r from-primary/5 via-primary/10 to-primary/5">
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6">
              <Link href="/my-personas">
                <Button variant="ghost" size="icon" className="rounded-full">
                  <X className="h-5 w-5" />
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
                  <p className="text-muted-foreground mt-1">Topic Training Hub</p>
                </div>
              </div>
            </div>
            <div className="flex gap-2">
              <Link href={`/my-personas/${personaId}/general-training`}>
                <Button variant="outline" className="gap-2">
                  <BookOpen className="h-4 w-4" />
                  General Training
                </Button>
              </Link>
              <Link href={`/my-personas/${personaId}/edit`}>
                <Button variant="outline" className="gap-2">
                  Edit Profile
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto space-y-6">
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
                    <div className="w-8 h-8 rounded-full bg-green-500 text-white flex items-center justify-center font-semibold">
                      ✓
                    </div>
                    <span className="text-muted-foreground">General Training</span>
                  </div>
                  <div className="flex-1 h-px bg-border" />
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-semibold">
                      3
                    </div>
                    <span className="font-medium">Topics</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Toolbar */}
          <div className="flex items-center justify-between gap-4">
            <div className="text-sm text-muted-foreground">
              {filteredTopics.length} {filteredTopics.length === 1 ? "topic" : "topics"}
              {searchTerm || selectedCategoryId !== "all" || selectedTagIds.length > 0 ? " (filtered)" : ""}
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant={viewMode === "grid" ? "default" : "outline"}
                size="sm"
                onClick={() => handleViewModeChange("grid")}
                title="Grid view"
              >
                <Grid3x3 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === "list" ? "default" : "outline"}
                size="sm"
                onClick={() => handleViewModeChange("list")}
                title="List view"
              >
                <List className="h-4 w-4" />
              </Button>
              <Link href={`/my-personas/${personaId}/topics/new/train`}>
                <Button>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Topic
                </Button>
              </Link>
            </div>
          </div>

          {/* Search and Filters */}
          <Card>
            <CardContent className="p-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {/* Search */}
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search topics..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10 pr-8"
                  />
                  {searchTerm && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="absolute right-1 top-1/2 transform -translate-y-1/2 h-7 w-7"
                      onClick={() => setSearchTerm("")}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>

                {/* Category Filter */}
                <Select value={selectedCategoryId} onValueChange={setSelectedCategoryId}>
                  <SelectTrigger>
                    <div className="flex items-center gap-2">
                      <BookMarked className="h-4 w-4" />
                      <SelectValue placeholder="All Categories" />
                    </div>
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Categories ({topics.length})</SelectItem>
                    {categories.map((category) => {
                      const count = topics.filter((t) => t.categoryId === category.id).length;
                      return (
                        <SelectItem key={category.id} value={category.id}>
                          {category.name} ({count})
                        </SelectItem>
                      );
                    })}
                  </SelectContent>
                </Select>

                {/* Tag Filter with Autocomplete */}
                <div className="relative">
                  <TagIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Filter by tags..."
                    value={tagFilterInput}
                    onChange={(e) => setTagFilterInput(e.target.value)}
                    className="pl-10"
                  />
                  {tagFilterInput && (
                    <div className="absolute z-10 w-full mt-1 bg-background border rounded-md shadow-lg max-h-60 overflow-auto">
                      {allTags
                        .filter((tag) =>
                          tag.name.toLowerCase().includes(tagFilterInput.toLowerCase())
                        )
                        .slice(0, 10)
                        .map((tag) => {
                          const isSelected = selectedTagIds.includes(tag.id);
                          return (
                            <div
                              key={tag.id}
                              className="px-3 py-2 hover:bg-muted cursor-pointer flex items-center justify-between"
                              onClick={() => {
                                if (isSelected) {
                                  setSelectedTagIds(selectedTagIds.filter((id) => id !== tag.id));
                                } else {
                                  setSelectedTagIds([...selectedTagIds, tag.id]);
                                }
                                setTagFilterInput("");
                              }}
                            >
                              <span className="text-sm">{tag.name}</span>
                              {isSelected && (
                                <Badge variant="default" className="text-xs">
                                  Selected
                                </Badge>
                              )}
                            </div>
                          );
                        })}
                      {allTags.filter((tag) =>
                          tag.name.toLowerCase().includes(tagFilterInput.toLowerCase())
                        ).length === 0 && (
                        <div className="px-3 py-2 text-sm text-muted-foreground">
                          No tags found
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Active Filters */}
              {(selectedCategoryId !== "all" || selectedTagIds.length > 0) && (
                <div className="flex items-center gap-2 mt-3 pt-3 border-t">
                  <span className="text-xs text-muted-foreground">Active filters:</span>
                  {selectedCategoryId !== "all" && (
                    <Badge
                      variant="secondary"
                      className="gap-1 cursor-pointer"
                      onClick={() => setSelectedCategoryId("all")}
                    >
                      {categories.find((c) => c.id === selectedCategoryId)?.name}
                      <X className="h-3 w-3" />
                    </Badge>
                  )}
                  {selectedTagIds.map((tagId) => {
                    const tag = allTags.find((t) => t.id === tagId);
                    if (!tag) return null;
                    return (
                      <Badge
                        key={tagId}
                        variant="secondary"
                        className="gap-1 cursor-pointer"
                        onClick={() =>
                          setSelectedTagIds(selectedTagIds.filter((id) => id !== tagId))
                        }
                      >
                        {tag.name}
                        <X className="h-3 w-3" />
                      </Badge>
                    );
                  })}
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setSearchTerm("");
                      setSelectedCategoryId("all");
                      setSelectedTagIds([]);
                      setTagFilterInput("");
                    }}
                    className="h-6 text-xs"
                  >
                    Clear all
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Topics Display */}
          {filteredTopics.length === 0 ? (
            <Card>
              <CardContent className="text-center py-20">
                <BookMarked className="h-16 w-16 mx-auto mb-4 opacity-50" />
                <h3 className="text-xl font-semibold mb-2">
                  {searchTerm || selectedCategoryId !== "all" || selectedTagIds.length > 0
                    ? "No topics match your filters"
                    : "No topics yet"}
                </h3>
                <p className="text-muted-foreground mb-6">
                  {searchTerm || selectedCategoryId !== "all" || selectedTagIds.length > 0
                    ? "Try adjusting your search or filters"
                    : "Create your first topic to start training this persona"}
                </p>
                {!searchTerm && selectedCategoryId === "all" && selectedTagIds.length === 0 && (
                  <Link href={`/my-personas/${personaId}/topics/new/train`}>
                    <Button size="lg">
                      <Plus className="h-5 w-5 mr-2" />
                      Create Your First Topic
                    </Button>
                  </Link>
                )}
              </CardContent>
            </Card>
          ) : viewMode === "grid" ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {filteredTopics.map((topic) => (
                <Card key={topic.id} className="hover:shadow-lg transition-shadow flex flex-col h-full">
                  <CardHeader className="pb-3">
                    <div className="space-y-2">
                      <CardTitle className="text-lg line-clamp-2">{topic.name}</CardTitle>
                      {topic.description && (
                        <CardDescription className="line-clamp-2">
                          {topic.description}
                        </CardDescription>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="flex flex-col flex-1 space-y-4">
                    <div className="space-y-3 flex-1">
                      <div>
                        <Badge className={`${getCategoryColor(topic.categoryId)} text-white`}>
                          {topic.categoryName}
                        </Badge>
                      </div>
                      {topic.tags.length > 0 ? (
                        <div className="space-y-1">
                          <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <TagIcon className="h-3 w-3" />
                            <span>{topic.tags.length} {topic.tags.length === 1 ? "tag" : "tags"}</span>
                          </div>
                          <div className="flex gap-1 flex-wrap">
                            {topic.tags.slice(0, 5).map((tag) => (
                              <Badge key={tag.id} variant="secondary" className="text-xs">
                                {tag.name}
                              </Badge>
                            ))}
                            {topic.tags.length > 5 && (
                              <Badge variant="secondary" className="text-xs">
                                +{topic.tags.length - 5} more
                              </Badge>
                            )}
                          </div>
                        </div>
                      ) : (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <TagIcon className="h-3 w-3" />
                          <span>No tags</span>
                        </div>
                      )}
                    </div>

                    <div className="space-y-3 mt-auto">
                      <div className="text-xs text-muted-foreground">
                        Updated {new Date(topic.updatedAt).toLocaleDateString()}
                      </div>
                      
                      <div className="flex gap-2 justify-end">
                        <Link
                          href={`/my-personas/${personaId}/topics/${topic.id}/train`}
                        >
                          <Button size="sm">
                            <GraduationCap className="mr-2 h-4 w-4" />
                            Train
                          </Button>
                        </Link>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDeleteClick(topic)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <Card>
              <CardContent className="p-0">
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead className="border-b">
                      <tr className="text-left">
                        <th className="p-4 font-medium">Topic</th>
                        <th className="p-4 font-medium">Category</th>
                        <th className="p-4 font-medium">Tags</th>
                        <th className="p-4 font-medium">Updated</th>
                        <th className="p-4 font-medium">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredTopics.map((topic) => (
                        <tr key={topic.id} className="border-b hover:bg-muted/50">
                          <td className="p-4">
                            <div>
                              <div className="font-medium">{topic.name}</div>
                              {topic.description && (
                                <div className="text-sm text-muted-foreground line-clamp-1">
                                  {topic.description}
                                </div>
                              )}
                            </div>
                          </td>
                          <td className="p-4">
                            <Badge className={`${getCategoryColor(topic.categoryId)} text-white`}>
                              {topic.categoryName}
                            </Badge>
                          </td>
                          <td className="p-4">
                            {topic.tags.length > 0 ? (
                              <div className="space-y-1">
                                <div className="text-xs text-muted-foreground">
                                  {topic.tags.length} {topic.tags.length === 1 ? "tag" : "tags"}
                                </div>
                                <div className="flex gap-1 flex-wrap">
                                  {topic.tags.slice(0, 3).map((tag) => (
                                    <Badge key={tag.id} variant="secondary" className="text-xs">
                                      {tag.name}
                                    </Badge>
                                  ))}
                                  {topic.tags.length > 3 && (
                                    <Badge variant="secondary" className="text-xs">
                                      +{topic.tags.length - 3}
                                    </Badge>
                                  )}
                                </div>
                              </div>
                            ) : (
                              <span className="text-sm text-muted-foreground">No tags</span>
                            )}
                          </td>
                          <td className="p-4 text-sm text-muted-foreground">
                            {new Date(topic.updatedAt).toLocaleDateString()}
                          </td>
                          <td className="p-4">
                            <div className="flex gap-2">
                              <Link href={`/my-personas/${personaId}/topics/${topic.id}/train`}>
                                <Button variant="default" size="sm">
                                  <GraduationCap className="mr-2 h-4 w-4" />
                                  Train
                                </Button>
                              </Link>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleDeleteClick(topic)}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
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
            <AlertDialogTitle>Remove Topic from Persona?</AlertDialogTitle>
            <AlertDialogDescription>
              This will remove "{topicToDelete?.name}" from {displayName}. The topic itself will
              not be deleted, but all training content for this persona will be removed. This action
              cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Removing...
                </>
              ) : (
                "Remove"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

