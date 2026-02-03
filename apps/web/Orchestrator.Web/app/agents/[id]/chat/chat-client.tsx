"use client";

import { useEffect, useState, useRef } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { AgentAvatar } from "@/components/agent-avatar";
import { Header } from "@/components/header";
import { ArrowLeft, Send, Loader2, MessageSquarePlus, MessageSquare, Menu, Edit2, Check, X, Trash2, BookMarked, FolderOpen, Search, Mic } from "lucide-react";
import { VoiceConversationMode } from "@/components/voice-conversation-mode";
import { MessagePlayButton } from "@/components/message-play-button";
import Link from "next/link";
import {
  fetchChatMessages,
  sendMessage,
  updateChatTitle,
  deleteChat,
  fetchChatTopics,
  addTopicToChat,
  createChat,
  Chat,
  Message,
  TopicItem,
  Category,
  AgentCategory,
  TagItem,
} from "./actions";
import { AgentItem } from "../../actions";
import { ScrollArea } from "@/components/ui/scroll-area";
import { cn } from "@/lib/utils";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useServerAction } from "@/lib/use-server-action";

interface ChatClientProps {
  user: any;
  agentId: string;
  initialAgent: AgentItem;
  initialChats: Chat[];
  initialChatTopicsMap: Map<string, TopicItem[]>;
  initialAgentTopics: TopicItem[];
  initialAllTopics: TopicItem[];
  initialCategories: Category[];
  initialTags: TagItem[];
  initialAgentCategories: AgentCategory[];
  chatIdFromUrl: string | null;
  topicIdFromUrl: string | null;
  initialMessages?: Message[];
  initialLoadedTopics?: TopicItem[];
}

export function ChatClient({
  user,
  agentId,
  initialAgent,
  initialChats,
  initialChatTopicsMap,
  initialAgentTopics,
  initialAllTopics,
  initialCategories,
  initialTags,
  initialAgentCategories,
  chatIdFromUrl,
  topicIdFromUrl,
  initialMessages,
  initialLoadedTopics,
}: ChatClientProps) {
  const router = useRouter();

  const [agent] = useState<AgentItem>(initialAgent);
  const [currentChat, setCurrentChat] = useState<Chat | null>(null);
  const [pastChats, setPastChats] = useState<Chat[]>(initialChats);
  const [messages, setMessages] = useState<Message[]>(initialMessages || []);
  const [inputMessage, setInputMessage] = useState("");
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [editedTitle, setEditedTitle] = useState("");
  const [editingChatId, setEditingChatId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [chatToDelete, setChatToDelete] = useState<Chat | null>(null);
  const [isVoiceModeOpen, setIsVoiceModeOpen] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Server actions for mutations
  const { execute: executeSendMessage, isLoading: isSending } = useServerAction(
    async () => {
      if (!currentChat) throw new Error("No active chat");
      
      const messageContent = inputMessage;
      setInputMessage("");
      
      await sendMessage(currentChat.id, messageContent);
      
      // Reload messages to get both user message and AI response
      const messagesResponse = await fetchChatMessages(currentChat.id);
      setMessages(messagesResponse.items);
      
      // Update the lastMessageAt for the current chat
      setPastChats((prev) =>
        prev.map((chat) =>
          chat.id === currentChat.id
            ? { ...chat, lastMessageAt: new Date().toISOString() }
            : chat
        ).sort(
          (a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
        )
      );
    },
    {
      onError: () => {
        // Restore input on error so user can retry
        setInputMessage(inputMessage);
      },
    }
  );

  const { execute: executeUpdateTitle, isLoading: isUpdatingTitle } = useServerAction(
    async (chatId: string, newTitle: string) => {
      const updatedChat = await updateChatTitle(chatId, newTitle);
      
      // Update current chat if it matches
      if (currentChat?.id === chatId) {
        setCurrentChat(updatedChat);
      }
      
      // Update in past chats list
      setPastChats((prev) =>
        prev.map((chat) =>
          chat.id === chatId ? { ...chat, title: updatedChat.title } : chat
        )
      );
      
      setIsEditingTitle(false);
      setEditingChatId(null);
      setEditedTitle("");
    },
    {
      successMessage: "Chat title updated!",
    }
  );

  const { execute: executeDeleteChat, isLoading: isDeleting } = useServerAction(
    async () => {
      if (!chatToDelete) throw new Error("No chat selected for deletion");
      
      await deleteChat(chatToDelete.id);
      
      // Remove the deleted chat from the list
      setPastChats((prev) => prev.filter((chat) => chat.id !== chatToDelete.id));
      
      // If the deleted chat was the current chat, load another chat or create a new one
      if (currentChat?.id === chatToDelete.id) {
        const remainingChats = pastChats.filter((chat) => chat.id !== chatToDelete.id);
        if (remainingChats.length > 0) {
          await loadChat(remainingChats[0]);
        } else {
          await createNewChat();
        }
      }
      
      setDeleteDialogOpen(false);
      setChatToDelete(null);
    },
    {
      successMessage: "Chat deleted successfully!",
    }
  );

  const { execute: executeCreateChatWithTopic, isLoading: isCreatingChat } = useServerAction(
    async (topicId: string | null, chatName: string) => {
      if (!userId || !agentId || !agent) throw new Error("Session not ready");

      let newChat: Chat | null = null;
      
      try {
        // Create the chat
        newChat = await createChat(agentId, userId, chatName);
        setCurrentChat(newChat);
        setPastChats((prev) => [newChat, ...prev]);
        setMessages([]);
        
        // If a topic was selected, add it to the chat
        if (topicId) {
          try {
            await addTopicToChat(newChat.id, topicId);
            const chatTopics = await fetchChatTopics(newChat.id);
            setLoadedTopics(chatTopics);
            
            // Update the topics map for the sidebar
            setChatTopicsMap((prev) => {
              const newMap = new Map(prev);
              newMap.set(newChat.id, chatTopics);
              return newMap;
            });
          } catch (topicError: any) {
            // If adding topic fails (e.g., topic not trained), clean up the chat
            console.error("Failed to add topic to chat:", topicError);
            
            // Delete the newly created chat since we couldn't add the topic
            try {
              await deleteChat(newChat.id);
              setPastChats((prev) => prev.filter((chat) => chat.id !== newChat!.id));
              setCurrentChat(null);
            } catch (deleteError) {
              console.error("Failed to delete chat after topic addition failure:", deleteError);
            }
            
            // Re-throw with a more helpful message
            const errorMessage = topicError.message || "Failed to add topic to chat";
            if (errorMessage.includes("not been trained")) {
              throw new Error("This topic doesn't have any training content yet. Please train the agent on this topic first before starting a conversation.");
            }
            throw new Error(errorMessage);
          }
        } else {
          setLoadedTopics([]);
        }
        
        // Update URL with new chat ID
        router.replace(`/agents/${agentId}/chat?chatId=${newChat.id}`, { scroll: false });
        
        return newChat;
      } catch (error) {
        // If anything fails, ensure we don't have a partially created chat
        if (newChat) {
          try {
            await deleteChat(newChat.id);
            setPastChats((prev) => prev.filter((chat) => chat.id !== newChat!.id));
            setCurrentChat(null);
          } catch (deleteError) {
            console.error("Failed to clean up chat after error:", deleteError);
          }
        }
        throw error;
      }
    },
    {
      successMessage: "Chat created successfully!",
    }
  );

  const { execute: executeAddTopic, isLoading: isAddingTopic } = useServerAction(
    async (topicId: string) => {
      if (!currentChat) throw new Error("No active chat");
      
      await addTopicToChat(currentChat.id, topicId);
      
      const chatTopics = await fetchChatTopics(currentChat.id);
      setLoadedTopics(chatTopics);
    },
    {
      successMessage: "Topic added to chat!",
    }
  );

  // Topic management state
  const [loadedTopics, setLoadedTopics] = useState<TopicItem[]>(initialLoadedTopics || []);
  const [agentTopics] = useState<TopicItem[]>(initialAgentTopics);
  const [availableTopics] = useState<TopicItem[]>(initialAllTopics);
  const [allCategories] = useState<Category[]>(initialCategories);
  const [allTags] = useState<TagItem[]>(initialTags);
  const [agentCategories] = useState<AgentCategory[]>(initialAgentCategories);

  // Sidebar mode state
  const [sidebarMode, setSidebarMode] = useState<'chats' | 'topics'>('topics');
  
  // Track if we're creating a chat from URL to prevent duplicates
  const isCreatingChatFromUrl = useRef(false);
  
  // Topic filtering and sorting state
  const [topicSearchQuery, setTopicSearchQuery] = useState("");
  const [topicSortOrder, setTopicSortOrder] = useState<'chronological' | 'alphabetical'>('chronological');
  const [topicFilterCategory, setTopicFilterCategory] = useState<string | null>(null);
  const [topicFilterTags, setTopicFilterTags] = useState<string[]>([]);

  // Simplified chat creation state
  const [showChatNameDialog, setShowChatNameDialog] = useState(false);
  const [selectedTopicForNewChat, setSelectedTopicForNewChat] = useState<TopicItem | null>(null);
  const [newChatName, setNewChatName] = useState("");

  // Chat topics cache for sidebar
  const [chatTopicsMap, setChatTopicsMap] = useState<Map<string, TopicItem[]>>(initialChatTopicsMap);

  // Get user ID from Auth0 sub claim
  const userId = user?.sub;

  // Helper function: Generate default chat name based on topic
  const generateDefaultChatName = (topic?: TopicItem): string => {
    if (topic) {
      return `${topic.name} Discussion`;
    }
    return "New Conversation";
  };

  // Helper function: Filter and sort topics
  const filterAndSortTopics = (
    topics: TopicItem[],
    searchQuery: string,
    categoryFilter: string | null,
    tagFilter: string[],
    sortOrder: 'chronological' | 'alphabetical'
  ): TopicItem[] => {
    let filtered = [...topics];

    // Apply search filter (matches name or description)
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter((topic) => {
        const matchesName = topic.name.toLowerCase().includes(query);
        const matchesDescription = topic.description?.toLowerCase().includes(query) || false;
        
        return matchesName || matchesDescription;
      });
    }

    // Apply category filter
    if (categoryFilter) {
      filtered = filtered.filter((topic) => {
        return topic.categoryId === categoryFilter;
      });
    }

    // Apply tag filter (topics must have at least one of the selected tags)
    if (tagFilter.length > 0) {
      filtered = filtered.filter((topic) => {
        if (!topic.tags) return false;
        
        return topic.tags.some(tag => tagFilter.includes(tag.id));
      });
    }

    // Apply sort order
    filtered.sort((a, b) => {
      if (sortOrder === 'alphabetical') {
        return a.name.localeCompare(b.name);
      } else {
        // Chronological (newest first based on createdAt)
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
    });

    return filtered;
  };

  // Helper function: Create chat with optional topic
  const createChatWithTopic = async (topicId: string | null, chatName: string) => {
    await executeCreateChatWithTopic(topicId, chatName);
  };

  const createNewChat = async () => {
    await executeCreateChatWithTopic(null, "New Conversation");
  };

  // Handler: Select topic from sidebar (or General Discussion with null)
  const handleSelectTopicFromSidebar = (topic: TopicItem | null) => {
    setSelectedTopicForNewChat(topic);
    const defaultName = generateDefaultChatName(topic || undefined);
    setNewChatName(defaultName);
    setShowChatNameDialog(true);
  };

  // Handler: Create chat with selected name and topic
  const handleCreateChatWithName = async () => {
    if (!newChatName.trim()) return;
    
    setShowChatNameDialog(false);
    await createChatWithTopic(selectedTopicForNewChat?.id || null, newChatName.trim());
    
    // Switch back to chats mode and reset state
    setSidebarMode('chats');
    setSelectedTopicForNewChat(null);
    setNewChatName("");
  };

  const loadChat = async (chat: Chat) => {
    try {
      setIsLoadingMessages(true);
      setCurrentChat(chat);
      
      // Load messages and topics for this chat in parallel
      const [messagesResponse, chatTopics] = await Promise.all([
        fetchChatMessages(chat.id),
        fetchChatTopics(chat.id),
      ]);
      
      setMessages(messagesResponse.items);
      setLoadedTopics(chatTopics);
      
      // Update URL with current chat ID
      router.replace(`/agents/${agentId}/chat?chatId=${chat.id}`, { scroll: false });
    } catch (err) {
      console.error("Error loading chat:", err);
    } finally {
      setIsLoadingMessages(false);
    }
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!inputMessage.trim() || isSending || !currentChat) return;

    await executeSendMessage();
  };

  const handleStartEditTitle = () => {
    if (currentChat) {
      setEditedTitle(currentChat.title || "");
      setIsEditingTitle(true);
    }
  };

  const handleCancelEditTitle = () => {
    setIsEditingTitle(false);
    setEditedTitle("");
  };

  const handleSaveTitle = async () => {
    if (!currentChat || !editedTitle.trim()) return;
    await executeUpdateTitle(currentChat.id, editedTitle.trim());
  };

  const handleStartEditSidebarTitle = (chatId: string, currentTitle: string, e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent chat selection
    setEditingChatId(chatId);
    setEditedTitle(currentTitle || "");
  };

  const handleSaveSidebarTitle = async (chatId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (!editedTitle.trim()) {
      setEditingChatId(null);
      return;
    }

    await executeUpdateTitle(chatId, editedTitle.trim());
  };

  const handleCancelSidebarEdit = (e: React.MouseEvent) => {
    e.stopPropagation();
    setEditingChatId(null);
    setEditedTitle("");
  };

  const handleDeleteClick = (chat: Chat, e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent chat selection
    setChatToDelete(chat);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    await executeDeleteChat();
  };

  // Initial load effect - handle URL params and initial chat
  useEffect(() => {
    if (topicIdFromUrl && !isCreatingChatFromUrl.current) {
      // Handle topic selection from URL (from "Chat Now" button)
      // Automatically create chat and have agent kick off the conversation
      const selectedTopic = agentTopics.find(t => t.id === topicIdFromUrl);
      if (selectedTopic) {
        const defaultName = generateDefaultChatName(selectedTopic);
        
        // Mark that we're creating a chat to prevent duplicates
        isCreatingChatFromUrl.current = true;
        
        // Create chat immediately without showing dialog
        (async () => {
          try {
            // Create the chat with the topic - this returns the new chat and sets currentChat
            const newChat = await createChat(agentId, userId, defaultName);
            setCurrentChat(newChat);
            setPastChats((prev) => [newChat, ...prev]);
            setMessages([]);
            
            // Add the topic to the chat
            await addTopicToChat(newChat.id, selectedTopic.id);
            const chatTopics = await fetchChatTopics(newChat.id);
            setLoadedTopics(chatTopics);
            
            // Update the topics map for the sidebar
            setChatTopicsMap((prev) => {
              const newMap = new Map(prev);
              newMap.set(newChat.id, chatTopics);
              return newMap;
            });
            
            // Switch to chats view to show the newly created chat
            setSidebarMode('chats');
            
            // Update URL with new chat ID (this removes topicId from URL)
            router.replace(`/agents/${agentId}/chat?chatId=${newChat.id}`, { scroll: false });
          } catch (error) {
            console.error("Failed to auto-create chat:", error);
            isCreatingChatFromUrl.current = false;
            // Fallback to showing the dialog
            setSelectedTopicForNewChat(selectedTopic);
            setNewChatName(defaultName);
            setShowChatNameDialog(true);
            setSidebarMode('topics');
          }
        })();
      } else {
        // Fallback to creating a general chat if topic not found
        createNewChat();
      }
    } else if (chatIdFromUrl && initialMessages && initialLoadedTopics !== undefined) {
      // Load initial chat from URL
      const chat = initialChats.find(c => c.id === chatIdFromUrl);
      if (chat) {
        setCurrentChat(chat);
        setMessages(initialMessages);
        setLoadedTopics(initialLoadedTopics);
      } else if (initialChats.length > 0) {
        // Chat not found, load most recent
        loadChat(initialChats[0]);
      } else {
        // No chats, create new one
        createNewChat();
      }
    } else if (initialChats.length > 0 && !chatIdFromUrl) {
      // No specific chat requested, load most recent
      loadChat(initialChats[0]);
    }
    // If no chats and no topic, just show empty state
  }, []); // Only run once on mount

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  return (
    <div className="min-h-screen bg-background flex flex-col">
      <Header user={user} />

      <main className="flex-1 flex overflow-hidden">
        {/* Sidebar with Mode Toggle */}
        <div
          className={cn(
            "border-r bg-muted/10 transition-all duration-300",
            isSidebarOpen ? "w-80" : "w-0 md:w-80"
          )}
        >
          <div className="h-full flex flex-col">
            {/* Sidebar Header */}
            <div className="p-4 border-b space-y-3">
              <div className="flex items-center gap-2">
                <AgentAvatar
                  imageUrl={agent.profileImageUrl}
                  displayName={agent.displayName}
                  size="md"
                />
                <div className="flex-1 min-w-0">
                  <h2 className="font-semibold text-sm truncate">{agent.displayName}</h2>
                  <Link href="/agents">
                    <Button variant="link" size="sm" className="h-auto p-0 text-xs">
                      <ArrowLeft className="mr-1 h-3 w-3" />
                      Back to Agents
                    </Button>
                  </Link>
                </div>
              </div>

              {/* Mode Toggle */}
              <div className="flex gap-1 p-1 bg-muted rounded-lg">
                <Button
                  variant={sidebarMode === 'topics' ? 'default' : 'ghost'}
                  size="sm"
                  className="flex-1 h-8 text-xs"
                  onClick={() => setSidebarMode('topics')}
                >
                  <MessageSquarePlus className="mr-1.5 h-3.5 w-3.5 flex-shrink-0" />
                  <span className="truncate">Topics</span>
                </Button>
                <Button
                  variant={sidebarMode === 'chats' ? 'default' : 'ghost'}
                  size="sm"
                  className="flex-1 h-8 text-xs"
                  onClick={() => setSidebarMode('chats')}
                >
                  <MessageSquare className="mr-1.5 h-3.5 w-3.5 flex-shrink-0" />
                  <span className="truncate">Chats</span>
                </Button>
              </div>
            </div>

            {/* Sidebar Content - Changes based on mode */}
            {sidebarMode === 'chats' ? (
              /* Chats Mode */
              <ScrollArea className="flex-1">
                <div className="p-2 space-y-1">
                  <p className="text-xs font-medium text-muted-foreground px-2 py-1.5">
                    Conversation history
                  </p>
                  {pastChats.map((chat) => (
                  <div
                    key={chat.id}
                    className={cn(
                      "relative group rounded-lg transition-colors",
                      currentChat?.id === chat.id && "bg-muted"
                    )}
                  >
                    {editingChatId === chat.id ? (
                      <div className="p-2 flex items-center gap-1">
                        <Input
                          value={editedTitle}
                          onChange={(e) => setEditedTitle(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              handleSaveSidebarTitle(chat.id, e as any);
                            } else if (e.key === "Escape") {
                              handleCancelSidebarEdit(e as any);
                            }
                          }}
                          className="h-7 text-sm"
                          placeholder="Conversation title"
                          autoFocus
                          onClick={(e) => e.stopPropagation()}
                        />
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={(e) => handleSaveSidebarTitle(chat.id, e)}
                          className="h-7 w-7 p-0 flex-shrink-0"
                        >
                          <Check className="h-3 w-3 text-green-600" />
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={handleCancelSidebarEdit}
                          className="h-7 w-7 p-0 flex-shrink-0"
                        >
                          <X className="h-3 w-3 text-red-600" />
                        </Button>
                      </div>
                    ) : (
                      <div
                        onClick={() => loadChat(chat)}
                        className="w-full text-left p-3 hover:bg-muted/50 rounded-lg transition-colors cursor-pointer"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <p className="font-medium text-sm truncate flex-1">
                            {chat.title || "New Conversation"}
                          </p>
                          <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={(e) => handleStartEditSidebarTitle(chat.id, chat.title || "", e)}
                              className="h-6 w-6 p-0"
                            >
                              <Edit2 className="h-3 w-3" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={(e) => handleDeleteClick(chat, e)}
                              className="h-6 w-6 p-0 hover:text-destructive"
                            >
                              <Trash2 className="h-3 w-3" />
                            </Button>
                          </div>
                        </div>
                        <p className="text-xs text-muted-foreground mt-1">
                          {new Date(chat.lastMessageAt).toLocaleDateString()}
                        </p>
                        {/* Topic badges */}
                        {chatTopicsMap.has(chat.id) && chatTopicsMap.get(chat.id)!.length > 0 && (
                          <div className="flex flex-wrap gap-1 mt-2">
                            {chatTopicsMap.get(chat.id)!.slice(0, 2).map((topic) => (
                              <span
                                key={topic.id}
                                className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs border border-primary/20"
                              >
                                <BookMarked className="h-2.5 w-2.5" />
                                {topic.name}
                              </span>
                            ))}
                            {chatTopicsMap.get(chat.id)!.length > 2 && (
                              <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-muted text-muted-foreground text-xs">
                                +{chatTopicsMap.get(chat.id)!.length - 2} more
                              </span>
                            )}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                  ))}
                </div>
              </ScrollArea>
            ) : (
              /* Topics Mode - Topics List */
              <>
                {/* Search and Filter Controls */}
                <div className="p-3 border-b space-y-2">
                  {/* Search Input */}
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Search topics..."
                      value={topicSearchQuery}
                      onChange={(e) => setTopicSearchQuery(e.target.value)}
                      className="pl-9 h-9"
                    />
                    {topicSearchQuery && (
                      <button
                        onClick={() => setTopicSearchQuery("")}
                        className="absolute right-3 top-1/2 -translate-y-1/2"
                      >
                        <X className="h-4 w-4 text-muted-foreground hover:text-foreground" />
                      </button>
                    )}
                  </div>

                  {/* Category and Tag Filters */}
                  <div className="flex gap-2">
                    {/* Category Filter */}
                    {allCategories.length > 0 && (
                      <Select value={topicFilterCategory || "all"} onValueChange={(value) => setTopicFilterCategory(value === "all" ? null : value)}>
                        <SelectTrigger className="h-8 text-xs flex-1">
                          <SelectValue placeholder="All Categories" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="all">All Categories</SelectItem>
                          {allCategories.map((category) => (
                            <SelectItem key={category.id} value={category.id}>
                              {category.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}

                    {/* Tag Filter */}
                    <Select 
                      value={topicFilterTags.length > 0 ? topicFilterTags[0] : "all"} 
                      onValueChange={(value) => {
                        if (value === "all") {
                          setTopicFilterTags([]);
                        } else {
                          // For now, single select. Could expand to multi-select later
                          setTopicFilterTags([value]);
                        }
                      }}
                    >
                      <SelectTrigger className="h-8 text-xs flex-1">
                        <SelectValue placeholder="All Tags" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Tags</SelectItem>
                        {allTags.length === 0 ? (
                          <SelectItem value="none" disabled>
                            No tags found - add tags to topics first
                          </SelectItem>
                        ) : (
                          allTags
                            .sort((a, b) => a.name.localeCompare(b.name))
                            .map((tag) => (
                              <SelectItem key={tag.id} value={tag.id}>
                                {tag.name}
                              </SelectItem>
                            ))
                        )}
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Sort Links */}
                  <div className="flex items-center gap-1 text-xs">
                    <button
                      onClick={() => setTopicSortOrder('chronological')}
                      className={cn(
                        "px-2 py-1 rounded transition-colors",
                        topicSortOrder === 'chronological'
                          ? "font-semibold text-primary underline underline-offset-2"
                          : "text-muted-foreground hover:text-foreground"
                      )}
                    >
                      Chronological
                    </button>
                    <span className="text-muted-foreground">|</span>
                    <button
                      onClick={() => setTopicSortOrder('alphabetical')}
                      className={cn(
                        "px-2 py-1 rounded transition-colors",
                        topicSortOrder === 'alphabetical'
                          ? "font-semibold text-primary underline underline-offset-2"
                          : "text-muted-foreground hover:text-foreground"
                      )}
                    >
                      Alphabetical
                    </button>
                  </div>
                </div>

                {/* Topics List */}
                <ScrollArea className="flex-1">
                  <div className="p-2 space-y-1">
                    {/* General Discussion Card */}
                    <button
                      key="general-discussion"
                      onClick={() => handleSelectTopicFromSidebar(null)}
                      className="w-full text-left p-3 rounded-lg border-2 border-dashed border-primary/30 bg-primary/5 hover:bg-primary/10 transition-colors"
                    >
                      <div className="flex items-start gap-2">
                        <MessageSquare className="h-5 w-5 text-primary mt-0.5 flex-shrink-0" />
                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-sm text-primary">General Discussion</p>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            Start a conversation without loading specific topic knowledge
                          </p>
                        </div>
                      </div>
                    </button>

                    {/* Filtered Topics */}
                    {(() => {
                      const filteredTopics = filterAndSortTopics(
                        agentTopics,
                        topicSearchQuery,
                        topicFilterCategory,
                        topicFilterTags,
                        topicSortOrder
                      );

                      if (filteredTopics.length === 0) {
                        return (
                          <div key="empty" className="text-center py-8 px-4">
                            <p className="text-sm text-muted-foreground">
                              {agentTopics.length === 0 
                                ? "No topics available yet."
                                : "No topics match your search."}
                            </p>
                          </div>
                        );
                      }

                      return filteredTopics.map((topic) => {
                          const category = allCategories.find(c => c.id === topic.categoryId);
                          const hasNoContent = !topic.contentUrl || topic.contentUrl.trim() === '';
                          
                          return (
                            <button
                              key={topic.id}
                              onClick={() => handleSelectTopicFromSidebar(topic)}
                              className="w-full text-left p-3 rounded-lg border hover:bg-accent transition-colors"
                            >
                              <div className="space-y-2">
                                <div className="flex items-start gap-2">
                                  <BookMarked className="h-4 w-4 text-muted-foreground mt-0.5 flex-shrink-0" />
                                  <div className="flex-1 min-w-0">
                                    <p className="font-medium text-sm">{topic.name}</p>
                                    {topic.description && (
                                      <p className="text-xs text-muted-foreground line-clamp-2 mt-1">
                                        {topic.description}
                                      </p>
                                    )}
                                    {hasNoContent && (
                                      <p className="text-xs text-amber-600 dark:text-amber-500 mt-1 flex items-center gap-1">
                                        <span className="inline-block w-1 h-1 rounded-full bg-amber-600 dark:bg-amber-500"></span>
                                        No training content yet
                                      </p>
                                    )}
                                  </div>
                                </div>
                                {category && (
                                  <div className="flex items-center gap-1">
                                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-muted text-muted-foreground text-xs">
                                      <FolderOpen className="h-2.5 w-2.5" />
                                      {category.name}
                                    </span>
                                  </div>
                                )}
                              </div>
                            </button>
                          );
                        });
                    })()}
                  </div>
                </ScrollArea>
              </>
            )}
          </div>
        </div>

        {/* Main Chat Area */}
        <div className="flex-1 flex flex-col">
          {/* Mobile Toggle */}
          <div className="md:hidden p-2 border-b">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            >
              <Menu className="h-4 w-4 mr-2" />
              {isSidebarOpen ? "Hide" : "Show"} Conversations
            </Button>
          </div>

          {/* Chat Header */}
          <div className="border-b p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <AgentAvatar
                  imageUrl={agent.profileImageUrl}
                  displayName={agent.displayName}
                  size="lg"
                />
                <div>
                  <h1 className="text-xl font-bold">{agent.displayName}</h1>
                </div>
              </div>
              {currentChat && (
                <div className="flex items-center gap-2">
                  {isEditingTitle ? (
                    <>
                      <Input
                        value={editedTitle}
                        onChange={(e) => setEditedTitle(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            handleSaveTitle();
                          } else if (e.key === "Escape") {
                            handleCancelEditTitle();
                          }
                        }}
                        className="h-8 max-w-xs"
                        placeholder="Conversation title"
                        autoFocus
                      />
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={handleSaveTitle}
                        className="h-8 w-8 p-0"
                      >
                        <Check className="h-4 w-4 text-green-600" />
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={handleCancelEditTitle}
                        className="h-8 w-8 p-0"
                      >
                        <X className="h-4 w-4 text-red-600" />
                      </Button>
                    </>
                  ) : (
                    <>
                      <div className="text-sm text-muted-foreground">
                        {currentChat.title || "New Conversation"}
                      </div>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={handleStartEditTitle}
                        className="h-8 w-8 p-0"
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setIsVoiceModeOpen(true)}
                        className="gap-1 ml-2"
                        title="Voice Conversation Mode"
                      >
                        <Mic className="h-4 w-4" />
                        Voice
                      </Button>
                    </>
                  )}
                </div>
              )}
            </div>
            
            {/* Active Topics Display */}
            {currentChat && loadedTopics.length > 0 && (
              <div className="px-4 py-2 bg-muted/50 border-t">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-xs font-medium text-muted-foreground">Active Topics:</span>
                  {loadedTopics.map((topic) => (
                    <span
                      key={topic.id}
                      className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-primary/10 text-primary text-xs border border-primary/20"
                    >
                      <BookMarked className="h-3 w-3" />
                      {topic.name}
                    </span>
                  ))}
                </div>
              </div>
            )}
            
            {/* General Discussion Indicator */}
            {currentChat && loadedTopics.length === 0 && (
              <div className="px-4 py-2 bg-muted/30 border-t">
                <div className="flex items-center gap-2">
                  <MessageSquare className="h-3 w-3 text-muted-foreground" />
                  <span className="text-xs text-muted-foreground">General Discussion (no topics loaded)</span>
                </div>
              </div>
            )}
          </div>

          {/* Messages Area */}
          <ScrollArea className="flex-1 p-4">
            {isLoadingMessages ? (
              <div className="flex items-center justify-center h-full">
                <Loader2 className="h-8 w-8 animate-spin" />
              </div>
            ) : messages.length === 0 ? (
              <div className="flex items-center justify-center h-full">
                <div className="text-center space-y-2 px-4">
                  <h3 className="text-lg font-semibold">
                    Start chatting with {agent.displayName}
                  </h3>
                  <p className="text-sm text-muted-foreground">
                    Select a topic from the sidebar or type a message below
                  </p>
                </div>
              </div>
            ) : (
              <div className="space-y-4 max-w-3xl mx-auto">
                {messages.map((message) => (
                  <div
                    key={message.id}
                    className={`flex gap-3 ${
                      message.role === "user" ? "flex-row-reverse" : "flex-row"
                    }`}
                  >
                    {message.role === "assistant" ? (
                      <AgentAvatar
                        imageUrl={agent.profileImageUrl}
                        displayName={agent.displayName}
                        size="sm"
                        className="flex-shrink-0"
                      />
                    ) : (
                      <Avatar className="h-8 w-8 flex-shrink-0">
                        <AvatarImage
                          src={user.picture || undefined}
                          alt={user.name || "User"}
                        />
                        <AvatarFallback>
                          {user.name?.charAt(0).toUpperCase() || "U"}
                        </AvatarFallback>
                      </Avatar>
                    )}
                    <div
                      className={`flex flex-col max-w-[70%] ${
                        message.role === "user" ? "items-end" : "items-start"
                      }`}
                    >
                      <div
                        className={`rounded-lg px-4 py-2 ${
                          message.role === "user"
                            ? "bg-primary text-primary-foreground"
                            : "bg-muted"
                        }`}
                      >
                        <p className="text-sm whitespace-pre-wrap">{message.content}</p>
                      </div>
                      <div className="flex items-center gap-2 mt-1">
                        <span className="text-xs text-muted-foreground">
                          {new Date(message.createdAt).toLocaleTimeString([], {
                            hour: "2-digit",
                            minute: "2-digit",
                          })}
                        </span>
                        {message.role === "assistant" && (
                          <MessagePlayButton messageId={message.id} />
                        )}
                      </div>
                    </div>
                  </div>
                ))}
                {isSending && (
                  <div className="flex gap-3">
                    <AgentAvatar
                      imageUrl={agent.profileImageUrl}
                      displayName={agent.displayName}
                      size="sm"
                      className="flex-shrink-0"
                    />
                    <div className="bg-muted rounded-lg px-4 py-2">
                      <Loader2 className="h-4 w-4 animate-spin" />
                    </div>
                  </div>
                )}
                <div ref={messagesEndRef} />
              </div>
            )}
          </ScrollArea>

          {/* Message Input */}
          <div className="border-t p-4">
            <form onSubmit={handleSendMessage} className="flex gap-2 max-w-3xl mx-auto">
              <Input
                placeholder="Type your message..."
                value={inputMessage}
                onChange={(e) => setInputMessage(e.target.value)}
                disabled={isSending || !currentChat}
                className="flex-1"
              />
              <Button
                type="submit"
                disabled={isSending || !inputMessage.trim() || !currentChat}
              >
                <Send className="h-4 w-4" />
              </Button>
            </form>
          </div>
        </div>
      </main>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this conversation?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete "{chatToDelete?.title || "New Conversation"}" and all its messages. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                "Delete"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Simplified Chat Name Dialog */}
      <Dialog open={showChatNameDialog} onOpenChange={setShowChatNameDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Name Your Conversation</DialogTitle>
            <DialogDescription>
              {selectedTopicForNewChat 
                ? `Give your conversation about "${selectedTopicForNewChat.name}" a name.`
                : "Give your conversation a name."}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 mt-4">
            <div className="space-y-2">
              <label htmlFor="chat-name" className="text-sm font-medium">
                Conversation Name
              </label>
              <Input
                id="chat-name"
                value={newChatName}
                onChange={(e) => setNewChatName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && newChatName.trim()) {
                    handleCreateChatWithName();
                  }
                }}
                placeholder="Enter a name for this conversation"
                autoFocus
                disabled={isCreatingChat}
              />
            </div>

            {selectedTopicForNewChat && (
              <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-primary/10 text-primary text-sm border border-primary/20">
                <BookMarked className="h-4 w-4" />
                <span>Topic: {selectedTopicForNewChat.name}</span>
              </div>
            )}
          </div>

          <DialogFooter className="mt-4">
            <Button
              variant="outline"
              onClick={() => setShowChatNameDialog(false)}
              disabled={isCreatingChat}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateChatWithName}
              disabled={!newChatName.trim() || isCreatingChat}
            >
              {isCreatingChat ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                "Create Chat"
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Voice Conversation Mode */}
      {currentChat && (
        <VoiceConversationMode
          isOpen={isVoiceModeOpen}
          onClose={() => {
            setIsVoiceModeOpen(false);
            // Refresh messages after voice conversation
            fetchChatMessages(currentChat.id).then((response) => {
              setMessages(response.items);
            });
          }}
          chatId={currentChat.id}
          agentId={agentId}
          agentName={agent.displayName}
          agentImageUrl={agent.profileImageUrl}
        />
      )}
    </div>
  );
}

