"use client";

import { useEffect, useState, useMemo, useCallback } from "react";
import { testAtsApi } from "@/lib/test-ats-api";
import { ApplyModal } from "./apply-modal";

interface JobItem {
  id: string;
  externalJobId: string;
  title: string;
  description?: string | null;
  location?: string | null;
  status: string;
  organizationId?: string | null;
  createdAt: string;
}

interface JobListResponse {
  items: JobItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export default function CareersPage() {
  const [jobs, setJobs] = useState<JobItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedLocation, setSelectedLocation] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [applyingJob, setApplyingJob] = useState<JobItem | null>(null);

  const loadJobs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await testAtsApi.get<JobListResponse>(
        "/api/v1/jobs?pageNumber=1&pageSize=500"
      );
      // Only show active jobs on the career site
      setJobs(data.items.filter((j) => j.status === "active"));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load jobs");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadJobs();
  }, [loadJobs]);

  // Reload when location context changes
  useEffect(() => {
    const handler = () => loadJobs();
    window.addEventListener("test-ats-context-changed", handler);
    return () => window.removeEventListener("test-ats-context-changed", handler);
  }, [loadJobs]);

  // Extract distinct locations
  const locations = useMemo(() => {
    const locs = new Set<string>();
    jobs.forEach((job) => {
      if (job.location) locs.add(job.location);
    });
    return Array.from(locs).sort();
  }, [jobs]);

  // Filter jobs
  const filteredJobs = useMemo(() => {
    let result = jobs;
    if (selectedLocation) {
      result = result.filter((j) => j.location === selectedLocation);
    }
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (j) =>
          j.title.toLowerCase().includes(query) ||
          j.description?.toLowerCase().includes(query)
      );
    }
    return result;
  }, [jobs, selectedLocation, searchQuery]);

  // Group by location
  const groupedJobs = useMemo(() => {
    const groups: Record<string, JobItem[]> = {};
    filteredJobs.forEach((job) => {
      const loc = job.location || "Other";
      if (!groups[loc]) groups[loc] = [];
      groups[loc].push(job);
    });
    return groups;
  }, [filteredJobs]);

  const sortedLocationKeys = Object.keys(groupedJobs).sort((a, b) => {
    if (a === "Other") return 1;
    if (b === "Other") return -1;
    return a.localeCompare(b);
  });

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">Careers</h1>
        <p className="text-slate-600 text-sm mt-1">
          Browse open positions and apply.
        </p>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <p className="text-slate-500">Loading...</p>
      ) : jobs.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-slate-50 p-8 text-center text-slate-600">
          No open positions at this time. Check back later.
        </div>
      ) : (
        <>
          {/* Search */}
          <div className="mb-4">
            <input
              type="text"
              placeholder="Search positions..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full max-w-md rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 focus:border-indigo-300 focus:outline-none focus:ring-2 focus:ring-indigo-100"
            />
          </div>

          {/* Location pills */}
          {locations.length > 1 && (
            <div className="mb-4 flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setSelectedLocation(null)}
                className={`rounded-full px-3 py-1 text-sm font-medium transition-colors ${
                  selectedLocation === null
                    ? "bg-indigo-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                All locations
              </button>
              {locations.map((loc) => (
                <button
                  key={loc}
                  type="button"
                  onClick={() =>
                    setSelectedLocation(loc === selectedLocation ? null : loc)
                  }
                  className={`rounded-full px-3 py-1 text-sm font-medium transition-colors ${
                    selectedLocation === loc
                      ? "bg-indigo-600 text-white"
                      : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                  }`}
                >
                  {loc}
                </button>
              ))}
            </div>
          )}

          {/* Results count */}
          <p className="mb-4 text-sm text-slate-500">
            {filteredJobs.length} position{filteredJobs.length !== 1 ? "s" : ""}
            {selectedLocation ? ` in ${selectedLocation}` : ""}
            {searchQuery ? ` matching "${searchQuery}"` : ""}
          </p>

          {filteredJobs.length === 0 ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-8 text-center text-slate-600">
              No positions match your filters. Try adjusting your search.
            </div>
          ) : (
            <div className="space-y-6">
              {sortedLocationKeys.map((location) => (
                <div key={location}>
                  <div className="flex items-center gap-2 mb-3">
                    <svg
                      className="w-4 h-4 text-slate-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
                      />
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                    </svg>
                    <h3 className="text-base font-semibold text-slate-800">
                      {location}
                    </h3>
                    <span className="text-sm text-slate-400">
                      ({groupedJobs[location].length})
                    </span>
                  </div>
                  <div className="space-y-2">
                    {groupedJobs[location].map((job) => (
                      <div
                        key={job.id}
                        className="flex items-start justify-between gap-4 rounded-lg border border-slate-200 bg-white p-4 hover:border-indigo-200 transition-colors"
                      >
                        <div className="flex-1 min-w-0">
                          <h4 className="text-sm font-semibold text-slate-900">
                            {job.title}
                          </h4>
                          {job.description && (
                            <p className="mt-1 text-sm text-slate-500 line-clamp-2">
                              {job.description}
                            </p>
                          )}
                          {job.location && (
                            <p className="mt-1 text-xs text-slate-400">
                              {job.location}
                            </p>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => setApplyingJob(job)}
                          className="shrink-0 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 transition-colors"
                        >
                          Apply
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {/* Apply modal */}
      {applyingJob && (
        <ApplyModal
          job={applyingJob}
          onClose={() => setApplyingJob(null)}
        />
      )}
    </div>
  );
}
