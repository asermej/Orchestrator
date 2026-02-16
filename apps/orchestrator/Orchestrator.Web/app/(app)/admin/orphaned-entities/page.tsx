"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Loader2,
  AlertTriangle,
  Bot,
  BookOpen,
  Settings2,
  Briefcase,
  Users,
  RefreshCw,
} from "lucide-react";
import { fetchOrphanedEntities, OrphanedEntitySummary } from "./actions";

export default function OrphanedEntitiesPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [summary, setSummary] = useState<OrphanedEntitySummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user) {
      loadSummary();
    }
  }, [user]);

  const loadSummary = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await fetchOrphanedEntities();
      setSummary(data);
    } catch (err: any) {
      if (err?.error?.statusCode === 403) {
        setError("You do not have permission to view this page. Group Admin access is required.");
      } else {
        setError("Failed to load orphaned entity summary. Please try again.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  if (isUserLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) return null;

  const entityCards = summary
    ? [
        {
          label: "Agents",
          count: summary.orphanedAgentCount,
          icon: Bot,
          color: "text-violet-600",
          bg: "bg-violet-100",
        },
        {
          label: "Interview Guides",
          count: summary.orphanedInterviewGuideCount,
          icon: BookOpen,
          color: "text-blue-600",
          bg: "bg-blue-100",
        },
        {
          label: "Interview Configs",
          count: summary.orphanedInterviewConfigurationCount,
          icon: Settings2,
          color: "text-emerald-600",
          bg: "bg-emerald-100",
        },
        {
          label: "Jobs",
          count: summary.orphanedJobCount,
          icon: Briefcase,
          color: "text-amber-600",
          bg: "bg-amber-100",
        },
        {
          label: "Applicants",
          count: summary.orphanedApplicantCount,
          icon: Users,
          color: "text-rose-600",
          bg: "bg-rose-100",
        },
      ]
    : [];

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">Orphaned Entities</h1>
          <p className="text-muted-foreground mt-2">
            Entities assigned to organizations that no longer exist in the ATS.
            These need to be reassigned or cleaned up.
          </p>
        </div>
        <Button
          variant="outline"
          onClick={loadSummary}
          disabled={isLoading}
          className="gap-2"
        >
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          Refresh
        </Button>
      </div>

      {error && (
        <Card className="border-destructive/50 bg-destructive/5 mb-8">
          <CardContent className="pt-6">
            <p className="text-destructive">{error}</p>
          </CardContent>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center items-center py-20">
          <Loader2 className="h-12 w-12 animate-spin text-primary" />
        </div>
      ) : summary ? (
        <>
          {/* Summary banner */}
          <Card className={`mb-8 ${summary.totalOrphanedCount > 0 ? "border-amber-200 bg-amber-50/50" : "border-emerald-200 bg-emerald-50/50"}`}>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                {summary.totalOrphanedCount > 0 ? (
                  <>
                    <AlertTriangle className="h-6 w-6 text-amber-600" />
                    <div>
                      <p className="font-semibold text-amber-900">
                        {summary.totalOrphanedCount} orphaned{" "}
                        {summary.totalOrphanedCount === 1 ? "entity" : "entities"} found
                      </p>
                      <p className="text-sm text-amber-700 mt-0.5">
                        Across {summary.orphanedOrganizationIds.length} unknown{" "}
                        {summary.orphanedOrganizationIds.length === 1
                          ? "organization"
                          : "organizations"}
                      </p>
                    </div>
                  </>
                ) : (
                  <div>
                    <p className="font-semibold text-emerald-900">
                      No orphaned entities
                    </p>
                    <p className="text-sm text-emerald-700 mt-0.5">
                      All entities are assigned to valid organizations.
                    </p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Entity type breakdown */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4 mb-8">
            {entityCards.map((card) => (
              <Card key={card.label}>
                <CardContent className="pt-6">
                  <div className="flex items-center gap-3">
                    <div className={`p-2 rounded-lg ${card.bg}`}>
                      <card.icon className={`h-5 w-5 ${card.color}`} />
                    </div>
                    <div>
                      <p className="text-2xl font-bold">{card.count}</p>
                      <p className="text-sm text-muted-foreground">{card.label}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Orphaned organization IDs */}
          {summary.orphanedOrganizationIds.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Unknown Organization IDs</CardTitle>
                <CardDescription>
                  These organization IDs are referenced by entities in AI Assistants
                  but do not exist in the ATS. They may have been deleted.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {summary.orphanedOrganizationIds.map((orgId) => (
                    <Badge key={orgId} variant="outline" className="font-mono text-xs">
                      {orgId}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </>
      ) : null}
    </div>
  );
}
