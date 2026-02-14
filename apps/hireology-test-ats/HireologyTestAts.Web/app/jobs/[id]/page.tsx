"use client";

import { useEffect, useState, useCallback } from "react";
import { useParams } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";

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

export default function JobDetailPage() {
  const params = useParams<{ id: string }>();
  const jobId = params.id;

  const [job, setJob] = useState<JobDetail | null>(null);
  const [applicants, setApplicants] = useState<ApplicantItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [jobData, applicantData] = await Promise.all([
        testAtsApi.get<JobDetail>(`/api/v1/jobs/${jobId}`),
        testAtsApi.get<ApplicantItem[]>(`/api/v1/jobs/${jobId}/applicants`),
      ]);
      setJob(jobData);
      setApplicants(applicantData);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load job");
    } finally {
      setLoading(false);
    }
  }, [jobId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

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
        {job.description && (
          <p className="mt-3 text-sm text-slate-600">{job.description}</p>
        )}
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
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 bg-white">
                {applicants.map((applicant) => (
                  <tr key={applicant.id}>
                    <td className="px-4 py-3 text-sm text-slate-900">
                      {applicant.firstName} {applicant.lastName}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {applicant.email}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-600">
                      {applicant.phone || "—"}
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
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
