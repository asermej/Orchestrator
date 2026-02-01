"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Folder } from "lucide-react";

interface Category {
  id: string;
  name: string;
}

interface CategorySelectorProps {
  availableCategories: Category[];
  selectedCategoryId?: string;
  onCategoryChange: (categoryId: string | undefined) => void;
  placeholder?: string;
}

export function CategorySelector({
  availableCategories,
  selectedCategoryId,
  onCategoryChange,
  placeholder = "All Categories",
}: CategorySelectorProps) {
  return (
    <Select
      value={selectedCategoryId || "all"}
      onValueChange={(value) => {
        if (value === "all") {
          onCategoryChange(undefined);
        } else {
          onCategoryChange(value);
        }
      }}
    >
      <SelectTrigger className="w-full">
        <div className="flex items-center gap-2">
          <Folder className="h-4 w-4 text-muted-foreground" />
          <SelectValue placeholder={placeholder} />
        </div>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="all">All Categories</SelectItem>
        {availableCategories.map((category) => (
          <SelectItem key={category.id} value={category.id}>
            {category.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

