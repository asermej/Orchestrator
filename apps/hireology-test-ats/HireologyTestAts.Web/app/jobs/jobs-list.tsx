"use client";

import { useState } from "react";
import type { JobItem } from "./page";
import { testAtsApi } from "@/lib/test-ats-api";

interface JobsListProps {
  jobs: JobItem[];
  onDeleted: () => void;
  onUpdated?: () => void;
}

export function JobsList({ jobs, onDeleted }: JobsListProps) {
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleDelete = async (id: string) => {
    if (!confirm("Delete this job? It will also be removed from Orchestrator.")) return;
    setDeletingId(id);
    try {
      await testAtsApi.delete(`/api/jobs/${id}`);
      onDeleted();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Delete failed");
    } finally {
      setDeletingId(null);
    }
  };

  if (jobs.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-slate-50 p-8 text-center text-slate-600">
        No jobs yet. Click “Add job” to create one; it will sync to Orchestrator.
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-slate-200 overflow-hidden">
      <table className="min-w-full divide-y divide-slate-200">
        <thead className="bg-slate-50">
          <tr>
            <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">External ID</th>
            <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Title</th>
            <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Location</th>
            <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Status</th>
            <th className="px-4 py-3 text-right text-xs font-medium text-slate-500 uppercase">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 bg-white">
          {jobs.map((job) => (
            <tr key={job.id}>
              <td className="px-4 py-3 text-sm text-slate-900">{job.externalJobId}</td>
              <td className="px-4 py-3 text-sm text-slate-900">{job.title}</td>
              <td className="px-4 py-3 text-sm text-slate-600">{job.location ?? "—"}</td>
              <td className="px-4 py-3 text-sm text-slate-600">{job.status}</td>
              <td className="px-4 py-3 text-right">
                <button
                  type="button"
                  onClick={() => handleDelete(job.id)}
                  disabled={deletingId === job.id}
                  className="text-red-600 hover:text-red-800 text-sm disabled:opacity-50"
                >
                  {deletingId === job.id ? "Deleting…" : "Delete"}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
