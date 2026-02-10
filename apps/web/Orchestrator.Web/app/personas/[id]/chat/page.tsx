import { auth0 } from "@/lib/auth0";
import { redirect } from "next/navigation";
import { ChatClient } from "./chat-client";
import {
  fetchAgentById,
  fetchChats,
  fetchAllCategories,
  fetchAgentCategories,
  fetchChatMessages,
} from "./actions";

export default async function AgentChatPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ chatId?: string }>;
}) {
  // 1. Get authenticated user
  const session = await auth0.getSession();
  if (!session?.user) {
    redirect("/api/auth/login");
  }

  // Await params and searchParams (Next.js 15 requirement)
  const { id: agentId } = await params;
  const { chatId } = await searchParams;
  const userId = session.user.sub;
  const chatIdFromUrl = chatId || null;

  try {
    // 2. Fetch all initial data in parallel
    const [
      agent,
      chatsResponse,
      categories,
      agentCategories,
    ] = await Promise.all([
      fetchAgentById(agentId),
      fetchChats(agentId, userId),
      fetchAllCategories(),
      fetchAgentCategories(agentId),
    ]);

    // 3. Sort chats by last message
    const chats = chatsResponse.items.sort(
      (a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
    );

    // 4. Load current chat data if chatId in URL
    let initialMessages = undefined;
    if (chatIdFromUrl) {
      const chat = chats.find(c => c.id === chatIdFromUrl);
      if (chat) {
        const messagesResponse = await fetchChatMessages(chat.id);
        initialMessages = messagesResponse.items;
      }
    }

    // 6. Render client component with all data
    return (
      <ChatClient
        user={session.user}
        agentId={agentId}
        initialAgent={agent}
        initialChats={chats}
        initialCategories={categories}
        initialAgentCategories={agentCategories}
        chatIdFromUrl={chatIdFromUrl}
        initialMessages={initialMessages}
      />
    );
  } catch (error) {
    // Handle 404 or other errors - redirect to agents list
    console.error("Error loading agent chat page:", error);
    redirect("/personas");
  }
}
