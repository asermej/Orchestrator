"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ArrowLeft, Search, Loader2, Briefcase, Plus, Clock, MessageSquare } from "lucide-react";
import Link from "next/link";
import { fetchJobTypes, JobTypeItem } from "./actions";

export default function JobTypesPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  
  const [jobTypes, setJobTypes] = useState<JobTypeItem[]>([]);
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

  useEffect(() => {
    if (user) {
      loadJobTypes();
    }
  }, [user, currentPage, searchTerm]);

  const loadJobTypes = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchJobTypes(currentPage, pageSize, searchTerm);
      setJobTypes(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      console.error("Error loading job types:", err);
      setError("Failed to load job types. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
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
        <div className="mb-8">
          <Link href="/">
            <Button variant="ghost" className="mb-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </Link>
          
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold">Job Types</h1>
              <p className="text-muted-foreground mt-2">
                Manage interview templates for different positions
              </p>
            </div>
            
            <div className="flex gap-2">
              <form onSubmit={handleSearch} className="flex gap-2 max-w-md w-full md:w-auto">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search job types..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Button type="submit">Search</Button>
              </form>
              
              <Link href="/job-types/new">
                <Button className="gap-2">
                  <Plus className="h-4 w-4" />
                  New Job Type
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

        {isLoading ? (
          <div className="flex justify-center items-center py-20">
            <Loader2 className="h-12 w-12 animate-spin text-primary" />
          </div>
        ) : jobTypes.length === 0 ? (
          <div className="text-center py-20">
            <Briefcase className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-xl font-semibold mb-2">No job types found</h3>
            <p className="text-muted-foreground mb-6">
              {searchTerm
                ? "Try adjusting your search terms"
                : "Create your first job type to get started!"}
            </p>
            {!searchTerm && (
              <Link href="/job-types/new">
                <Button>Create Job Type</Button>
              </Link>
            )}
          </div>
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
              {jobTypes.map((jobType) => (
                <Link key={jobType.id} href={`/job-types/${jobType.id}`}>
                  <Card className="overflow-hidden hover:shadow-lg transition-shadow cursor-pointer h-full">
                    <CardHeader>
                      <div className="flex items-start justify-between">
                        <div className="p-2 bg-primary/10 rounded-lg">
                          <Briefcase className="h-6 w-6 text-primary" />
                        </div>
                      </div>
                      <CardTitle className="mt-4">{jobType.name}</CardTitle>
                      {jobType.description && (
                        <CardDescription className="line-clamp-2">
                          {jobType.description}
                        </CardDescription>
                      )}
                    </CardHeader>
                    <CardContent>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <MessageSquare className="h-4 w-4" />
                          <span>{jobType.questionCount} questions</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Clock className="h-4 w-4" />
                          <span>{jobType.interviewDurationMinutes} min</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </Link>
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
        )}
      </main>
    </div>
  );
}
