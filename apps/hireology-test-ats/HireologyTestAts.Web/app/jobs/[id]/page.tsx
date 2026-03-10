"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import { useParams } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";
import { SendInterviewModal } from "./send-interview-modal";
import { InterviewResultsModal } from "./interview-results-modal";
import { ApplicantSidePanel } from "./applicant-side-panel";

interface JobDetail {
  id: string;
  externalJobId: string;
  title: string;
  description?: string | null;
  location?: string | null;
  status: string;
  createdAt: string;
}

interface ApplicantItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  createdAt: string;
}

interface InterviewRequestItem {
  id: string;
  applicantId: string;
  jobId: string;
  orchestratorInterviewId?: string | null;
  inviteUrl?: string | null;
  shortCode?: string | null;
  status: string;
  score?: number | null;
  resultSummary?: string | null;
  resultRecommendation?: string | null;
  resultStrengths?: string | null;
  resultAreasForImprovement?: string | null;
  webhookReceivedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

function JobDescription({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false);
  const [clamped, setClamped] = useState(false);
  const ref = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    if (ref.current) {
      setClamped(ref.current.scrollHeight > ref.current.clientHeight);
    }
  }, [text]);

  return (
    <div>
      <p
        ref={ref}
        className={`text-sm text-slate-500 leading-relaxed ${expanded ? "" : "line-clamp-2"}`}
      >
        {text}
      </p>
      {clamped && (
        <button
          onClick={() => setExpanded((prev) => !prev)}
          className="mt-1 text-xs font-medium text-indigo-500 hover:text-indigo-700 transition-colors"
        >
          {expanded ? "Show less" : "Show more"}
        </button>
      )}
    </div>
  );
}

function StatusBadge({ status, score }: { status: string; score?: number | null }) {
  const config: Record<string, { label: string; className: string; dot: string }> = {
    not_started: {
      label: "Not Started",
      className: "bg-slate-50 text-slate-600 ring-1 ring-slate-200",
      dot: "bg-slate-400",
    },
    in_progress: {
      label: "In Progress",
      className: "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
      dot: "bg-amber-400",
    },
    completed: {
      label: score != null ? `${score}/100` : "Completed",
      className:
        score != null && score >= 70
          ? "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200"
          : score != null && score >= 40
            ? "bg-blue-50 text-blue-700 ring-1 ring-blue-200"
            : score != null
              ? "bg-orange-50 text-orange-700 ring-1 ring-orange-200"
              : "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
      dot:
        score != null && score >= 70
          ? "bg-emerald-400"
          : score != null && score >= 40
            ? "bg-blue-400"
            : score != null
              ? "bg-orange-400"
              : "bg-emerald-400",
    },
    link_expired: {
      label: "Expired",
      className: "bg-red-50 text-red-600 ring-1 ring-red-200",
      dot: "bg-red-400",
    },
  };

  const c = config[status] || {
    label: status,
    className: "bg-slate-50 text-slate-600 ring-1 ring-slate-200",
    dot: "bg-slate-400",
  };

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold ${c.className}`}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${c.dot}`} />
      {c.label}
    </span>
  );
}

function ScoreMiniBar({ score }: { score: number }) {
  const pct = Math.min(100, Math.max(0, score));
  let barColor = "bg-orange-400";
  if (score >= 70) barColor = "bg-emerald-400";
  else if (score >= 40) barColor = "bg-blue-400";

  return (
    <div className="flex items-center gap-2 min-w-[80px]">
      <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden">
        <div
          className={`h-full rounded-full ${barColor} transition-all duration-500`}
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className="text-xs font-semibold text-slate-600 tabular-nums">{score}</span>
    </div>
  );
}

function JobStatusBadge({ status }: { status: string }) {
  const isActive = status === "active";
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
        isActive
          ? "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200"
          : "bg-slate-100 text-slate-600 ring-1 ring-slate-200"
      }`}
    >
      <span
        className={`w-1.5 h-1.5 rounded-full ${isActive ? "bg-emerald-400 animate-pulse" : "bg-slate-400"}`}
      />
      {status.charAt(0).toUpperCase() + status.slice(1)}
    </span>
  );
}

function SkeletonRow() {
  return (
    <tr className="animate-pulse">
      <td className="px-4 py-4 sm:px-6">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-slate-200" />
          <div className="space-y-1.5">
            <div className="h-3.5 w-28 bg-slate-200 rounded" />
            <div className="h-3 w-40 bg-slate-100 rounded lg:hidden" />
          </div>
        </div>
      </td>
      <td className="hidden lg:table-cell px-4 py-4"><div className="h-3.5 w-36 bg-slate-100 rounded" /></td>
      <td className="hidden xl:table-cell px-4 py-4"><div className="h-3.5 w-28 bg-slate-100 rounded" /></td>
      <td className="hidden md:table-cell px-4 py-4"><div className="h-3.5 w-24 bg-slate-100 rounded" /></td>
      <td className="px-4 py-4"><div className="h-6 w-20 bg-slate-100 rounded-full" /></td>
      <td className="px-4 py-4 sm:px-6"><div className="h-7 w-24 bg-slate-100 rounded-lg" /></td>
    </tr>
  );
}

function StatCard({
  label,
  value,
  icon,
  accent,
}: {
  label: string;
  value: string | number;
  icon: React.ReactNode;
  accent: string;
}) {
  return (
    <div className="flex items-center gap-3 rounded-xl bg-white p-4 ring-1 ring-slate-200/60 shadow-sm">
      <div className={`flex items-center justify-center w-10 h-10 rounded-lg ${accent}`}>
        {icon}
      </div>
      <div>
        <p className="text-2xl font-bold text-slate-900 leading-none">{value}</p>
        <p className="text-xs text-slate-500 mt-0.5">{label}</p>
      </div>
    </div>
  );
}

export default function JobDetailPage() {
  const params = useParams<{ id: string }>();
  const jobId = params.id;

  const [job, setJob] = useState<JobDetail | null>(null);
  const [applicants, setApplicants] = useState<ApplicantItem[]>([]);
  const [interviewRequests, setInterviewRequests] = useState<InterviewRequestItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const [sendModalApplicant, setSendModalApplicant] = useState<ApplicantItem | null>(null);
  const [resultsModalRequest, setResultsModalRequest] = useState<InterviewRequestItem | null>(null);
  const [selectedApplicantId, setSelectedApplicantId] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [jobData, applicantData, interviewData] = await Promise.all([
        testAtsApi.get<JobDetail>(`/api/v1/jobs/${jobId}`),
        testAtsApi.get<ApplicantItem[]>(`/api/v1/jobs/${jobId}/applicants`),
        testAtsApi.get<InterviewRequestItem[]>(`/api/v1/jobs/${jobId}/interviews`),
      ]);
      setJob(jobData);
      setApplicants(applicantData);
      setInterviewRequests(interviewData);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load job");
    } finally {
      setLoading(false);
    }
  }, [jobId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const getInterviewRequest = (applicantId: string): InterviewRequestItem | undefined => {
    return interviewRequests.find((r) => r.applicantId === applicantId);
  };

  const handleInterviewSent = (newRequest: InterviewRequestItem) => {
    setInterviewRequests((prev) => [...prev, newRequest]);
  };

  const handleRefreshInvite = async (ir: InterviewRequestItem) => {
    try {
      const updated = await testAtsApi.post<InterviewRequestItem>(
        `/api/v1/interview-requests/${ir.id}/refresh-invite`,
        {}
      );
      setInterviewRequests((prev) => prev.map((r) => (r.id === ir.id ? updated : r)));
    } catch (e) {
      alert(e instanceof Error ? e.message : "Failed to refresh invite");
    }
  };

  const handleCopyUrl = (ir: InterviewRequestItem) => {
    if (ir.inviteUrl) {
      navigator.clipboard.writeText(ir.inviteUrl);
      setCopiedId(ir.id);
      setTimeout(() => setCopiedId(null), 2000);
    }
  };

  // Compute stats
  const completedInterviews = interviewRequests.filter((r) => r.status === "completed");
  const avgScore =
    completedInterviews.length > 0
      ? Math.round(
          completedInterviews.reduce((sum, r) => sum + (r.score || 0), 0) /
            completedInterviews.length
        )
      : null;
  const pendingCount = interviewRequests.filter(
    (r) => r.status === "not_started" || r.status === "in_progress"
  ).length;

  if (error && !job) {
    return (
      <div className="max-w-5xl mx-auto px-4 py-8">
        <a
          href="/jobs"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-800 transition-colors group"
        >
          <svg
            className="w-4 h-4 transition-transform group-hover:-translate-x-0.5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Jobs
        </a>
        <div className="mt-6 p-6 bg-red-50 border border-red-200 rounded-2xl text-red-700 text-sm flex items-center gap-3">
          <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
      {/* Back nav */}
      <div className="pt-6 pb-4">
        <a
          href="/jobs"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-800 transition-colors group"
        >
          <svg
            className="w-4 h-4 transition-transform group-hover:-translate-x-0.5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Jobs
        </a>
      </div>

      {/* Job header card */}
      {loading ? (
        <div className="animate-pulse rounded-2xl bg-white ring-1 ring-slate-200/60 shadow-sm p-6 sm:p-8 mb-8">
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
            <div className="space-y-3 flex-1">
              <div className="h-7 w-56 bg-slate-200 rounded-lg" />
              <div className="flex gap-3">
                <div className="h-5 w-20 bg-slate-100 rounded" />
                <div className="h-5 w-24 bg-slate-100 rounded" />
                <div className="h-5 w-16 bg-slate-100 rounded-full" />
              </div>
              <div className="h-4 w-full max-w-md bg-slate-100 rounded" />
            </div>
          </div>
        </div>
      ) : job ? (
        <div className="rounded-2xl bg-white ring-1 ring-slate-200/60 shadow-sm overflow-hidden mb-8">
          <div className="h-1 bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500" />
          <div className="p-6 sm:p-8">
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-3 mb-2">
                  <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">
                    {job.title}
                  </h1>
                  <JobStatusBadge status={job.status} />
                </div>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-slate-500">
                  <span className="inline-flex items-center gap-1.5">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={1.5}
                        d="M7 20l4-16m2 16l4-16M6 9h14M4 15h14"
                      />
                    </svg>
                    {job.externalJobId}
                  </span>
                  {job.location && (
                    <span className="inline-flex items-center gap-1.5">
                      <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={1.5}
                          d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
                        />
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={1.5}
                          d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
                        />
                      </svg>
                      {job.location}
                    </span>
                  )}
                  <span className="inline-flex items-center gap-1.5">
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={1.5}
                        d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                      />
                    </svg>
                    Posted{" "}
                    {new Date(job.createdAt).toLocaleDateString("en-US", {
                      month: "short",
                      day: "numeric",
                      year: "numeric",
                    })}
                  </span>
                </div>
                {job.description && (
                  <div className="mt-4">
                    <JobDescription text={job.description} />
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {/* Stats row */}
      {!loading && job && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4 mb-8">
          <StatCard
            label="Total Applicants"
            value={applicants.length}
            accent="bg-indigo-50"
            icon={
              <svg className="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
            }
          />
          <StatCard
            label="Interviews Done"
            value={completedInterviews.length}
            accent="bg-emerald-50"
            icon={
              <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            }
          />
          <StatCard
            label="Avg Score"
            value={avgScore != null ? avgScore : "\u2014"}
            accent="bg-purple-50"
            icon={
              <svg className="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"
                />
              </svg>
            }
          />
          <StatCard
            label="Pending"
            value={pendingCount}
            accent="bg-amber-50"
            icon={
              <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            }
          />
        </div>
      )}

      {/* Applicants section */}
      <div className="rounded-2xl bg-white ring-1 ring-slate-200/60 shadow-sm overflow-hidden">
        <div className="px-6 py-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Applicants</h2>
            {!loading && (
              <p className="text-sm text-slate-500 mt-0.5">
                {applicants.length} candidate{applicants.length !== 1 ? "s" : ""} applied
              </p>
            )}
          </div>
        </div>

        {loading ? (
          <table className="w-full">
            <tbody className="divide-y divide-slate-100">
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonRow key={i} />
              ))}
            </tbody>
          </table>
        ) : applicants.length === 0 ? (
          <div className="px-6 py-16 text-center">
            <div className="mx-auto w-16 h-16 rounded-2xl bg-slate-50 ring-1 ring-slate-200 flex items-center justify-center mb-4">
              <svg className="w-8 h-8 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1.5}
                  d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"
                />
              </svg>
            </div>
            <h3 className="text-base font-semibold text-slate-900 mb-1">No applicants yet</h3>
            <p className="text-sm text-slate-500 max-w-sm mx-auto">
              Candidates who apply for this job will appear here. Share the job listing to attract applicants.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50/50">
                  <th className="px-4 py-3 sm:px-6 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Candidate
                  </th>
                  <th className="hidden lg:table-cell px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Email
                  </th>
                  <th className="hidden xl:table-cell px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Phone
                  </th>
                  <th className="hidden md:table-cell px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Applied
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Interview
                  </th>
                  <th className="px-4 py-3 sm:px-6 text-right text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {applicants.map((applicant) => {
                  const ir = getInterviewRequest(applicant.id);
                  const initials = `${applicant.firstName.charAt(0)}${applicant.lastName.charAt(0)}`.toUpperCase();

                  return (
                    <tr
                      key={applicant.id}
                      onClick={() => setSelectedApplicantId(applicant.id)}
                      className="cursor-pointer hover:bg-slate-50/80 transition-colors group"
                    >
                      {/* Candidate - always visible; shows email below name on < lg */}
                      <td className="px-4 py-4 sm:px-6">
                        <div className="flex items-center gap-3">
                          <div className="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 text-white flex items-center justify-center text-xs font-bold shrink-0 shadow-sm">
                            {initials}
                          </div>
                          <div className="min-w-0">
                            <p className="text-sm font-semibold text-slate-900 truncate group-hover:text-indigo-600 transition-colors">
                              {applicant.firstName} {applicant.lastName}
                            </p>
                            <p className="text-xs text-slate-500 truncate lg:hidden">
                              {applicant.email}
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* Email - hidden below lg */}
                      <td className="hidden lg:table-cell px-4 py-4">
                        <span className="text-sm text-slate-600 truncate block max-w-[200px]">
                          {applicant.email}
                        </span>
                      </td>

                      {/* Phone - hidden below xl */}
                      <td className="hidden xl:table-cell px-4 py-4">
                        <span className="text-sm text-slate-500">
                          {applicant.phone || "\u2014"}
                        </span>
                      </td>

                      {/* Applied date - hidden below md */}
                      <td className="hidden md:table-cell px-4 py-4">
                        <span className="text-sm text-slate-500">
                          {new Date(applicant.createdAt).toLocaleDateString("en-US", {
                            month: "short",
                            day: "numeric",
                            year: "numeric",
                          })}
                        </span>
                      </td>

                      {/* Interview status - always visible */}
                      <td className="px-4 py-4">
                        {ir ? (
                          <div className="flex flex-col gap-1">
                            <StatusBadge status={ir.status} score={ir.score} />
                            {ir.status === "completed" && ir.score != null && (
                              <div className="hidden sm:block">
                                <ScoreMiniBar score={ir.score} />
                              </div>
                            )}
                          </div>
                        ) : (
                          <span className="text-xs text-slate-400 italic">No interview</span>
                        )}
                      </td>

                      {/* Actions - always visible */}
                      <td
                        className="px-4 py-4 sm:px-6 text-right"
                        onClick={(e) => e.stopPropagation()}
                      >
                        {!ir ? (
                          <button
                            onClick={() => setSendModalApplicant(applicant)}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 shadow-sm hover:shadow transition-all"
                          >
                            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
                              />
                            </svg>
                            <span className="hidden sm:inline">Send Interview</span>
                            <span className="sm:hidden">Send</span>
                          </button>
                        ) : (
                          <div className="flex items-center justify-end gap-2">
                            {ir.status === "link_expired" && (
                              <button
                                onClick={() => handleRefreshInvite(ir)}
                                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-red-600 bg-red-50 rounded-lg hover:bg-red-100 ring-1 ring-red-200 transition-all"
                              >
                                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                                  />
                                </svg>
                                <span className="hidden sm:inline">Resend</span>
                              </button>
                            )}
                            {ir.status === "not_started" && ir.inviteUrl && (
                              <button
                                onClick={() => handleCopyUrl(ir)}
                                className={`inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg ring-1 transition-all ${
                                  copiedId === ir.id
                                    ? "text-emerald-700 bg-emerald-50 ring-emerald-200"
                                    : "text-slate-700 bg-white ring-slate-200 hover:bg-slate-50"
                                }`}
                                title={ir.inviteUrl}
                              >
                                {copiedId === ir.id ? (
                                  <>
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                    </svg>
                                    Copied
                                  </>
                                ) : (
                                  <>
                                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"
                                      />
                                    </svg>
                                    <span className="hidden sm:inline">Copy URL</span>
                                    <span className="sm:hidden">Copy</span>
                                  </>
                                )}
                              </button>
                            )}
                            {ir.status === "in_progress" && (
                              <span className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-amber-700 bg-amber-50 rounded-lg ring-1 ring-amber-200">
                                <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-pulse" />
                                In progress
                              </span>
                            )}
                            {ir.status === "completed" && (
                              <button
                                onClick={() => setResultsModalRequest(ir)}
                                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-emerald-700 bg-emerald-50 rounded-lg hover:bg-emerald-100 ring-1 ring-emerald-200 transition-all"
                              >
                                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                                  />
                                </svg>
                                <span className="hidden sm:inline">View Results</span>
                                <span className="sm:hidden">Results</span>
                              </button>
                            )}
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Send Interview Modal */}
      {sendModalApplicant && (
        <SendInterviewModal
          applicant={sendModalApplicant}
          onClose={() => setSendModalApplicant(null)}
          onSent={handleInterviewSent}
        />
      )}

      {/* Interview Results Modal */}
      {resultsModalRequest && (
        <InterviewResultsModal
          request={resultsModalRequest}
          applicant={applicants.find((a) => a.id === resultsModalRequest.applicantId)!}
          onClose={() => setResultsModalRequest(null)}
        />
      )}

      {/* Applicant Side Panel */}
      {selectedApplicantId &&
        (() => {
          const selectedApplicant = applicants.find((a) => a.id === selectedApplicantId);
          if (!selectedApplicant) return null;
          const selectedIndex = applicants.findIndex((a) => a.id === selectedApplicantId);
          const ir = getInterviewRequest(selectedApplicantId);
          return (
            <ApplicantSidePanel
              applicant={selectedApplicant}
              interviewRequest={ir}
              applicants={applicants}
              currentIndex={selectedIndex}
              onNavigate={(id) => setSelectedApplicantId(id)}
              onClose={() => setSelectedApplicantId(null)}
              onInterviewSent={handleInterviewSent}
              onRefreshInvite={handleRefreshInvite}
            />
          );
        })()}
    </div>
  );
}
