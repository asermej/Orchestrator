import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { AgentAvatar } from "@/components/agent-avatar";
import { MessageSquare, Mail, ArrowRight } from "lucide-react";

interface AgentFeedCardProps {
  agent: {
    id: string;
    displayName: string;
    profileImageUrl?: string;
    bio?: string;
    categories: Array<{
      id: string;
      name: string;
    }>;
    chatCount: number;
    messageCount: number;
    createdAt: string;
  };
}

export function AgentFeedCard({ agent }: AgentFeedCardProps) {
  const getTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return "just now";
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 2592000) return `${Math.floor(seconds / 86400)}d ago`;
    if (seconds < 31536000) return `${Math.floor(seconds / 2592000)}mo ago`;
    return `${Math.floor(seconds / 31536000)}y ago`;
  };

  return (
    <div className="bg-card border rounded-lg p-4 hover:shadow-md transition-all cursor-pointer">
      <div className="flex gap-4">
        {/* Avatar - larger size */}
        <Link href={`/agents/${agent.id}/chat`}>
          <AgentAvatar
            imageUrl={agent.profileImageUrl}
            displayName={agent.displayName}
            size="xl"
            shape="circle"
          />
        </Link>

        {/* Content */}
        <div className="flex-1 min-w-0">
          {/* Display name */}
          <div className="mb-2">
            <Link href={`/agents/${agent.id}/chat`} className="hover:underline">
              <h3 className="text-lg font-bold">{agent.displayName}</h3>
            </Link>
          </div>

          {/* Bio */}
          {agent.bio && (
            <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{agent.bio}</p>
          )}

          {/* Categories */}
          {agent.categories && agent.categories.length > 0 && (
            <div className="flex flex-wrap gap-2 mb-3">
              {agent.categories.slice(0, 3).map((category) => (
                <Badge key={category.id} variant="secondary" className="text-xs">
                  {category.name}
                </Badge>
              ))}
              {agent.categories.length > 3 && (
                <span className="text-xs text-muted-foreground">
                  +{agent.categories.length - 3} more
                </span>
              )}
            </div>
          )}

          {/* Engagement metrics */}
          <div className="flex items-center gap-4 text-sm text-muted-foreground mb-3">
            <div className="flex items-center gap-1.5">
              <MessageSquare className="h-3.5 w-3.5" />
              <span>{agent.chatCount} {agent.chatCount === 1 ? 'chat' : 'chats'}</span>
            </div>
            <div className="flex items-center gap-1.5">
              <Mail className="h-3.5 w-3.5" />
              <span>{agent.messageCount.toLocaleString()} {agent.messageCount === 1 ? 'message' : 'messages'}</span>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between">
            <Link 
              href={`/agents/${agent.id}/chat`} 
              className="text-sm text-primary hover:underline"
            >
              View Profile
            </Link>
            <Link href={`/agents/${agent.id}/chat`}>
              <Button variant="outline" size="sm" className="h-8 px-4 gap-1.5">
                Chat Now
                <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

