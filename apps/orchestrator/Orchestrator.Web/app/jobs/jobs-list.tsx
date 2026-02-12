"use client";

import { useEffect, useState } from "react";
import { fetchJobs, JobItem } from "./actions";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Loader2, Briefcase } from "lucide-react";

export function JobsList() {
  const [jobs, setJobs] = useState<JobItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize] = useState(20);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const res = await fetchJobs(pageNumber, pageSize);
        if (!cancelled) {
          setJobs(res.items);
          setTotalCount(res.totalCount);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load jobs");
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [pageNumber, pageSize]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-slate-400" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-amber-200 bg-amber-50">
        <CardContent className="pt-6">
          <p className="text-amber-800">{error}</p>
        </CardContent>
      </Card>
    );
  }

  if (jobs.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Briefcase className="h-12 w-12 text-slate-300 mb-4" />
            <p className="text-slate-600 font-medium">No jobs yet</p>
            <p className="text-slate-500 text-sm mt-1">
              Create jobs in your ATS and sync them to Orchestrator to see them here.
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-500">
        {totalCount} job{totalCount !== 1 ? "s" : ""} synced from ATS
      </p>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {jobs.map((job) => (
          <Card key={job.id} className="hover:border-slate-300 transition-colors">
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-2">
                <CardTitle className="text-lg">{job.title}</CardTitle>
                <Badge variant="secondary" className="shrink-0">
                  {job.status}
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-2">
              {job.location && (
                <p className="text-sm text-slate-600">{job.location}</p>
              )}
              {job.description && (
                <p className="text-sm text-slate-500 line-clamp-2">{job.description}</p>
              )}
              <p className="text-xs text-slate-400">ID: {job.externalJobId}</p>
            </CardContent>
          </Card>
        ))}
      </div>
      {totalCount > pageSize && (
        <div className="flex items-center justify-center gap-2 pt-4">
          <button
            type="button"
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            disabled={pageNumber <= 1}
            className="px-4 py-2 text-sm border rounded-md disabled:opacity-50"
          >
            Previous
          </button>
          <span className="text-sm text-slate-600">
            Page {pageNumber} of {Math.ceil(totalCount / pageSize)}
          </span>
          <button
            type="button"
            onClick={() => setPageNumber((p) => p + 1)}
            disabled={pageNumber >= Math.ceil(totalCount / pageSize)}
            className="px-4 py-2 text-sm border rounded-md disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
