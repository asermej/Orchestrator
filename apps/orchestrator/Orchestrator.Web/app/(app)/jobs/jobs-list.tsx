"use client";

import { useCallback, useEffect, useState } from "react";
import { fetchJobs, JobItem } from "./actions";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { SearchBar } from "@/components/search-bar";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Loader2,
  Briefcase,
  MoreVertical,
  ChevronDown,
} from "lucide-react";
import { format } from "date-fns";

const PIPELINE_STAGES = [
  "Applied",
  "Shortlisted",
  "In Progress",
  "Pre-Hire",
  "Hired",
] as const;

function getStageCountsForJob(_job: JobItem) {
  return {
    Applied: 0,
    Shortlisted: 0,
    "In Progress": 5,
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

function JobCard({ job }: { job: JobItem }) {
  const stageCounts = getStageCountsForJob(job);
  const createdDate = job.createdAt
    ? format(new Date(job.createdAt), "MM/dd/yyyy")
    : null;

  return (
    <div className="border rounded-lg bg-card hover:shadow-sm transition-shadow">
      {/* Top section: title, badge, date, menu */}
      <div className="px-5 pt-4 pb-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3 flex-wrap min-w-0">
            <h3 className="text-base font-semibold text-primary truncate">
              {job.title}
            </h3>
            <Badge className="bg-teal-600 text-white border-transparent hover:bg-teal-700 text-xs shrink-0">
              {formatStatusBadge(job.status)}
            </Badge>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            {createdDate && (
              <span className="text-sm text-muted-foreground">
                Created: {createdDate}
              </span>
            )}
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreVertical className="h-4 w-4" />
            </Button>
          </div>
        </div>
        {job.location && (
          <p className="text-sm text-muted-foreground mt-1">
            {job.location} &bull; General
          </p>
        )}
      </div>

      {/* Bottom section: pipeline stage counts */}
      <div className="border-t px-5 py-4">
        <div className="grid grid-cols-5 gap-4">
          {PIPELINE_STAGES.map((stage) => {
            const count = stageCounts[stage];
            const isActive = stage === "In Progress";
            return (
              <div key={stage} className="text-center">
                <p
                  className={`text-xl font-bold ${
                    isActive
                      ? "text-foreground"
                      : count > 0
                        ? "text-foreground"
                        : "text-muted-foreground"
                  }`}
                >
                  {count}
                </p>
                <p
                  className={`text-xs mt-0.5 ${
                    isActive
                      ? "text-foreground font-semibold"
                      : "text-muted-foreground"
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

export function JobsList() {
  const [jobs, setJobs] = useState<JobItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize] = useState(20);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");

  const loadJobs = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await fetchJobs(
        pageNumber,
        pageSize,
        searchQuery || undefined
      );
      setJobs(res.items);
      setTotalCount(res.totalCount);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load jobs");
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, pageSize, searchQuery]);

  useEffect(() => {
    loadJobs();
  }, [loadJobs]);

  const handleSearchChange = useCallback((value: string) => {
    setSearchQuery(value);
    setPageNumber(1);
  }, []);

  return (
    <div className="space-y-5">
      {/* Page header */}
      <div>
        <h1 className="text-2xl font-bold text-foreground">Jobs</h1>
      </div>

      {/* Search bar */}
      <SearchBar
        value={searchQuery}
        onChange={handleSearchChange}
        placeholder="Search jobs by title"
        className="max-w-full"
      />

      {/* Filter row (cosmetic) */}
      <div className="flex items-center gap-3 flex-wrap">
        <Select>
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Location/Store" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Locations</SelectItem>
          </SelectContent>
        </Select>

        <Select>
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Hiring Manager" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Managers</SelectItem>
          </SelectContent>
        </Select>

        <Select>
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Department" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Departments</SelectItem>
          </SelectContent>
        </Select>

        <Select>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Status & Visibility" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
          </SelectContent>
        </Select>

        <Button variant="outline" size="sm" className="text-muted-foreground">
          More Filters
        </Button>

        <button
          type="button"
          className="text-sm text-muted-foreground hover:text-foreground underline underline-offset-2"
        >
          Reset Filters
        </button>
      </div>

      {/* Sort bar and results count */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm">
          <span className="text-muted-foreground">Sort By</span>
          <Select defaultValue="newest">
            <SelectTrigger className="w-[180px] h-8 text-sm font-medium border-0 shadow-none px-1 gap-1">
              <SelectValue />
              <ChevronDown className="h-3.5 w-3.5 opacity-70" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="newest">Newest First</SelectItem>
              <SelectItem value="title">Title A-Z</SelectItem>
              <SelectItem value="applied">Most Applied</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <span className="text-sm text-muted-foreground">
          {totalCount} Result{totalCount !== 1 ? "s" : ""}
        </span>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      )}

      {/* Error state */}
      {!isLoading && error && (
        <div className="border border-amber-200 bg-amber-50 rounded-lg p-4">
          <p className="text-amber-800">{error}</p>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && !error && jobs.length === 0 && (
        <div className="border rounded-lg p-12">
          <div className="flex flex-col items-center justify-center text-center">
            <Briefcase className="h-12 w-12 text-muted-foreground/40 mb-4" />
            <p className="text-foreground font-medium">No jobs found</p>
            <p className="text-muted-foreground text-sm mt-1">
              {searchQuery
                ? "Try adjusting your search or filters."
                : "Create jobs in your ATS and sync them to see them here."}
            </p>
          </div>
        </div>
      )}

      {/* Job cards */}
      {!isLoading && !error && jobs.length > 0 && (
        <div className="space-y-4">
          {jobs.map((job) => (
            <JobCard key={job.id} job={job} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {!isLoading && totalCount > pageSize && (
        <div className="flex items-center justify-center gap-2 pt-4">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            disabled={pageNumber <= 1}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {pageNumber} of {Math.ceil(totalCount / pageSize)}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPageNumber((p) => p + 1)}
            disabled={pageNumber >= Math.ceil(totalCount / pageSize)}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
