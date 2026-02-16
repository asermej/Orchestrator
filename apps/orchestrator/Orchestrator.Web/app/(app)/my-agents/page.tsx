"use client";

import { useEffect, useState, useCallback } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { AgentAvatar } from "@/components/agent-avatar";
import {
  Search,
  Loader2,
  User,
  Pencil,
  Plus,
  GraduationCap,
  ArrowRight,
  Copy,
  Building2,
  MapPin,
} from "lucide-react";
import Link from "next/link";
import { fetchMyAgents, cloneAgent, AgentItem } from "./actions";
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

export default function MyAgents() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const searchParams = useSearchParams();

  const [activeTab, setActiveTab] = useState<"local" | "inherited">("local");
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(12);
  const [error, setError] = useState<string | null>(null);
  const [cloningId, setCloningId] = useState<string | null>(null);

  // Read the selected org cookie on every render so agent fetches react to org changes.
  const selectedOrgId = getCookie("orchestrator_selected_org");
  const hasOrgSelected = !!selectedOrgId;
  // Track the selected org as state so React detects cookie changes across re-renders
  // triggered by the selectOrganization server action's revalidatePath.
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

  useEffect(() => {
    const searchFromUrl = searchParams.get("search");
    if (searchFromUrl) {
      setSearchTerm(searchFromUrl);
    }
  }, [searchParams]);

  const loadAgents = useCallback(async () => {
    if (!trackedOrgId) {
      setAgents([]);
      setTotalCount(0);
      setIsLoading(false);
      return;
    }
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchMyAgents(
        currentPage,
        pageSize,
        searchTerm,
        activeTab
      );
      setAgents(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error("Error loading agents:", err);
      setError("Failed to load agents. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, pageSize, searchTerm, activeTab, trackedOrgId]);

  useEffect(() => {
    if (user && trackedOrgId) {
      loadAgents();
    }
  }, [user, loadAgents, trackedOrgId]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
  };

  const handleTabChange = (tab: string) => {
    setActiveTab(tab as "local" | "inherited");
    setCurrentPage(1);
    setSearchTerm("");
  };

  const handleClone = async (agentId: string) => {
    try {
      setCloningId(agentId);
      const cloned = await cloneAgent(agentId);
      toast.success("Agent cloned successfully!");
      router.push(`/my-agents/${cloned.id}/edit`);
    } catch (err) {
      console.error("Error cloning agent:", err);
      toast.error("Failed to clone agent. Please try again.");
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
          <h1 className="text-3xl font-bold">Manage Agents</h1>
          <p className="text-muted-foreground mt-2">
            Create new agents or edit the ones you&apos;ve already created
          </p>
        </div>
        <div className="text-center py-20">
          <Building2 className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-xl font-semibold mb-2">
            Select an organization
          </h3>
          <p className="text-muted-foreground max-w-md mx-auto">
            Please select an organization from the dropdown in the top-right
            corner to manage agents. Agents are scoped to specific
            organizations.
          </p>
        </div>
      </div>
    );
  }

  const renderAgentGrid = (agentList: AgentItem[], isInherited: boolean) => {
    if (isLoading) {
      return (
        <div className="flex justify-center items-center py-20">
          <Loader2 className="h-12 w-12 animate-spin text-primary" />
        </div>
      );
    }

    if (agentList.length === 0) {
      return (
        <div className="text-center py-20">
          <User className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-xl font-semibold mb-2">
            {isInherited ? "No inherited agents" : "No agents yet"}
          </h3>
          <p className="text-muted-foreground mb-6">
            {searchTerm
              ? "No agents match your search"
              : isInherited
                ? "No agents have been shared from parent organizations"
                : "Create your first AI agent to get started!"}
          </p>
          {!searchTerm && !isInherited && (
            <Link href="/create-agent">
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                Create Your First Agent
              </Button>
            </Link>
          )}
        </div>
      );
    }

    return (
      <>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 mb-8">
          {agentList.map((agent) => (
            <Card
              key={agent.id}
              className="overflow-hidden hover:shadow-lg transition-shadow relative"
            >
              {/* Top-right action button */}
              {!isInherited && (
                <Link
                  href={`/my-agents/${agent.id}/edit`}
                  className="absolute top-2 right-2 z-10"
                >
                  <Button
                    variant="outline"
                    size="icon"
                    className="h-8 w-8 rounded-md bg-background shadow-sm"
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                </Link>
              )}

              <CardHeader className="p-4 pb-3">
                <div className="flex flex-col items-center text-center space-y-3">
                  <AgentAvatar
                    imageUrl={agent.profileImageUrl}
                    displayName={agent.displayName}
                    size="2xl"
                    shape="square"
                  />
                  <div className="space-y-1.5">
                    <h3 className="font-semibold text-base leading-none">
                      {agent.displayName}
                    </h3>

                    {/* Visibility scope badge (local tab only) */}
                    {!isInherited &&
                      visibilityScopeLabel(agent.visibilityScope) && (
                        <Badge
                          variant={visibilityScopeBadgeVariant(
                            agent.visibilityScope
                          )}
                          className="text-[10px]"
                        >
                          {visibilityScopeLabel(agent.visibilityScope)}
                        </Badge>
                      )}

                    {/* Inherited from badge */}
                    {isInherited && agent.ownerOrganizationName && (
                      <Badge variant="outline" className="text-[10px] gap-1">
                        <MapPin className="h-3 w-3" />
                        Inherited from {agent.ownerOrganizationName}
                      </Badge>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent className="p-4 pt-0 space-y-2">
                {isInherited ? (
                  <Button
                    className="w-full"
                    variant="secondary"
                    onClick={() => handleClone(agent.id)}
                    disabled={cloningId === agent.id}
                  >
                    {cloningId === agent.id ? (
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
                ) : (
                  <Link href={`/my-agents/${agent.id}/general-training`}>
                    <Button className="w-full" variant="secondary">
                      Train
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </Button>
                  </Link>
                )}
                <div className="text-xs text-muted-foreground text-center pt-1.5 border-t">
                  Created {new Date(agent.createdAt).toLocaleDateString()}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Pagination */}
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
              onClick={() =>
                setCurrentPage((prev) => Math.min(totalPages, prev + 1))
              }
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
      {/* Header Section */}
      <div className="mb-8">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Manage Agents</h1>
            <p className="text-muted-foreground mt-2">
              Create new agents or edit the ones you&apos;ve already created
            </p>
          </div>

          <div className="flex flex-col sm:flex-row gap-2">
            {/* Search Form */}
            <form
              onSubmit={handleSearch}
              className="flex gap-2 w-full md:w-[500px]"
            >
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by display name..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
              <Button type="submit" variant="outline">
                Search
              </Button>
            </form>

            {/* Create New Button */}
            <Link href="/create-agent">
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                Create New
              </Button>
            </Link>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-6 p-4 bg-destructive/10 text-destructive rounded-lg">
          {error}
        </div>
      )}

      {/* Tabs */}
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
          {renderAgentGrid(agents, false)}
        </TabsContent>

        <TabsContent value="inherited">
          {renderAgentGrid(agents, true)}
        </TabsContent>
      </Tabs>
    </div>
  );
}
