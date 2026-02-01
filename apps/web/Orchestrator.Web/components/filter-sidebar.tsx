"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Search, X } from "lucide-react";

interface Category {
  id: string;
  name: string;
}

interface Tag {
  id: string;
  name: string;
}

interface FilterSidebarProps {
  mode: "feed" | "personas";
  categories: Category[];
  tags?: Tag[];
  selectedCategoryIds: string[];
  selectedTagIds: string[];
  searchTerm: string;
  onCategoryChange: (categoryIds: string[]) => void;
  onTagChange?: (tagIds: string[]) => void;
  onSearchChange: (search: string) => void;
  onClearFilters: () => void;
}

export function FilterSidebar({
  mode,
  categories,
  tags = [],
  selectedCategoryIds,
  selectedTagIds,
  searchTerm,
  onCategoryChange,
  onTagChange,
  onSearchChange,
  onClearFilters,
}: FilterSidebarProps) {
  const [localSearch, setLocalSearch] = useState(searchTerm);

  const handleCategoryToggle = (categoryId: string) => {
    const newSelected = selectedCategoryIds.includes(categoryId)
      ? selectedCategoryIds.filter((id) => id !== categoryId)
      : [...selectedCategoryIds, categoryId];
    onCategoryChange(newSelected);
  };

  const handleTagToggle = (tagId: string) => {
    if (!onTagChange) return;
    const newSelected = selectedTagIds.includes(tagId)
      ? selectedTagIds.filter((id) => id !== tagId)
      : [...selectedTagIds, tagId];
    onTagChange(newSelected);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSearchChange(localSearch);
  };

  const hasActiveFilters =
    selectedCategoryIds.length > 0 ||
    selectedTagIds.length > 0 ||
    searchTerm.length > 0;

  return (
    <div className="w-full h-full flex flex-col">
      {/* Header */}
      <div className="mb-6">
        <h2 className="text-lg font-semibold mb-2">Filters</h2>
        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearFilters}
            className="text-muted-foreground hover:text-foreground"
          >
            <X className="h-4 w-4 mr-1" />
            Clear all
          </Button>
        )}
      </div>

      <ScrollArea className="flex-1">
        <div className="space-y-6 pr-4">
          {/* Search (only in personas mode) */}
          {mode === "personas" && (
            <div>
              <h3 className="font-medium mb-3">Search Personas</h3>
              <form onSubmit={handleSearchSubmit} className="flex gap-2">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search by name..."
                    value={localSearch}
                    onChange={(e) => setLocalSearch(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Button type="submit" size="sm">
                  Go
                </Button>
              </form>
            </div>
          )}

          {/* Categories */}
          <div>
            <h3 className="font-medium mb-3">Categories</h3>
            <div className="space-y-2">
              {categories.map((category) => (
                <div key={category.id} className="flex items-center space-x-2">
                  <Checkbox
                    id={`category-${category.id}`}
                    checked={selectedCategoryIds.includes(category.id)}
                    onCheckedChange={() => handleCategoryToggle(category.id)}
                  />
                  <Label
                    htmlFor={`category-${category.id}`}
                    className="text-sm font-normal cursor-pointer"
                  >
                    {category.name}
                  </Label>
                </div>
              ))}
              {categories.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No categories available
                </p>
              )}
            </div>
          </div>

          {/* Tags (only in feed mode) */}
          {mode === "feed" && onTagChange && (
            <div>
              <h3 className="font-medium mb-3">Tags</h3>
              <div className="space-y-2">
                {tags.map((tag) => (
                  <div key={tag.id} className="flex items-center space-x-2">
                    <Checkbox
                      id={`tag-${tag.id}`}
                      checked={selectedTagIds.includes(tag.id)}
                      onCheckedChange={() => handleTagToggle(tag.id)}
                    />
                    <Label
                      htmlFor={`tag-${tag.id}`}
                      className="text-sm font-normal cursor-pointer"
                    >
                      #{tag.name}
                    </Label>
                  </div>
                ))}
                {tags.length === 0 && (
                  <p className="text-sm text-muted-foreground">No tags available</p>
                )}
              </div>
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

