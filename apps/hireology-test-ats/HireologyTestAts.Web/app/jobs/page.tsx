"use client";

import { useEffect, useState, useCallback } from "react";
import { JobsList } from "./jobs-list";
import { CreateJobForm } from "./create-job-form";
import { testAtsApi } from "@/lib/test-ats-api";
import { Search, X } from "lucide-react";

export interface JobItem {
  id: string;
  externalJobId: string;
  title: string;
  description?: string | null;
  location?: string | null;
  status: string;
  organizationId?: string | null;
  createdAt: string;
  updatedAt: string;
  applicantCount: number;
}

interface JobListResponse {
  items: JobItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export default function JobsPage() {
  const [jobs, setJobs] = useState<JobItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  const loadJobs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({ pageNumber: "1", pageSize: "100" });
      if (searchQuery.trim()) {
        params.set("title", searchQuery.trim());
      }
      const data = await testAtsApi.get<JobListResponse>(
        `/api/v1/jobs?${params.toString()}`
      );
      setJobs(data.items);
      setTotalCount(data.totalCount);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load jobs");
    } finally {
      setLoading(false);
    }
  }, [searchQuery]);

  useEffect(() => {
    loadJobs();
  }, [loadJobs]);

  useEffect(() => {
    const handler = () => loadJobs();
    window.addEventListener("test-ats-context-changed", handler);
    return () =>
      window.removeEventListener("test-ats-context-changed", handler);
  }, [loadJobs]);

  return (
    <div className="space-y-5">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Jobs</h1>
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-teal-700 text-white rounded-md text-sm font-medium hover:bg-teal-800 transition-colors"
          >
            Create a Job
          </button>
        </div>
      </div>

      {/* Search bar */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
        <input
          type="text"
          placeholder="Search jobs by title"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full rounded-md border border-slate-300 bg-white pl-10 pr-10 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-teal-500 focus:outline-none focus:ring-1 focus:ring-teal-500"
        />
        {searchQuery && (
          <button
            type="button"
            onClick={() => setSearchQuery("")}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Filter row (cosmetic) */}
      <div className="flex items-center gap-3 flex-wrap">
        <select
          disabled
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 appearance-none pr-8 cursor-default"
          defaultValue=""
        >
          <option value="" disabled>
            Location/Store
          </option>
        </select>

        <select
          disabled
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 appearance-none pr-8 cursor-default"
          defaultValue=""
        >
          <option value="" disabled>
            Hiring Manager
          </option>
        </select>

        <select
          disabled
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 appearance-none pr-8 cursor-default"
          defaultValue=""
        >
          <option value="" disabled>
            Department
          </option>
        </select>

        <select
          disabled
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 appearance-none pr-8 cursor-default"
          defaultValue=""
        >
          <option value="" disabled>
            Status &amp; Visibility
          </option>
        </select>

        <button
          type="button"
          disabled
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-500 cursor-default"
        >
          More Filters
        </button>

        <button
          type="button"
          className="text-sm text-slate-500 hover:text-slate-700 underline underline-offset-2"
        >
          Reset Filters
        </button>
      </div>

      {/* Sort bar and results count */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm">
          <span className="text-slate-500">Sort By</span>
          <select className="border-0 bg-transparent text-sm font-semibold text-teal-700 cursor-pointer focus:outline-none">
            <option>Newest First</option>
            <option>Title A-Z</option>
            <option>Most Applied</option>
          </select>
        </div>
        <span className="text-sm text-slate-500">
          {totalCount} Result{totalCount !== 1 ? "s" : ""}
        </span>
      </div>

      {/* Create job modal */}
      {showCreate && (
        <CreateJobForm
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false);
            loadJobs();
          }}
        />
      )}

      {/* Error state */}
      {error && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800 text-sm">
          {error}
        </div>
      )}

      {/* Content */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-slate-200 border-t-teal-600" />
        </div>
      ) : (
        <JobsList
          jobs={jobs}
          searchQuery={searchQuery}
          onDeleted={loadJobs}
          onUpdated={loadJobs}
        />
      )}
    </div>
  );
}
