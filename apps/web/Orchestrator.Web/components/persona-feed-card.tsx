import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PersonaAvatar } from "@/components/persona-avatar";
import { BookOpen, MessageSquare, Mail, ArrowRight } from "lucide-react";

interface PersonaFeedCardProps {
  persona: {
    id: string;
    displayName: string;
    firstName?: string;
    lastName?: string;
    profileImageUrl?: string;
    bio?: string;
    categories: Array<{
      id: string;
      name: string;
    }>;
    topicCount: number;
    chatCount: number;
    messageCount: number;
    createdAt: string;
  };
}

export function PersonaFeedCard({ persona }: PersonaFeedCardProps) {
  const fullName = [persona.firstName, persona.lastName].filter(Boolean).join(" ");

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
        <Link href={`/personas/${persona.id}/chat`}>
          <PersonaAvatar
            imageUrl={persona.profileImageUrl}
            displayName={persona.displayName}
            size="xl"
            shape="circle"
          />
        </Link>

        {/* Content */}
        <div className="flex-1 min-w-0">
          {/* Display name and real name */}
          <div className="mb-2">
            <Link href={`/personas/${persona.id}/chat`} className="hover:underline">
              <h3 className="text-lg font-bold">{persona.displayName}</h3>
            </Link>
            {fullName && (
              <p className="text-sm text-muted-foreground">{fullName}</p>
            )}
          </div>

          {/* Bio */}
          {persona.bio && (
            <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{persona.bio}</p>
          )}

          {/* Categories */}
          {persona.categories && persona.categories.length > 0 && (
            <div className="flex flex-wrap gap-2 mb-3">
              {persona.categories.slice(0, 3).map((category) => (
                <Badge key={category.id} variant="secondary" className="text-xs">
                  {category.name}
                </Badge>
              ))}
              {persona.categories.length > 3 && (
                <span className="text-xs text-muted-foreground">
                  +{persona.categories.length - 3} more
                </span>
              )}
            </div>
          )}

          {/* Engagement metrics */}
          <div className="flex items-center gap-4 text-sm text-muted-foreground mb-3">
            <div className="flex items-center gap-1.5">
              <BookOpen className="h-3.5 w-3.5" />
              <span>{persona.topicCount} {persona.topicCount === 1 ? 'topic' : 'topics'}</span>
            </div>
            <div className="flex items-center gap-1.5">
              <MessageSquare className="h-3.5 w-3.5" />
              <span>{persona.chatCount} {persona.chatCount === 1 ? 'chat' : 'chats'}</span>
            </div>
            <div className="flex items-center gap-1.5">
              <Mail className="h-3.5 w-3.5" />
              <span>{persona.messageCount.toLocaleString()} {persona.messageCount === 1 ? 'message' : 'messages'}</span>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between">
            <Link 
              href={`/personas/${persona.id}/chat`} 
              className="text-sm text-primary hover:underline"
            >
              View Profile
            </Link>
            <Link href={`/personas/${persona.id}/chat`}>
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

