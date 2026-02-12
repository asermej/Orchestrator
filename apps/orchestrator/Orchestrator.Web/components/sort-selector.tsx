"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ArrowDownUp } from "lucide-react";

interface SortOption {
  value: string;
  label: string;
}

interface SortSelectorProps {
  value: string;
  onChange: (value: string) => void;
  options: SortOption[];
  className?: string;
}

export function SortSelector({
  value,
  onChange,
  options,
  className,
}: SortSelectorProps) {
  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger className={className}>
        <div className="flex items-center gap-2">
          <ArrowDownUp className="h-4 w-4" />
          <SelectValue />
        </div>
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

// Common sort options for different views
export const topicSortOptions: SortOption[] = [
  { value: "recent", label: "Most Recent" },
  { value: "popular", label: "Most Popular" },
  { value: "chat_count", label: "Most Chats" },
];

export const agentSortOptions: SortOption[] = [
  { value: "recent", label: "Most Recent" },
  { value: "popularity", label: "Most Popular" },
  { value: "alphabetical", label: "Alphabetical" },
];

