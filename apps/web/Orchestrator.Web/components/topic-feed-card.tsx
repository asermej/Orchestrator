import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { AgentAvatar } from "@/components/agent-avatar";
import { MessageCircle, TrendingUp, ArrowRight } from "lucide-react";

interface TopicFeedCardProps {
  topic: {
    id: string;
    name: string;
    description?: string;
    agentId: string;
    author?: {
      id: string;
      firstName: string;
      lastName: string;
      profileImageUrl?: string;
      topicCount?: number;
      messageCount?: number;
    };
    chatCount: number;
    category: {
      id: string;
      name: string;
    };
    tags: Array<{
      id: string;
      name: string;
    }>;
    createdAt: string;
  };
}

export function TopicFeedCard({ topic }: TopicFeedCardProps) {
  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="p-4">
        <div className="space-y-3">
          {/* Header with topic name and stats */}
          <div className="flex items-start justify-between gap-3">
            <h3 className="font-semibold text-base line-clamp-2 flex-1">
              {topic.name}
            </h3>
            <div className="flex items-center gap-1 text-sm text-muted-foreground flex-shrink-0">
              <TrendingUp className="h-4 w-4 text-orange-500" />
              <span className="font-medium">{topic.chatCount}</span>
              <MessageCircle className="h-4 w-4 ml-1" />
            </div>
          </div>

          {/* Description */}
          {topic.description && (
            <p className="text-sm text-muted-foreground line-clamp-2">
              {topic.description}
            </p>
          )}

          {/* Category and Tags */}
          <div className="flex items-center gap-2">
            <Badge variant="secondary" className="text-xs">
              {topic.category.name}
            </Badge>
            {topic.tags && topic.tags.length > 0 && (
              <>
                <div className="h-4 w-px bg-border" />
                <div className="flex flex-wrap gap-1">
                  {topic.tags.slice(0, 3).map((tag) => (
                    <Badge
                      key={tag.id}
                      variant="outline"
                      className="text-xs px-2 py-0"
                    >
                      #{tag.name}
                    </Badge>
                  ))}
                  {topic.tags.length > 3 && (
                    <span className="text-xs text-muted-foreground self-center">
                      +{topic.tags.length - 3} more
                    </span>
                  )}
                </div>
              </>
            )}
          </div>

          {/* Author and Action */}
          <div className="flex items-center justify-between pt-2 border-t">
            {topic.author && (
              <div className="flex items-center gap-2">
                <AgentAvatar
                  imageUrl={topic.author.profileImageUrl}
                  displayName={`${topic.author.firstName} ${topic.author.lastName}`}
                  size="sm"
                  shape="circle"
                />
                <span className="text-sm text-muted-foreground">
                  {topic.author.firstName} {topic.author.lastName}
                </span>
              </div>
            )}
            <Link href={`/agents/${topic.agentId}/chat?topicId=${topic.id}`}>
              <Button variant="outline" size="sm" className="h-8 px-4 gap-1.5">
                Chat Now
                <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            </Link>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

