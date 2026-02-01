"use client";

import { useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { PersonaAvatar } from "@/components/persona-avatar";
import { ChevronDown, ChevronUp, LucideIcon } from "lucide-react";
import Link from "next/link";

interface PersonaPreview {
  id: string;
  displayName: string;
  profileImageUrl?: string;
}

interface PersonaCategoryCardProps {
  title: string;
  description: string;
  icon: LucideIcon;
  personaCount: number;
  samplePersonas: PersonaPreview[];
  gradient: string;
}

export function PersonaCategoryCard({
  title,
  description,
  icon: Icon,
  personaCount,
  samplePersonas,
  gradient,
}: PersonaCategoryCardProps) {
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
                {personaCount} {personaCount === 1 ? 'persona' : 'personas'} available
              </CardDescription>
            </div>
          </div>
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">{description}</p>

        {/* Sample Personas */}
        <div className="flex items-center gap-2">
          <div className="flex -space-x-2">
            {samplePersonas.slice(0, 3).map((persona, index) => (
              <div 
                key={persona.id} 
                className="ring-2 ring-background rounded-full"
                style={{ zIndex: 10 - index }}
              >
                <PersonaAvatar
                  imageUrl={persona.profileImageUrl}
                  displayName={persona.displayName}
                  size="md"
                  shape="circle"
                />
              </div>
            ))}
          </div>
          {samplePersonas.length > 3 && (
            <span className="text-sm text-muted-foreground">
              +{samplePersonas.length - 3} more
            </span>
          )}
        </div>

        {/* Expanded Persona List */}
        {isExpanded && samplePersonas.length > 0 && (
          <div className="space-y-2 pt-2 border-t">
            {samplePersonas.map((persona) => (
              <Link 
                key={persona.id} 
                href={`/personas/${persona.id}/chat`}
                className="flex items-center gap-3 p-2 rounded-lg hover:bg-background/60 transition-colors"
              >
                <PersonaAvatar
                  imageUrl={persona.profileImageUrl}
                  displayName={persona.displayName}
                  size="sm"
                  shape="circle"
                />
                <span className="text-sm font-medium">{persona.displayName}</span>
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
              View All Personas <ChevronDown className="ml-2 h-4 w-4" />
            </>
          )}
        </Button>
      </CardContent>
    </Card>
  );
}

