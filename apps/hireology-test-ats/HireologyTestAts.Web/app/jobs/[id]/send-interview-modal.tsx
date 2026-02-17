"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface AgentItem {
  id: string;
  displayName: string;
  profileImageUrl?: string | null;
}

interface InterviewGuideItem {
  id: string;
  name: string;
  description?: string | null;
  questionCount: number;
  isActive: boolean;
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
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [guides, setGuides] = useState<InterviewGuideItem[]>([]);
  const [selectedAgentId, setSelectedAgentId] = useState<string>("");
  const [selectedGuideId, setSelectedGuideId] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<InterviewRequestItem | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function loadData() {
      try {
        const [agentsData, guidesData] = await Promise.all([
          testAtsApi.get<AgentItem[]>("/api/v1/agents"),
          testAtsApi.get<InterviewGuideItem[]>("/api/v1/interview-guides"),
        ]);
        if (!cancelled) {
          setAgents(agentsData);
          setGuides(guidesData);
          if (agentsData.length > 0) setSelectedAgentId(agentsData[0].id);
          if (guidesData.length > 0) setSelectedGuideId(guidesData[0].id);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load agents and interview guides");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    loadData();
    return () => { cancelled = true; };
  }, []);

  const selectedGuide = guides.find((g) => g.id === selectedGuideId);

  const handleSend = async () => {
    if (!selectedAgentId || !selectedGuideId) return;
    setSending(true);
    setError(null);
    try {
      const data = await testAtsApi.post<InterviewRequestItem>(
        `/api/v1/applicants/${applicant.id}/interview`,
        { agentId: selectedAgentId, interviewGuideId: selectedGuideId }
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

  const canSend = selectedAgentId && selectedGuideId && agents.length > 0 && guides.length > 0;

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
            {loading ? (
              <div className="text-sm text-slate-500 mb-4">Loading agents and interview guides...</div>
            ) : agents.length === 0 || guides.length === 0 ? (
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-4 text-amber-800 text-sm">
                {agents.length === 0 && "No agents found. "}
                {guides.length === 0 && "No interview guides found. "}
                Please create agents and interview guides in the Orchestrator first.
              </div>
            ) : (
              <>
                {/* Agent selector */}
                <div className="mb-4">
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">
                    Agent
                  </label>
                  <select
                    value={selectedAgentId}
                    onChange={(e) => setSelectedAgentId(e.target.value)}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                  >
                    {agents.map((agent) => (
                      <option key={agent.id} value={agent.id}>
                        {agent.displayName}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Interview Guide selector */}
                <div className="mb-5">
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">
                    Interview Guide
                  </label>
                  <select
                    value={selectedGuideId}
                    onChange={(e) => setSelectedGuideId(e.target.value)}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                  >
                    {guides.map((guide) => (
                      <option key={guide.id} value={guide.id}>
                        {guide.name} {`\u2014 ${guide.questionCount} questions`}
                      </option>
                    ))}
                  </select>

                  {selectedGuide && selectedGuide.description && (
                    <div className="mt-2 p-2 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-600">
                      {selectedGuide.description}
                    </div>
                  )}
                </div>
              </>
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
                disabled={sending || !canSend}
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
