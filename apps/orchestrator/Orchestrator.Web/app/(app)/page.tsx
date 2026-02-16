import { Header } from "@/components/header";
import { auth0 } from "@/lib/auth0";
import Link from "next/link";

export default async function HomePage() {
  // Get session on server
  const session = await auth0.getSession();

  // For logged-out users, show login prompt
  if (!session?.user) {
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

  // For logged-in users, show dashboard with quick-access cards
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-900">
          Welcome back, {session.user.name || session.user.email}
        </h1>
        <p className="text-slate-600 mt-2">
          Manage your AI agents, interview configurations, and interviews.
        </p>
      </div>
      
      <div className="grid md:grid-cols-3 gap-6">
        <Link
          href="/my-agents"
          className="bg-white rounded-xl p-6 border border-slate-200 hover:border-indigo-300 hover:shadow-lg transition-all group"
        >
          <div className="text-4xl mb-4">🎭</div>
          <h2 className="text-xl font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
            My Agents
          </h2>
          <p className="text-slate-600 mt-2">
            Create and manage AI agents for interviews and chatbots.
          </p>
        </Link>
        
        <Link
          href="/interview-guides"
          className="bg-white rounded-xl p-6 border border-slate-200 hover:border-indigo-300 hover:shadow-lg transition-all group"
        >
          <div className="text-4xl mb-4">📖</div>
          <h2 className="text-xl font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
            Interview Guides
          </h2>
          <p className="text-slate-600 mt-2">
            Create reusable question sets with opening/closing templates and scoring rubrics.
          </p>
        </Link>

        <Link
          href="/interview-configurations"
          className="bg-white rounded-xl p-6 border border-slate-200 hover:border-indigo-300 hover:shadow-lg transition-all group"
        >
          <div className="text-4xl mb-4">⚙️</div>
          <h2 className="text-xl font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
            Interview Configurations
          </h2>
          <p className="text-slate-600 mt-2">
            Configure AI agents and interview guides for different positions.
          </p>
        </Link>
        
        <Link
          href="/interviews"
          className="bg-white rounded-xl p-6 border border-slate-200 hover:border-indigo-300 hover:shadow-lg transition-all group"
        >
          <div className="text-4xl mb-4">🎙️</div>
          <h2 className="text-xl font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
            Interviews
          </h2>
          <p className="text-slate-600 mt-2">
            Run voice interviews and view session results.
          </p>
        </Link>

        <Link
          href="/jobs"
          className="bg-white rounded-xl p-6 border border-slate-200 hover:border-indigo-300 hover:shadow-lg transition-all group"
        >
          <div className="text-4xl mb-4">📋</div>
          <h2 className="text-xl font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
            Jobs
          </h2>
          <p className="text-slate-600 mt-2">
            View jobs synced from your ATS for use in interviews.
          </p>
        </Link>
      </div>
    </div>
  );
}
