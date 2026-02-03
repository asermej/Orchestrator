import { auth0 } from "@/lib/auth0";
import { redirect } from "next/navigation";
import { ChatClient } from "./chat-client";
import {
  fetchAgentById,
  fetchChats,
  fetchAgentTopics,
  fetchAllTopics,
  fetchAllCategories,
  fetchAllTags,
  fetchAgentCategories,
  fetchChatMessages,
  fetchChatTopics,
} from "./actions";

export default async function AgentChatPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ chatId?: string; topicId?: string }>;
}) {
  // 1. Get authenticated user
  const session = await auth0.getSession();
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  // Await params and searchParams (Next.js 15 requirement)
  const { id: agentId } = await params;
  const { chatId, topicId } = await searchParams;
  const userId = session.user.sub;
  const chatIdFromUrl = chatId || null;
  const topicIdFromUrl = topicId || null;

  try {
    // 2. Fetch all initial data in parallel
    const [
      agent,
      chatsResponse,
      agentTopics,
      allTopics,
      categories,
      tags,
      agentCategories,
    ] = await Promise.all([
      fetchAgentById(agentId),
      fetchChats(agentId, userId),
      fetchAgentTopics(agentId),
      fetchAllTopics(),
      fetchAllCategories(),
      fetchAllTags(),
      fetchAgentCategories(agentId),
    ]);

    // 3. Sort chats by last message
    const chats = chatsResponse.items.sort(
      (a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
    );

    // 4. Load chat topics for sidebar badges (in parallel)
    const chatIds = chats.map(c => c.id);
    const chatTopicsArray = await Promise.all(
      chatIds.map(async (chatId) => {
        try {
          const topics = await fetchChatTopics(chatId);
          return { chatId, topics };
        } catch {
          return { chatId, topics: [] };
        }
      })
    );
    
    const chatTopicsMap = new Map(
      chatTopicsArray.map(({ chatId, topics }) => [chatId, topics])
    );

    // 5. Load current chat data if chatId in URL
    let initialMessages = undefined;
    let initialLoadedTopics = undefined;
    if (chatIdFromUrl) {
      const chat = chats.find(c => c.id === chatIdFromUrl);
      if (chat) {
        const [messagesResponse, chatTopics] = await Promise.all([
          fetchChatMessages(chat.id),
          fetchChatTopics(chat.id),
        ]);
        initialMessages = messagesResponse.items;
        initialLoadedTopics = chatTopics;
      }
    }

    // 6. Render client component with all data
    return (
      <ChatClient
        user={session.user}
        agentId={agentId}
        initialAgent={agent}
        initialChats={chats}
        initialChatTopicsMap={chatTopicsMap}
        initialAgentTopics={agentTopics}
        initialAllTopics={allTopics}
        initialCategories={categories}
        initialTags={tags}
        initialAgentCategories={agentCategories}
        chatIdFromUrl={chatIdFromUrl}
        topicIdFromUrl={topicIdFromUrl}
        initialMessages={initialMessages}
        initialLoadedTopics={initialLoadedTopics}
      />
    );
  } catch (error) {
    // Handle 404 or other errors - redirect to agents list
    console.error("Error loading agent chat page:", error);
    redirect("/personas");
  }
}
