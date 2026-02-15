import { CandidateInterviewClient } from "./candidate-interview-client";

interface CandidateInterviewPageProps {
  params: Promise<{ code: string }>;
}

async function redeemInvite(shortCode: string) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const response = await fetch(`${apiUrl}/api/v1/candidate/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ shortCode }),
    cache: "no-store",
  });

  if (!response.ok) {
    return { error: response.status, data: null };
  }

  const data = await response.json();
  return { error: null, data };
}

export default async function CandidateInterviewPage({
  params,
}: CandidateInterviewPageProps) {
  const { code } = await params;
  const { error, data } = await redeemInvite(code);

  if (error || !data) {
    return <InviteErrorPage status={error ?? 500} />;
  }

  return (
    <CandidateInterviewClient
      interview={data.interview}
      agent={data.agent}
      job={data.job}
      applicant={data.applicant}
      questions={data.questions}
      token={data.token}
    />
  );
}

function InviteErrorPage({ status }: { status: number }) {
  let title = "Something went wrong";
  let message = "An unexpected error occurred. Please try again later.";

  if (status === 404) {
    title = "Interview Not Found";
    message =
      "This interview link is invalid. Please check the link and try again.";
  } else if (status === 410 || status === 400) {
    title = "Link Expired or Unavailable";
    message =
      "This interview link has expired, been revoked, or has reached its usage limit. Please contact your recruiter for a new link.";
  }

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center p-8 max-w-md">
        <div className="mb-6">
          <svg
            className="mx-auto h-16 w-16 text-slate-500"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1.5}
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"
            />
          </svg>
        </div>
        <h1 className="text-3xl font-bold text-white mb-4">{title}</h1>
        <p className="text-slate-400">{message}</p>
      </div>
    </div>
  );
}
