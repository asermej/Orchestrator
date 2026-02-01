"use client";

import { useState } from "react";
import { Check, X, Search } from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Button } from "@/components/ui/button";

interface Option {
  id: string;
  name: string;
}

interface TagCategorySelectorProps {
  categories?: Option[];
  tags?: Option[];
  selectedCategoryIds: string[];
  selectedTagIds: string[];
  onCategoryChange: (ids: string[]) => void;
  onTagChange: (ids: string[]) => void;
  placeholder?: string;
  className?: string;
}

export function TagCategorySelector({
  categories = [],
  tags = [],
  selectedCategoryIds,
  selectedTagIds,
  onCategoryChange,
  onTagChange,
  placeholder = "Filter by tags and categories...",
  className,
}: TagCategorySelectorProps) {
  const [open, setOpen] = useState(false);
  
  // Get selected items for display
  const selectedCategories = categories.filter((c) =>
    selectedCategoryIds.includes(c.id)
  );
  const selectedTags = tags.filter((t) => selectedTagIds.includes(t.id));
  const selectedCount = selectedCategories.length + selectedTags.length;

  const handleCategoryToggle = (categoryId: string) => {
    const newSelected = selectedCategoryIds.includes(categoryId)
      ? selectedCategoryIds.filter((id) => id !== categoryId)
      : [...selectedCategoryIds, categoryId];
    onCategoryChange(newSelected);
  };

  const handleTagToggle = (tagId: string) => {
    const newSelected = selectedTagIds.includes(tagId)
      ? selectedTagIds.filter((id) => id !== tagId)
      : [...selectedTagIds, tagId];
    onTagChange(newSelected);
  };

  const handleRemoveCategory = (categoryId: string) => {
    onCategoryChange(selectedCategoryIds.filter((id) => id !== categoryId));
  };

  const handleRemoveTag = (tagId: string) => {
    onTagChange(selectedTagIds.filter((id) => id !== tagId));
  };

  const handleClearAll = () => {
    onCategoryChange([]);
    onTagChange([]);
  };

  return (
    <div className={cn("space-y-2", className)}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className="w-full justify-between"
          >
            <span className="flex items-center gap-2">
              <Search className="h-4 w-4 text-muted-foreground" />
              {selectedCount > 0 ? (
                <span>{selectedCount} selected</span>
              ) : (
                <span className="text-muted-foreground">{placeholder}</span>
              )}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[400px] p-0" align="start">
          <Command>
            <CommandInput placeholder="Search categories and tags..." />
            <CommandList>
              <CommandEmpty>No results found.</CommandEmpty>
              
              {categories.length > 0 && (
                <CommandGroup heading="Categories">
                  {categories.map((category) => (
                    <CommandItem
                      key={category.id}
                      value={category.name}
                      onSelect={() => handleCategoryToggle(category.id)}
                    >
                      <div
                        className={cn(
                          "mr-2 flex h-4 w-4 items-center justify-center rounded-sm border border-primary",
                          selectedCategoryIds.includes(category.id)
                            ? "bg-primary text-primary-foreground"
                            : "opacity-50 [&_svg]:invisible"
                        )}
                      >
                        <Check className={cn("h-4 w-4")} />
                      </div>
                      <span>{category.name}</span>
                    </CommandItem>
                  ))}
                </CommandGroup>
              )}

              {tags.length > 0 && (
                <CommandGroup heading="Tags">
                  {tags.map((tag) => (
                    <CommandItem
                      key={tag.id}
                      value={tag.name}
                      onSelect={() => handleTagToggle(tag.id)}
                    >
                      <div
                        className={cn(
                          "mr-2 flex h-4 w-4 items-center justify-center rounded-sm border border-primary",
                          selectedTagIds.includes(tag.id)
                            ? "bg-primary text-primary-foreground"
                            : "opacity-50 [&_svg]:invisible"
                        )}
                      >
                        <Check className={cn("h-4 w-4")} />
                      </div>
                      <span>#{tag.name}</span>
                    </CommandItem>
                  ))}
                </CommandGroup>
              )}
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      {/* Selected items display */}
      {selectedCount > 0 && (
        <div className="flex flex-wrap gap-2 items-center">
          {selectedCategories.map((category) => (
            <Badge
              key={category.id}
              variant="secondary"
              className="pl-2 pr-1 py-1"
            >
              {category.name}
              <button
                onClick={() => handleRemoveCategory(category.id)}
                className="ml-1 rounded-full hover:bg-muted p-0.5"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
          {selectedTags.map((tag) => (
            <Badge key={tag.id} variant="outline" className="pl-2 pr-1 py-1">
              #{tag.name}
              <button
                onClick={() => handleRemoveTag(tag.id)}
                className="ml-1 rounded-full hover:bg-muted p-0.5"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
          {selectedCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClearAll}
              className="h-7 text-xs"
            >
              Clear all
            </Button>
          )}
        </div>
      )}
    </div>
  );
}

