export default function HomePage() {
  return (
    <div>
      <h1 className="text-3xl font-bold text-slate-900 mb-4">Hireology Test ATS</h1>
      <p className="text-slate-600 mb-6">
        Test applicant tracking system for Orchestrator integration. Use the nav
        to manage jobs, applicants, settings, or open the public careers page.
      </p>
      <ul className="list-disc list-inside text-slate-600 space-y-2">
        <li>
          <a href="/jobs" className="text-indigo-600 hover:underline">
            Jobs
          </a>{" "}
          — manage and sync jobs to Orchestrator
        </li>
        <li>
          <a href="/applicants" className="text-indigo-600 hover:underline">
            Applicants
          </a>{" "}
          — manage and sync applicants
        </li>
        <li>
          <a href="/settings" className="text-indigo-600 hover:underline">
            Settings
          </a>{" "}
          — Orchestrator connection (API URL, API key)
        </li>
        <li>
          <a href="/careers" className="text-indigo-600 hover:underline">
            Careers
          </a>{" "}
          — public career site with chatbot (for testing the widget)
        </li>
      </ul>
    </div>
  );
}
