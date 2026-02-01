"use client";

import * as React from "react";
import { X, Tag, ChevronDown, ChevronUp } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface TagItem {
  id: string;
  name: string;
}

interface TagSelectorProps {
  availableTags: TagItem[];
  selectedTagIds: string[];
  onTagChange: (ids: string[]) => void;
  placeholder?: string;
}

export function TagSelector({
  availableTags,
  selectedTagIds,
  onTagChange,
  placeholder = "Filter by tags...",
}: TagSelectorProps) {
  const [open, setOpen] = React.useState(false);
  const [search, setSearch] = React.useState("");

  const handleToggleTag = (tagId: string) => {
    if (selectedTagIds.includes(tagId)) {
      onTagChange(selectedTagIds.filter((id) => id !== tagId));
    } else {
      onTagChange([...selectedTagIds, tagId]);
    }
  };

  const filteredTags = availableTags.filter((tag) =>
    tag.name.toLowerCase().includes(search.toLowerCase())
  );

  const selectedTags = availableTags.filter((tag) =>
    selectedTagIds.includes(tag.id)
  );

  return (
    <div className="space-y-2">
      {/* Trigger button */}
      <Button
        type="button"
        variant="outline"
        onClick={() => setOpen(!open)}
        className="w-full justify-between"
      >
        <div className="flex items-center">
          <Tag className="mr-2 h-4 w-4" />
          {selectedTagIds.length === 0 ? (
            <span className="text-muted-foreground">{placeholder}</span>
          ) : (
            <span>{selectedTagIds.length} tag(s) selected</span>
          )}
        </div>
        {open ? (
          <ChevronUp className="h-4 w-4 opacity-50" />
        ) : (
          <ChevronDown className="h-4 w-4 opacity-50" />
        )}
      </Button>

      {/* Dropdown */}
      {open && (
        <div className="rounded-md border bg-background p-2 space-y-2">
          {/* Search input */}
          <Input
            placeholder="Search tags..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-8"
          />

          {/* Tag list */}
          <div className="max-h-48 overflow-y-auto space-y-1">
            {filteredTags.length === 0 ? (
              <div className="py-6 text-center text-sm text-muted-foreground">
                No tags found.
              </div>
            ) : (
              filteredTags.map((tag) => {
                const isSelected = selectedTagIds.includes(tag.id);
                return (
                  <button
                    key={tag.id}
                    type="button"
                    onClick={() => handleToggleTag(tag.id)}
                    className={cn(
                      "w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded-sm transition-colors",
                      "hover:bg-accent hover:text-accent-foreground",
                      isSelected && "bg-accent"
                    )}
                  >
                    <div
                      className={cn(
                        "h-4 w-4 rounded border flex items-center justify-center",
                        isSelected
                          ? "bg-primary border-primary"
                          : "border-input"
                      )}
                    >
                      {isSelected && (
                        <svg
                          className="h-3 w-3 text-primary-foreground"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                          strokeWidth={3}
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            d="M5 13l4 4L19 7"
                          />
                        </svg>
                      )}
                    </div>
                    {tag.name}
                  </button>
                );
              })
            )}
          </div>
        </div>
      )}

      {/* Selected tags as badges */}
      {selectedTags.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {selectedTags.map((tag) => (
            <Badge key={tag.id} variant="secondary" className="pr-1">
              <Tag className="mr-1 h-3 w-3" />
              {tag.name}
              <button
                type="button"
                className="ml-1 rounded-full outline-none ring-offset-background focus:ring-2 focus:ring-ring focus:ring-offset-2"
                onClick={() => handleToggleTag(tag.id)}
              >
                <X className="h-3 w-3 text-muted-foreground hover:text-foreground" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}
