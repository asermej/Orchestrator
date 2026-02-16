import { auth0 } from "@/lib/auth0";
import { redirect } from "next/navigation";
import { JobsList } from "./jobs-list";

export default async function JobsPage() {
  const session = await auth0.getSession();
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  return (
    <div className="mx-auto px-6 py-6">
      <JobsList />
    </div>
  );
}
