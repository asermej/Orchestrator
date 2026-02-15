"use client";

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

interface ApplicantItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

interface Props {
  request: InterviewRequestItem;
  applicant: ApplicantItem;
  onClose: () => void;
}

function ScoreIndicator({ score }: { score: number }) {
  let colorClass = "text-red-600 bg-red-50 border-red-200";
  if (score >= 70) colorClass = "text-green-700 bg-green-50 border-green-200";
  else if (score >= 40) colorClass = "text-amber-700 bg-amber-50 border-amber-200";

  return (
    <div className={`inline-flex items-center gap-2 px-4 py-2 rounded-lg border ${colorClass}`}>
      <span className="text-2xl font-bold">{score}</span>
      <span className="text-sm font-medium">/100</span>
    </div>
  );
}

function RecommendationBadge({ recommendation }: { recommendation: string }) {
  const config: Record<string, { label: string; className: string }> = {
    hire: { label: "Hire", className: "bg-green-100 text-green-800" },
    no_hire: { label: "No Hire", className: "bg-red-100 text-red-800" },
    further_review: { label: "Further Review", className: "bg-amber-100 text-amber-800" },
  };

  const c = config[recommendation] || { label: recommendation, className: "bg-slate-100 text-slate-700" };

  return (
    <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${c.className}`}>
      {c.label}
    </span>
  );
}

export function InterviewResultsModal({ request, applicant, onClose }: Props) {
  const orchestratorUrl = request.orchestratorInterviewId
    ? `http://localhost:3000/interviews/${request.orchestratorInterviewId}`
    : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />

      {/* Modal */}
      <div className="relative bg-white rounded-xl shadow-2xl w-full max-w-2xl mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <button
          onClick={onClose}
          className="absolute top-3 right-3 text-slate-400 hover:text-slate-600 text-xl leading-none"
        >
          &times;
        </button>

        <h2 className="text-lg font-semibold text-slate-900 mb-1">
          Interview Results
        </h2>
        <p className="text-sm text-slate-500 mb-5">
          {applicant.firstName} {applicant.lastName} ({applicant.email})
        </p>

        {/* Score and Recommendation */}
        <div className="flex items-center gap-4 mb-6">
          {request.score != null && (
            <ScoreIndicator score={request.score} />
          )}
          {request.resultRecommendation && (
            <RecommendationBadge recommendation={request.resultRecommendation} />
          )}
        </div>

        {/* Summary */}
        {request.resultSummary && (
          <div className="mb-5">
            <h3 className="text-sm font-semibold text-slate-700 mb-2">Summary</h3>
            <p className="text-sm text-slate-600 bg-slate-50 rounded-lg p-3 border border-slate-200">
              {request.resultSummary}
            </p>
          </div>
        )}

        {/* Strengths */}
        {request.resultStrengths && (
          <div className="mb-5">
            <h3 className="text-sm font-semibold text-green-700 mb-2">Strengths</h3>
            <p className="text-sm text-slate-600 bg-green-50 rounded-lg p-3 border border-green-200">
              {request.resultStrengths}
            </p>
          </div>
        )}

        {/* Areas for Improvement */}
        {request.resultAreasForImprovement && (
          <div className="mb-5">
            <h3 className="text-sm font-semibold text-amber-700 mb-2">Areas for Improvement</h3>
            <p className="text-sm text-slate-600 bg-amber-50 rounded-lg p-3 border border-amber-200">
              {request.resultAreasForImprovement}
            </p>
          </div>
        )}

        {/* Webhook Received */}
        {request.webhookReceivedAt && (
          <div className="mb-5 text-xs text-slate-400">
            Results received: {new Date(request.webhookReceivedAt).toLocaleString()}
          </div>
        )}

        {/* Link to Orchestrator */}
        {orchestratorUrl && (
          <div className="mb-5 p-3 bg-indigo-50 border border-indigo-200 rounded-lg">
            <p className="text-xs text-indigo-700 mb-1 font-medium">
              View full details (transcripts, recordings) in the Orchestrator:
            </p>
            <a
              href={orchestratorUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-indigo-600 hover:text-indigo-800 underline font-mono break-all"
            >
              {orchestratorUrl}
            </a>
          </div>
        )}

        {/* No results yet */}
        {!request.resultSummary && !request.score && (
          <div className="bg-slate-50 border border-slate-200 rounded-lg p-4 text-sm text-slate-500 text-center">
            Detailed results are not yet available. They will appear here once the Orchestrator sends them via webhook.
          </div>
        )}

        <div className="mt-6">
          <button
            onClick={onClose}
            className="w-full py-2 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
