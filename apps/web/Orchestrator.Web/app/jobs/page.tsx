import { auth0 } from "@/lib/auth0";
import { redirect } from "next/navigation";
import { Header } from "@/components/header";
import { JobsList } from "./jobs-list";

export default async function JobsPage() {
  const session = await auth0.getSession();
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Header user={session.user} />
      <main className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-slate-900">Jobs</h1>
          <p className="text-slate-600 mt-1">
            Jobs synced from your ATS. Create and manage jobs in your ATS, then sync them here to use in interviews.
          </p>
        </div>
        <JobsList />
      </main>
    </div>
  );
}
