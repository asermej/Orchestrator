"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface InterviewConfiguration {
  id: string;
  name: string;
  description?: string | null;
  agentId: string;
  agentDisplayName?: string | null;
  questionCount: number;
}

interface ApplicantItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
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

interface Props {
  applicant: ApplicantItem;
  onClose: () => void;
  onSent: (request: InterviewRequestItem) => void;
}

export function SendInterviewModal({ applicant, onClose, onSent }: Props) {
  const [configurations, setConfigurations] = useState<InterviewConfiguration[]>([]);
  const [selectedConfigId, setSelectedConfigId] = useState<string>("");
  const [loadingConfigs, setLoadingConfigs] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<InterviewRequestItem | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function loadConfigurations() {
      try {
        const data = await testAtsApi.get<InterviewConfiguration[]>("/api/v1/configurations");
        if (!cancelled) {
          setConfigurations(data);
          if (data.length > 0) {
            setSelectedConfigId(data[0].id);
          }
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load interview configurations");
        }
      } finally {
        if (!cancelled) setLoadingConfigs(false);
      }
    }
    loadConfigurations();
    return () => { cancelled = true; };
  }, []);

  const selectedConfig = configurations.find((c) => c.id === selectedConfigId);

  const handleSend = async () => {
    if (!selectedConfigId) return;
    setSending(true);
    setError(null);
    try {
      const data = await testAtsApi.post<InterviewRequestItem>(
        `/api/v1/applicants/${applicant.id}/interview`,
        { interviewConfigurationId: selectedConfigId }
      );
      setResult(data);
      onSent(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to send interview request");
    } finally {
      setSending(false);
    }
  };

  const handleCopy = () => {
    if (result?.inviteUrl) {
      navigator.clipboard.writeText(result.inviteUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />

      {/* Modal */}
      <div className="relative bg-white rounded-xl shadow-2xl w-full max-w-md mx-4 p-6">
        <button
          onClick={onClose}
          className="absolute top-3 right-3 text-slate-400 hover:text-slate-600 text-xl leading-none"
        >
          &times;
        </button>

        <h2 className="text-lg font-semibold text-slate-900 mb-1">
          Send Interview Request
        </h2>
        <p className="text-sm text-slate-500 mb-5">
          {applicant.firstName} {applicant.lastName} ({applicant.email})
        </p>

        {/* Show result if interview was created */}
        {result ? (
          <div>
            <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-4">
              <p className="text-green-800 font-medium text-sm mb-2">
                Interview request sent successfully!
              </p>
              <p className="text-xs text-green-700 mb-3">
                Share this URL with the candidate to start the interview:
              </p>
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  readOnly
                  value={result.inviteUrl || ""}
                  className="flex-1 text-xs bg-white border border-green-300 rounded px-3 py-2 text-slate-800 font-mono"
                />
                <button
                  onClick={handleCopy}
                  className="px-3 py-2 bg-green-600 text-white text-xs font-medium rounded hover:bg-green-700 transition-colors whitespace-nowrap"
                >
                  {copied ? "Copied!" : "Copy"}
                </button>
              </div>
            </div>
            <button
              onClick={onClose}
              className="w-full py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors"
            >
              Close
            </button>
          </div>
        ) : (
          <div>
            {/* Interview configuration selector */}
            {loadingConfigs ? (
              <div className="text-sm text-slate-500 mb-4">Loading interview configurations...</div>
            ) : configurations.length === 0 ? (
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-4 text-amber-800 text-sm">
                No interview configurations found. Please create an interview configuration in the Orchestrator first
                (with an agent and questions).
              </div>
            ) : (
              <div className="mb-5">
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Interview Configuration
                </label>
                <select
                  value={selectedConfigId}
                  onChange={(e) => setSelectedConfigId(e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                >
                  {configurations.map((config) => (
                    <option key={config.id} value={config.id}>
                      {config.name}
                      {config.agentDisplayName ? ` (${config.agentDisplayName})` : ""}
                      {` \u2014 ${config.questionCount} questions`}
                    </option>
                  ))}
                </select>

                {/* Selected configuration detail */}
                {selectedConfig && (
                  <div className="mt-3 p-3 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-600 space-y-1">
                    {selectedConfig.description && (
                      <p>{selectedConfig.description}</p>
                    )}
                    <div className="flex items-center gap-3">
                      {selectedConfig.agentDisplayName && (
                        <span>
                          <span className="font-medium text-slate-700">Agent:</span>{" "}
                          {selectedConfig.agentDisplayName}
                        </span>
                      )}
                      <span>
                        <span className="font-medium text-slate-700">Questions:</span>{" "}
                        {selectedConfig.questionCount}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            )}

            {error && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-3 mb-4 text-red-700 text-sm">
                {error}
              </div>
            )}

            <div className="flex gap-3">
              <button
                onClick={onClose}
                className="flex-1 py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleSend}
                disabled={sending || !selectedConfigId || configurations.length === 0}
                className="flex-1 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {sending ? "Sending..." : "Send Interview"}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
