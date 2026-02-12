"use client";

import { useEffect, useState, useCallback } from "react";
import { JobsList } from "./jobs-list";
import { CreateJobForm } from "./create-job-form";
import { testAtsApi } from "@/lib/test-ats-api";

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

  const loadJobs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await testAtsApi.get<JobListResponse>("/api/jobs?pageNumber=1&pageSize=100");
      setJobs(data.items);
      setTotalCount(data.totalCount);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load jobs");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadJobs();
  }, [loadJobs]);

  useEffect(() => {
    const handler = () => loadJobs();
    window.addEventListener("test-ats-context-changed", handler);
    return () => window.removeEventListener("test-ats-context-changed", handler);
  }, [loadJobs]);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Jobs</h1>
          <p className="text-slate-600 text-sm mt-1">
            Jobs are scoped to your selected location. Create jobs here; they sync to Orchestrator.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
        >
          Add job
        </button>
      </div>

      {showCreate && (
        <CreateJobForm
          onClose={() => setShowCreate(false)}
          onSaved={() => {
            setShowCreate(false);
            loadJobs();
          }}
        />
      )}

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      {loading ? (
        <p className="text-slate-500">Loading…</p>
      ) : (
        <JobsList jobs={jobs} onDeleted={loadJobs} onUpdated={loadJobs} />
      )}
    </div>
  );
}
