"use client";

import { useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { AgentAvatar } from "@/components/agent-avatar";
import { ChevronDown, ChevronUp, LucideIcon } from "lucide-react";
import Link from "next/link";

interface AgentPreview {
  id: string;
  displayName: string;
  profileImageUrl?: string;
}

interface AgentCategoryCardProps {
  title: string;
  description: string;
  icon: LucideIcon;
  agentCount: number;
  sampleAgents: AgentPreview[];
  gradient: string;
}

export function AgentCategoryCard({
  title,
  description,
  icon: Icon,
  agentCount,
  sampleAgents,
  gradient,
}: AgentCategoryCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <Card className={`overflow-hidden transition-all hover:shadow-lg ${gradient}`}>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-lg bg-background/80 backdrop-blur">
              <Icon className="h-6 w-6" />
            </div>
            <div>
              <CardTitle className="text-xl">{title}</CardTitle>
              <CardDescription className="mt-1">
                {agentCount} {agentCount === 1 ? 'agent' : 'agents'} available
              </CardDescription>
            </div>
          </div>
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">{description}</p>

        {/* Sample Agents */}
        <div className="flex items-center gap-2">
          <div className="flex -space-x-2">
            {sampleAgents.slice(0, 3).map((agent, index) => (
              <div 
                key={agent.id} 
                className="ring-2 ring-background rounded-full"
                style={{ zIndex: 10 - index }}
              >
                <AgentAvatar
                  imageUrl={agent.profileImageUrl}
                  displayName={agent.displayName}
                  size="md"
                  shape="circle"
                />
              </div>
            ))}
          </div>
          {sampleAgents.length > 3 && (
            <span className="text-sm text-muted-foreground">
              +{sampleAgents.length - 3} more
            </span>
          )}
        </div>

        {/* Expanded Agent List */}
        {isExpanded && sampleAgents.length > 0 && (
          <div className="space-y-2 pt-2 border-t">
            {sampleAgents.map((agent) => (
              <Link 
                key={agent.id} 
                href={`/agents/${agent.id}/chat`}
                className="flex items-center gap-3 p-2 rounded-lg hover:bg-background/60 transition-colors"
              >
                <AgentAvatar
                  imageUrl={agent.profileImageUrl}
                  displayName={agent.displayName}
                  size="sm"
                  shape="circle"
                />
                <span className="text-sm font-medium">{agent.displayName}</span>
              </Link>
            ))}
          </div>
        )}

        {/* Expand/Collapse Button */}
        <Button
          variant="ghost"
          size="sm"
          className="w-full"
          onClick={() => setIsExpanded(!isExpanded)}
        >
          {isExpanded ? (
            <>
              Show Less <ChevronUp className="ml-2 h-4 w-4" />
            </>
          ) : (
            <>
              View All Agents <ChevronDown className="ml-2 h-4 w-4" />
            </>
          )}
        </Button>
      </CardContent>
    </Card>
  );
}

