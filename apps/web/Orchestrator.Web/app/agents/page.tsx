"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { AgentAvatar } from "@/components/agent-avatar";
import { Header } from "@/components/header";
import { ArrowLeft, Search, Loader2, User } from "lucide-react";
import Link from "next/link";
import { fetchAgents, AgentItem } from "./actions";

export default function BrowseAgents() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const searchParams = useSearchParams();
  
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(12);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  // Read search parameter from URL on initial load
  useEffect(() => {
    const searchFromUrl = searchParams.get("search");
    if (searchFromUrl) {
      setSearchTerm(searchFromUrl);
    }
  }, [searchParams]);

  useEffect(() => {
    if (user) {
      loadAgents();
    }
  }, [user, currentPage, searchTerm]);

  const loadAgents = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchAgents(currentPage, pageSize, searchTerm);
      setAgents(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error("Error loading agents:", err);
      setError("Failed to load agents. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1); // Reset to first page on new search
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

  return (
    <div className="min-h-screen bg-background">
      <Header user={user} />

      <main className="container mx-auto px-4 py-8">
        {/* Header Section */}
        <div className="mb-8">
          <Link href="/">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
          
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold">Discover Agents</h1>
              <p className="text-muted-foreground mt-2">
                Browse all AI agents created by the community
              </p>
            </div>
            
            {/* Search Form */}
            <form onSubmit={handleSearch} className="flex gap-2 max-w-md w-full md:w-auto">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by display name..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
              <Button type="submit">Search</Button>
            </form>
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-destructive/10 text-destructive rounded-lg">
            {error}
          </div>
        )}

        {/* Loading State */}
        {isLoading ? (
          <div className="flex justify-center items-center py-20">
            <Loader2 className="h-12 w-12 animate-spin text-primary" />
          </div>
        ) : agents.length === 0 ? (
          /* Empty State */
          <div className="text-center py-20">
            <User className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-xl font-semibold mb-2">No agents found</h3>
            <p className="text-muted-foreground mb-6">
              {searchTerm
                ? "Try adjusting your search terms"
                : "Be the first to create an agent!"}
            </p>
            {!searchTerm && (
              <Link href="/create-agent">
                <Button>Create Agent</Button>
              </Link>
            )}
          </div>
        ) : (
          /* Agents Grid */
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 mb-8">
              {agents.map((agent) => (
                <Link key={agent.id} href={`/agents/${agent.id}/chat`}>
                  <Card className="overflow-hidden hover:shadow-lg transition-shadow cursor-pointer">
                    <CardHeader className="p-6">
                      <div className="flex flex-col items-center text-center space-y-4">
                        <AgentAvatar
                          imageUrl={agent.profileImageUrl}
                          displayName={agent.displayName}
                          size="2xl"
                          shape="square"
                        />
                        <div className="space-y-1">
                          <h3 className="font-semibold text-lg leading-none">
                            {agent.displayName}
                          </h3>
                        </div>
                      </div>
                    </CardHeader>
                    <CardContent className="p-6 pt-0">
                      <div className="text-xs text-muted-foreground text-center">
                        Created {new Date(agent.createdAt).toLocaleDateString()}
                      </div>
                    </CardContent>
                  </Card>
                </Link>
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
                  onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
                  disabled={currentPage === totalPages}
                >
                  Next
                </Button>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

