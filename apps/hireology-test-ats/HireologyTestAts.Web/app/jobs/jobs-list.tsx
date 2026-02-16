"use client";

import { useState } from "react";
import type { JobItem } from "./page";
import { testAtsApi } from "@/lib/test-ats-api";
import { MoreVertical, Briefcase } from "lucide-react";

const PIPELINE_STAGES = [
  "Applied",
  "Shortlisted",
  "In Progress",
  "Pre-Hire",
  "Hired",
] as const;

function getStageCounts(applicantCount: number) {
  return {
    Applied: 0,
    Shortlisted: 0,
    "In Progress": applicantCount,
    "Pre-Hire": 0,
    Hired: 0,
  };
}

function formatStatusBadge(status: string) {
  const normalized = status.toLowerCase();
  if (normalized === "active" || normalized === "open") {
    return "Open - Internal";
  }
  if (normalized === "closed") {
    return "Closed";
  }
  return status;
}

function formatDate(dateStr: string) {
  const d = new Date(dateStr);
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  const yyyy = d.getFullYear();
  return `${mm}/${dd}/${yyyy}`;
}

interface JobCardProps {
  job: JobItem;
  onDelete: (id: string) => void;
  isDeleting: boolean;
}

function JobCard({ job, onDelete, isDeleting }: JobCardProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const stageCounts = getStageCounts(job.applicantCount ?? 0);

  return (
    <div className="border border-slate-200 rounded-lg bg-white hover:shadow-sm transition-shadow">
      {/* Top section: title, badge, date, menu */}
      <div className="px-5 pt-4 pb-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3 flex-wrap min-w-0">
            <a
              href={`/jobs/${job.id}`}
              className="text-base font-semibold text-teal-700 hover:text-teal-900 truncate"
            >
              {job.title}
            </a>
            <span className="inline-flex items-center gap-1 rounded-full bg-teal-600 px-2.5 py-0.5 text-xs font-semibold text-white whitespace-nowrap">
              {formatStatusBadge(job.status)}
            </span>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            <span className="text-sm text-slate-500">
              Created: {formatDate(job.createdAt)}
            </span>
            <div className="relative">
              <button
                type="button"
                onClick={() => setMenuOpen(!menuOpen)}
                className="p-1 rounded hover:bg-slate-100 text-slate-400 hover:text-slate-600"
              >
                <MoreVertical className="h-4 w-4" />
              </button>
              {menuOpen && (
                <>
                  <div
                    className="fixed inset-0 z-10"
                    onClick={() => setMenuOpen(false)}
                  />
                  <div className="absolute right-0 top-full mt-1 z-20 w-36 rounded-md border border-slate-200 bg-white shadow-lg py-1">
                    <a
                      href={`/jobs/${job.id}`}
                      className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                    >
                      View Details
                    </a>
                    <button
                      type="button"
                      disabled={isDeleting}
                      onClick={() => {
                        setMenuOpen(false);
                        onDelete(job.id);
                      }}
                      className="block w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 disabled:opacity-50"
                    >
                      {isDeleting ? "Deleting…" : "Delete"}
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
        {job.location && (
          <p className="text-sm text-slate-500 mt-1">
            {job.location} &bull; General
          </p>
        )}
      </div>

      {/* Bottom section: pipeline stage counts */}
      <div className="border-t border-slate-200 px-5 py-4">
        <div className="grid grid-cols-5 gap-4">
          {PIPELINE_STAGES.map((stage) => {
            const count = stageCounts[stage];
            const isActive = stage === "In Progress";
            return (
              <div key={stage} className="text-center">
                <p
                  className={`text-xl font-bold ${
                    isActive || count > 0
                      ? "text-slate-900"
                      : "text-slate-400"
                  }`}
                >
                  {count}
                </p>
                <p
                  className={`text-xs mt-0.5 ${
                    isActive
                      ? "text-slate-900 font-semibold"
                      : "text-slate-500"
                  }`}
                >
                  {stage}
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

interface JobsListProps {
  jobs: JobItem[];
  searchQuery: string;
  onDeleted: () => void;
  onUpdated?: () => void;
}

export function JobsList({ jobs, searchQuery, onDeleted }: JobsListProps) {
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleDelete = async (id: string) => {
    if (
      !confirm("Delete this job? It will also be removed from Orchestrator.")
    )
      return;
    setDeletingId(id);
    try {
      await testAtsApi.delete(`/api/v1/jobs/${id}`);
      onDeleted();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Delete failed");
    } finally {
      setDeletingId(null);
    }
  };

  if (jobs.length === 0) {
    return (
      <div className="border border-slate-200 rounded-lg p-12">
        <div className="flex flex-col items-center justify-center text-center">
          <Briefcase className="h-12 w-12 text-slate-300 mb-4" />
          <p className="text-slate-700 font-medium">No jobs found</p>
          <p className="text-slate-500 text-sm mt-1">
            {searchQuery
              ? "Try adjusting your search or filters."
              : 'Click "Create a Job" to add one. It will sync to Orchestrator.'}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {jobs.map((job) => (
        <JobCard
          key={job.id}
          job={job}
          onDelete={handleDelete}
          isDeleting={deletingId === job.id}
        />
      ))}
    </div>
  );
}
