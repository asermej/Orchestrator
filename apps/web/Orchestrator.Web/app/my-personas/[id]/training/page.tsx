import { redirect } from "next/navigation";
import { Header } from "@/components/header";
import { auth0 } from "@/lib/auth0";
import { fetchPersonaById } from "../../actions";
import {
  fetchPersonaTopicsWithDetails,
  fetchCategories,
  fetchTags,
} from "./actions";
import { TrainingHubClient } from "./training-hub-client";

interface PageProps {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ onboarding?: string }>;
}

export default async function TrainingHubPage({ params, searchParams }: PageProps) {
  // Get session on server using custom Auth0 client
  const session = await auth0.getSession();
  
  // Redirect to login if not authenticated
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  // Await params and searchParams in Next.js 15+
  const { id: personaId } = await params;
  const { onboarding } = await searchParams;
  const isOnboarding = onboarding === "true";

  // Fetch all data in parallel on the server - single request
  const [persona, categoriesData, tagsData] = await Promise.all([
    fetchPersonaById(personaId),
    fetchCategories(),
    fetchTags(),
  ]);

  // Fetch topics with categories already loaded to avoid duplicate fetch
  const topics = await fetchPersonaTopicsWithDetails(personaId, categoriesData);

  // Filter active categories
  const activeCategories = categoriesData.filter((c) => c.isActive);

  return (
    <div className="min-h-screen bg-background">
      <Header user={session.user} />
      <TrainingHubClient
        personaId={personaId}
        displayName={persona.displayName}
        profileImageUrl={persona.profileImageUrl || ""}
        initialTopics={topics}
        categories={activeCategories}
        allTags={tagsData}
        isOnboarding={isOnboarding}
      />
    </div>
  );
}

