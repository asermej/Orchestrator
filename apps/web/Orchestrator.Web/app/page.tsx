import { Header } from "@/components/header";
import { auth0 } from "@/lib/auth0";
import {
  fetchTopicFeed,
  fetchPersonaFeed,
  fetchCategories,
  fetchPopularTags,
} from "./actions";
import { HomeClient } from "./home-client";
import { MarketingLanding } from "./marketing-landing";

export default async function HomePage() {
  // Get session on server (optional for home page - can be anonymous)
  const session = await auth0.getSession();

  // Fetch initial data on server in parallel
  const [initialTopics, initialPersonas, categories, tags] = await Promise.all([
    fetchTopicFeed(1, 10), // First page of topics
    fetchPersonaFeed(1, 10, ""), // First page of personas
    fetchCategories(),
    fetchPopularTags(50),
  ]);

  // For logged-out users, show marketing landing page
  if (!session?.user) {
    return (
      <div className="min-h-screen bg-background">
        <Header user={null} />
        <MarketingLanding
          personas={initialPersonas.items}
          categories={categories}
          totalPersonaCount={initialPersonas.totalCount}
          totalChatCount={50000} // Placeholder - could fetch from API if needed
        />
      </div>
    );
  }

  // For logged-in users, show the existing feed
  return (
    <div className="min-h-screen bg-background">
      <Header user={session.user} />
      <HomeClient
        user={session.user}
        initialTopics={initialTopics}
        initialPersonas={initialPersonas}
        categories={categories}
        tags={tags}
      />
    </div>
  );
}
