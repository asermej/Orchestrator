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
      // Check if text overflows 3 lines (~4.5em at text-sm line-height)
      setClamped(ref.current.scrollHeight > ref.current.clientHeight);
    }
  }, [text]);

  return (
    <div className="mt-3">
      <p
        ref={ref}
        className={`text-sm text-slate-600 whitespace-pre-line ${expanded ? "" : "line-clamp-3"}`}
      >
        {text}
      </p>
      {clamped && (
        <button
          onClick={() => setExpanded((prev) => !prev)}
          className="mt-1 text-xs font-medium text-indigo-600 hover:text-indigo-800"
        >
          {expanded ? "Show less" : "Show more"}
        </button>
      )}
    </div>
  );
}

function StatusBadge({ status, score }: { status: string; score?: number | null }) {
  const config: Record<string, { label: string; className: string }> = {
    not_started: {
      label: "Not Started",
      className: "bg-slate-100 text-slate-700",
    },
    in_progress: {
      label: "In Progress",
      className: "bg-amber-100 text-amber-800",
    },
    completed: {
      label: score != null ? `Completed (${score}/100)` : "Completed",
      className: "bg-green-100 text-green-800",
    },
    link_expired: {
      label: "Link Expired",
      className: "bg-red-100 text-red-800",
    },
  };

  const c = config[status] || { label: status, className: "bg-slate-100 text-slate-600" };

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${c.className}`}>
      {c.label}
    </span>
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

  // Modal state
  const [sendModalApplicant, setSendModalApplicant] = useState<ApplicantItem | null>(null);
  const [resultsModalRequest, setResultsModalRequest] = useState<InterviewRequestItem | null>(null);

  // Side panel state
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
      setInterviewRequests((prev) =>
        prev.map((r) => (r.id === ir.id ? updated : r))
      );
    } catch (e) {
      alert(e instanceof Error ? e.message : "Failed to refresh invite");
    }
  };

  if (loading) {
    return <p className="text-slate-500">Loading...</p>;
  }

  if (error || !job) {
    return (
      <div>
        <a href="/jobs" className="text-indigo-600 hover:text-indigo-800 text-sm">
          &larr; Back to Jobs
        </a>
        <div className="mt-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800 text-sm">
          {error || "Job not found"}
        </div>
      </div>
    );
  }

  return (
    <div>
      <a href="/jobs" className="text-indigo-600 hover:text-indigo-800 text-sm">
        &larr; Back to Jobs
      </a>

      {/* Job details */}
      <div className="mt-4 mb-8">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">{job.title}</h1>
            <div className="flex items-center gap-3 mt-1 text-sm text-slate-500">
              <span>ID: {job.externalJobId}</span>
              {job.location && (
                <>
                  <span className="text-slate-300">|</span>
                  <span>{job.location}</span>
                </>
              )}
              <span className="text-slate-300">|</span>
              <span
                className={
                  job.status === "active" ? "text-green-600" : "text-slate-500"
                }
              >
                {job.status}
              </span>
            </div>
          </div>
        </div>
        {job.description && <JobDescription text={job.description} />}
      </div>

      {/* Applicants */}
      <div>
        <h2 className="text-lg font-semibold text-slate-900 mb-4">
          Applicants ({applicants.length})
        </h2>

        {applicants.length === 0 ? (
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-8 text-center text-slate-600">
            No one has applied for this job yet.
          </div>
        ) : (
          <div className="rounded-lg border border-slate-200 overflow-hidden">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Name
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Email
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Phone
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Applied
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Interview
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 bg-white">
                {applicants.map((applicant) => {
                  const ir = getInterviewRequest(applicant.id);
                  return (
                    <tr
                      key={applicant.id}
                      onClick={() => setSelectedApplicantId(applicant.id)}
                      className="cursor-pointer hover:bg-slate-50 transition-colors"
                    >
                      <td className="px-4 py-3 text-sm text-slate-900">
                        {applicant.firstName} {applicant.lastName}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-600">
                        {applicant.email}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-600">
                        {applicant.phone || "\u2014"}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-500">
                        {new Date(applicant.createdAt).toLocaleDateString("en-US", {
                          month: "short",
                          day: "numeric",
                          year: "numeric",
                          hour: "numeric",
                          minute: "2-digit",
                        })}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {ir ? (
                          <StatusBadge status={ir.status} score={ir.score} />
                        ) : (
                          <span className="text-slate-400 text-xs">No interview</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center gap-2">
                          {!ir ? (
                            <button
                              onClick={() => setSendModalApplicant(applicant)}
                              className="text-indigo-600 hover:text-indigo-800 font-medium text-xs"
                            >
                              Send Interview
                            </button>
                          ) : (
                            <>
                              {ir.status === "link_expired" && (
                                <button
                                  onClick={() => handleRefreshInvite(ir)}
                                  className="text-red-600 hover:text-red-800 font-medium text-xs"
                                >
                                  Resend Link
                                </button>
                              )}
                              {ir.status === "not_started" && ir.inviteUrl && (
                                <button
                                  onClick={() => {
                                    navigator.clipboard.writeText(ir.inviteUrl!);
                                  }}
                                  className="text-indigo-600 hover:text-indigo-800 font-medium text-xs"
                                  title={ir.inviteUrl}
                                >
                                  Copy URL
                                </button>
                              )}
                              {ir.status === "in_progress" && (
                                <span className="text-xs text-amber-600">Interview in progress</span>
                              )}
                              {ir.status === "completed" && (
                                <button
                                  onClick={() => setResultsModalRequest(ir)}
                                  className="text-green-600 hover:text-green-800 font-medium text-xs"
                                >
                                  View Results
                                </button>
                              )}
                            </>
                          )}
                        </div>
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
      {selectedApplicantId && (() => {
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
