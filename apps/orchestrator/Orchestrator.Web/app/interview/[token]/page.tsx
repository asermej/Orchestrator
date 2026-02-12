import { InterviewClient } from "./interview-client";

interface InterviewPageProps {
  params: Promise<{ token: string }>;
}

async function getInterviewData(token: string) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
  
  const response = await fetch(`${apiUrl}/api/v1/interview/by-token/${token}`, {
    cache: "no-store",
  });

  if (!response.ok) {
    if (response.status === 404) {
      return null;
    }
    throw new Error(`Failed to fetch interview: ${response.status}`);
  }

  return response.json();
}

export default async function InterviewPage({ params }: InterviewPageProps) {
  const { token } = await params;
  const interview = await getInterviewData(token);

  if (!interview) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
        <div className="text-center p-8">
          <h1 className="text-3xl font-bold text-white mb-4">Interview Not Found</h1>
          <p className="text-slate-400">
            This interview link may have expired or is invalid.
          </p>
        </div>
      </div>
    );
  }

  if (interview.status === "completed") {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
        <div className="text-center p-8 max-w-md">
          <h1 className="text-3xl font-bold text-white mb-4">Interview Completed</h1>
          <p className="text-slate-400 mb-6">
            Thank you for completing your interview. The results have been submitted.
          </p>
          {interview.result && (
            <div className="bg-slate-800/50 rounded-lg p-6 text-left">
              <h2 className="text-lg font-semibold text-white mb-2">Summary</h2>
              <p className="text-slate-300 text-sm">{interview.result.summary}</p>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <InterviewClient
      token={token}
      interview={interview}
    />
  );
}
