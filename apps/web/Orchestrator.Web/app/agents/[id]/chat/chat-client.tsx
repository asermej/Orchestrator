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
  createChat,
  Chat,
  Message,
  Category,
  AgentCategory,
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
  initialCategories: Category[];
  initialAgentCategories: AgentCategory[];
  chatIdFromUrl: string | null;
  initialMessages?: Message[];
}

export function ChatClient({
  user,
  agentId,
  initialAgent,
  initialChats,
  initialCategories,
  initialAgentCategories,
  chatIdFromUrl,
  initialMessages,
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

  const { execute: executeCreateChat, isLoading: isCreatingChat } = useServerAction(
    async (chatName: string) => {
      if (!userId || !agentId || !agent) throw new Error("Session not ready");

      let newChat: Chat | null = null;
      
      try {
        // Create the chat
        newChat = await createChat(agentId, userId, chatName);
        setCurrentChat(newChat);
        setPastChats((prev) => [newChat, ...prev]);
        setMessages([]);
        
        
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

  const [allCategories] = useState<Category[]>(initialCategories);
  const [agentCategories] = useState<AgentCategory[]>(initialAgentCategories);

  // Sidebar mode state
  const [sidebarMode, setSidebarMode] = useState<'chats'>('chats');
  
  // Track if we're creating a chat from URL to prevent duplicates
  const isCreatingChatFromUrl = useRef(false);

  // Simplified chat creation state
  const [showChatNameDialog, setShowChatNameDialog] = useState(false);
  const [newChatName, setNewChatName] = useState("");

  // Get user ID from Auth0 sub claim
  const userId = user?.sub;

  const createNewChat = async () => {
    await executeCreateChat("New Conversation");
  };

  // Handler: Create chat with selected name
  const handleCreateChatWithName = async () => {
    if (!newChatName.trim()) return;
    
    setShowChatNameDialog(false);
    await executeCreateChat(newChatName.trim());
    
    // Reset state
    setNewChatName("");
  };

  const loadChat = async (chat: Chat) => {
    try {
      setIsLoadingMessages(true);
      setCurrentChat(chat);
      
      // Load messages for this chat
      const messagesResponse = await fetchChatMessages(chat.id);
      
      setMessages(messagesResponse.items);
      
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
    if (false && !isCreatingChatFromUrl.current) {
      // Handle topic selection from URL (from "Chat Now" button)
      // Automatically create chat and have agent kick off the conversation
      const selectedTopic = null;
      if (selectedTopic) {
        const defaultName = "New Conversation";
        
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
            
            // Switch to chats view to show the newly created chat
            setSidebarMode('chats');
            
            // Update URL with new chat ID
            router.replace(`/agents/${agentId}/chat?chatId=${newChat.id}`, { scroll: false });
          } catch (error) {
            console.error("Failed to auto-create chat:", error);
            isCreatingChatFromUrl.current = false;
          }
        })();
      }
    } else if (chatIdFromUrl && initialMessages) {
      // Load initial chat from URL
      const chat = initialChats.find(c => c.id === chatIdFromUrl);
      if (chat) {
        setCurrentChat(chat);
        setMessages(initialMessages);
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

            </div>

            {/* Sidebar Content */}
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
                      </div>
                    )}
                  </div>
                  ))}
                </div>
              </ScrollArea>
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
              Give your conversation a name.
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

