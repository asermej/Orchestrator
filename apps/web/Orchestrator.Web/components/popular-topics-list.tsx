"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { MessageCircle, TrendingUp } from "lucide-react";
import { PersonaAvatar } from "@/components/persona-avatar";

interface TopicTag {
  id: string;
  name: string;
}

interface TopicAuthor {
  id: string;
  firstName: string;
  lastName: string;
  profileImageUrl?: string;
}

interface TopicCategory {
  id: string;
  name: string;
}

interface PopularTopicItem {
  id: string;
  name: string;
  description?: string;
  category: TopicCategory;
  tags: TopicTag[];
  author: TopicAuthor;
  chatCount: number;
  personaId: string;
  createdAt: string;
}

interface PopularTopicsListProps {
  topics: PopularTopicItem[];
  onTopicClick: (topicId: string, personaId: string) => void;
}

export function PopularTopicsList({
  topics,
  onTopicClick,
}: PopularTopicsListProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {topics.map((topic) => (
        <Card
          key={topic.id}
          className="hover:shadow-md transition-shadow cursor-pointer"
          onClick={() => onTopicClick(topic.id, topic.personaId)}
        >
          <CardContent className="p-4">
            <div className="space-y-3">
              {/* Header with category and popularity */}
              <div className="flex items-center justify-between">
                <Badge variant="secondary" className="text-xs">
                  {topic.category.name}
                </Badge>
                <div className="flex items-center gap-1 text-sm text-muted-foreground">
                  <TrendingUp className="h-4 w-4 text-orange-500" />
                  <span className="font-medium">{topic.chatCount}</span>
                  <MessageCircle className="h-4 w-4 ml-1" />
                </div>
              </div>

              {/* Topic name */}
              <div>
                <h3 className="font-semibold text-base line-clamp-2">
                  {topic.name}
                </h3>
                {topic.description && (
                  <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                    {topic.description}
                  </p>
                )}
              </div>

              {/* Tags */}
              {topic.tags && topic.tags.length > 0 && (
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
              )}

              {/* Author */}
              <div className="flex items-center gap-2 pt-2 border-t">
                <PersonaAvatar
                  imageUrl={topic.author.profileImageUrl}
                  displayName={`${topic.author.firstName} ${topic.author.lastName}`}
                  size="sm"
                  shape="circle"
                />
                <span className="text-sm text-muted-foreground">
                  {topic.author.firstName} {topic.author.lastName}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

