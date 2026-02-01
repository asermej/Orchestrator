import { redirect } from "next/navigation";
import { Header } from "@/components/header";
import { auth0 } from "@/lib/auth0";
import { fetchPersonaById } from "../../../../actions";
import {
  fetchTopicDetails,
  fetchTopicTags,
  fetchPersonaTopicTraining,
  fetchAllTags,
  fetchCategories,
} from "./actions";
import { TrainClient } from "./train-client";

// Force dynamic rendering to prevent caching issues
export const dynamic = 'force-dynamic';
export const revalidate = 0;

interface PageProps {
  params: Promise<{ id: string; topicId: string }>;
}

export default async function TopicTrainPage({ params }: PageProps) {
  // Get session on server
  const session = await auth0.getSession();
  
  // Redirect to login if not authenticated
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  // Await params in Next.js 15+
  const { id: personaId, topicId } = await params;
  
  const isNewTopic = topicId === "new";

  // Fetch data on server in parallel
  if (isNewTopic) {
    // For new topics, only fetch persona and lookup data
    const [persona, categoriesData, allTagsData] = await Promise.all([
      fetchPersonaById(personaId),
      fetchCategories(),
      fetchAllTags(),
    ]);

    return (
      <div className="min-h-screen bg-background">
        <Header user={session.user} />
        <TrainClient
          personaId={personaId}
          topicId={topicId}
          displayName={persona.displayName}
          profileImageUrl={persona.profileImageUrl || ""}
          categories={categoriesData}
          allTags={allTagsData}
        />
      </div>
    );
  } else {
    // For existing topics, fetch all data in parallel
    const [persona, topicDetails, topicTags, trainingContent, categoriesData, allTagsData] = await Promise.all([
      fetchPersonaById(personaId),
      fetchTopicDetails(topicId),
      fetchTopicTags(topicId),
      fetchPersonaTopicTraining(personaId, topicId),
      fetchCategories(),
      fetchAllTags(),
    ]);
    
    // Merge contributionNotes from topicDetails into training data
    // (since we fetched them in parallel, training won't have it yet)
    const topicTraining = {
      ...trainingContent,
      contributionNotes: topicDetails.contributionNotes
    };

    return (
      <div className="min-h-screen bg-background">
        <Header user={session.user} />
        <TrainClient
          personaId={personaId}
          topicId={topicId}
          displayName={persona.displayName}
          profileImageUrl={persona.profileImageUrl || ""}
          categories={categoriesData}
          allTags={allTagsData}
          topicDetails={topicDetails}
          topicTags={topicTags}
          topicTraining={topicTraining}
        />
      </div>
    );
  }
}
