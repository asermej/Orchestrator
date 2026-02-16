import { redirect } from "next/navigation";
import { auth0 } from "@/lib/auth0";
import { Header } from "@/components/header";
import Link from "next/link";

export default async function AppRootPage() {
  const session = await auth0.getSession();

  // Logged-in users go straight to My Agents (default tab)
  if (session?.user) {
    redirect("/my-agents");
  }

  // Logged-out users see the marketing landing
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      <Header user={null} />
      <main className="container mx-auto px-4 py-24">
        <div className="max-w-4xl mx-auto text-center">
          <h1 className="text-5xl md:text-6xl font-bold text-white mb-6">
            Hireology AI Interviewer
          </h1>
          <p className="text-xl text-slate-300 mb-12 max-w-2xl mx-auto">
            Conduct AI-powered voice interviews and chat with applicants using
            customizable agents. Streamline your hiring process with intelligent automation.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <a
              href="/api/auth/login"
              className="inline-flex items-center justify-center px-8 py-4 text-lg font-semibold text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 transition-colors"
            >
              Sign In to Get Started
            </a>
          </div>

          <div className="mt-24 grid md:grid-cols-3 gap-8 text-left">
            <div className="bg-slate-800/50 rounded-xl p-6 border border-slate-700">
              <div className="text-3xl mb-4">🎙️</div>
              <h3 className="text-xl font-semibold text-white mb-2">Voice Interviews</h3>
              <p className="text-slate-400">
                AI-powered voice interviews that follow your custom question sets and provide natural follow-up conversations.
              </p>
            </div>
            <div className="bg-slate-800/50 rounded-xl p-6 border border-slate-700">
              <div className="text-3xl mb-4">🤖</div>
              <h3 className="text-xl font-semibold text-white mb-2">AI Agents</h3>
              <p className="text-slate-400">
                Create and customize AI agents with unique voices and personalities for different interview scenarios.
              </p>
            </div>
            <div className="bg-slate-800/50 rounded-xl p-6 border border-slate-700">
              <div className="text-3xl mb-4">📊</div>
              <h3 className="text-xl font-semibold text-white mb-2">Interview Analytics</h3>
              <p className="text-slate-400">
                Get detailed transcripts, AI analysis, and insights from every interview to make better hiring decisions.
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
