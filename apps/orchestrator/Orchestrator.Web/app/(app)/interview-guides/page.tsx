"use client";

import { useEffect, useState, useCallback } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Search, Loader2, BookOpen, Plus, MessageSquare, Copy, Building2, MapPin } from "lucide-react";
import Link from "next/link";
import { fetchInterviewGuides, cloneInterviewGuide, InterviewGuideItem } from "./actions";
import { toast } from "sonner";

function getCookie(name: string): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

function visibilityScopeLabel(scope?: string): string | null {
  switch (scope) {
    case "organization_and_descendants":
      return "Shared with sub-orgs";
    case "descendants_only":
      return "Sub-orgs only";
    default:
      return null;
  }
}

function visibilityScopeBadgeVariant(
  scope?: string
): "default" | "secondary" | "outline" {
  switch (scope) {
    case "organization_and_descendants":
      return "default";
    case "descendants_only":
      return "secondary";
    default:
      return "outline";
  }
}

export default function InterviewGuidesPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [activeTab, setActiveTab] = useState<"local" | "inherited">("local");
  const [guides, setGuides] = useState<InterviewGuideItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(12);
  const [error, setError] = useState<string | null>(null);
  const [cloningId, setCloningId] = useState<string | null>(null);

  const selectedOrgId = getCookie("orchestrator_selected_org");
  const hasOrgSelected = !!selectedOrgId;
  const [trackedOrgId, setTrackedOrgId] = useState(selectedOrgId);
  useEffect(() => {
    if (selectedOrgId !== trackedOrgId) {
      setTrackedOrgId(selectedOrgId);
    }
  }, [selectedOrgId, trackedOrgId]);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  const loadGuides = useCallback(async () => {
    if (!trackedOrgId) {
      setGuides([]);
      setTotalCount(0);
      setIsLoading(false);
      return;
    }
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchInterviewGuides(
        currentPage,
        pageSize,
        searchTerm,
        undefined,
        activeTab
      );
      setGuides(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error("Error loading interview guides:", err);
      setError("Failed to load interview guides. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, pageSize, searchTerm, activeTab, trackedOrgId]);

  useEffect(() => {
    if (user && trackedOrgId) {
      loadGuides();
    }
  }, [user, loadGuides, trackedOrgId]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
  };

  const handleTabChange = (tab: string) => {
    setActiveTab(tab as "local" | "inherited");
    setCurrentPage(1);
    setSearchTerm("");
  };

  const handleClone = async (guideId: string) => {
    try {
      setCloningId(guideId);
      const cloned = await cloneInterviewGuide(guideId);
      toast.success("Interview guide cloned successfully!");
      router.push(`/interview-guides/${cloned.id}`);
    } catch (err) {
      console.error("Error cloning interview guide:", err);
      toast.error("Failed to clone interview guide. Please try again.");
    } finally {
      setCloningId(null);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

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

  // Org required gate
  if (!hasOrgSelected) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold">Interview Guides</h1>
          <p className="text-muted-foreground mt-2">
            Create reusable question sets with opening/closing templates and scoring rubrics
          </p>
        </div>
        <div className="text-center py-20">
          <Building2 className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-xl font-semibold mb-2">
            Select an organization
          </h3>
          <p className="text-muted-foreground max-w-md mx-auto">
            Please select an organization from the dropdown in the top-right
            corner to manage interview guides. Guides are scoped to specific
            organizations.
          </p>
        </div>
      </div>
    );
  }

  const renderGuideGrid = (guideList: InterviewGuideItem[], isInherited: boolean) => {
    if (isLoading) {
      return (
        <div className="flex justify-center items-center py-20">
          <Loader2 className="h-12 w-12 animate-spin text-primary" />
        </div>
      );
    }

    if (guideList.length === 0) {
      return (
        <div className="text-center py-20">
          <BookOpen className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-xl font-semibold mb-2">
            {isInherited ? "No inherited guides" : "No guides yet"}
          </h3>
          <p className="text-muted-foreground mb-6">
            {searchTerm
              ? "No guides match your search"
              : isInherited
                ? "No guides have been shared from parent organizations"
                : "Create your first interview guide to get started!"}
          </p>
          {!searchTerm && !isInherited && (
            <Link href="/interview-guides/new">
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                Create Your First Guide
              </Button>
            </Link>
          )}
        </div>
      );
    }

    return (
      <>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {guideList.map((guide) => (
            <Card key={guide.id} className="overflow-hidden hover:shadow-lg transition-shadow relative">
              {!isInherited ? (
                <Link href={`/interview-guides/${guide.id}`}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 bg-primary/10 rounded-lg">
                          <BookOpen className="h-6 w-6 text-primary" />
                        </div>
                        <div>
                          <CardTitle className="text-lg">{guide.name}</CardTitle>
                        </div>
                      </div>
                      <Badge variant={guide.isActive ? "default" : "secondary"}>
                        {guide.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </div>
                    {guide.description && (
                      <CardDescription className="line-clamp-2 mt-2">
                        {guide.description}
                      </CardDescription>
                    )}
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-center gap-4 text-sm text-muted-foreground flex-wrap">
                      <div className="flex items-center gap-1">
                        <MessageSquare className="h-4 w-4" />
                        <span>{guide.questionCount || guide.questions?.length || 0} questions</span>
                      </div>
                      {guide.openingTemplate && (
                        <Badge variant="outline" className="text-xs">Has Opening</Badge>
                      )}
                      {guide.closingTemplate && (
                        <Badge variant="outline" className="text-xs">Has Closing</Badge>
                      )}
                      {visibilityScopeLabel(guide.visibilityScope) && (
                        <Badge
                          variant={visibilityScopeBadgeVariant(guide.visibilityScope)}
                          className="text-xs"
                        >
                          {visibilityScopeLabel(guide.visibilityScope)}
                        </Badge>
                      )}
                    </div>
                  </CardContent>
                </Link>
              ) : (
                <>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <div className="p-2 bg-primary/10 rounded-lg">
                          <BookOpen className="h-6 w-6 text-primary" />
                        </div>
                        <div>
                          <CardTitle className="text-lg">{guide.name}</CardTitle>
                        </div>
                      </div>
                      <Badge variant={guide.isActive ? "default" : "secondary"}>
                        {guide.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </div>
                    {guide.description && (
                      <CardDescription className="line-clamp-2 mt-2">
                        {guide.description}
                      </CardDescription>
                    )}
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-center gap-4 text-sm text-muted-foreground flex-wrap mb-3">
                      <div className="flex items-center gap-1">
                        <MessageSquare className="h-4 w-4" />
                        <span>{guide.questionCount || guide.questions?.length || 0} questions</span>
                      </div>
                      {guide.ownerOrganizationName && (
                        <Badge variant="outline" className="text-xs gap-1">
                          <MapPin className="h-3 w-3" />
                          Inherited from {guide.ownerOrganizationName}
                        </Badge>
                      )}
                    </div>
                    <Button
                      className="w-full"
                      variant="secondary"
                      onClick={() => handleClone(guide.id)}
                      disabled={cloningId === guide.id}
                    >
                      {cloningId === guide.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Cloning...
                        </>
                      ) : (
                        <>
                          <Copy className="mr-2 h-4 w-4" />
                          Clone to this org
                        </>
                      )}
                    </Button>
                  </CardContent>
                </>
              )}
            </Card>
          ))}
        </div>

        {totalPages > 1 && (
          <div className="flex justify-center items-center gap-2">
            <Button
              variant="outline"
              onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
              disabled={currentPage === 1}
            >
              Previous
            </Button>

            <div className="flex items-center gap-2 px-4">
              <span className="text-sm text-muted-foreground">
                Page {currentPage} of {totalPages}
              </span>
              <span className="text-sm text-muted-foreground">
                ({totalCount} total)
              </span>
            </div>

            <Button
              variant="outline"
              onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
              disabled={currentPage === totalPages}
            >
              Next
            </Button>
          </div>
        )}
      </>
    );
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Interview Guides</h1>
            <p className="text-muted-foreground mt-2">
              Create reusable question sets with opening/closing templates and scoring rubrics
            </p>
          </div>

          <div className="flex flex-col sm:flex-row gap-2">
            <form onSubmit={handleSearch} className="flex gap-2 w-full md:w-[500px]">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search guides..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
              <Button type="submit" variant="outline">Search</Button>
            </form>

            <Link href="/interview-guides/new">
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                New Guide
              </Button>
            </Link>
          </div>
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-destructive/10 text-destructive rounded-lg">
          {error}
        </div>
      )}

      <Tabs
        value={activeTab}
        onValueChange={handleTabChange}
        className="w-full"
      >
        <TabsList className="mb-6">
          <TabsTrigger value="local">Local</TabsTrigger>
          <TabsTrigger value="inherited">Inherited</TabsTrigger>
        </TabsList>

        <TabsContent value="local">
          {renderGuideGrid(guides, false)}
        </TabsContent>

        <TabsContent value="inherited">
          {renderGuideGrid(guides, true)}
        </TabsContent>
      </Tabs>
    </div>
  );
}
