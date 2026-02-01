"use client";

import { motion } from "framer-motion";
import { Rss, Users, TrendingUp } from "lucide-react";
import { cn } from "@/lib/utils";

type ViewMode = "feed" | "personas" | "popular";

interface ViewOption {
  id: ViewMode;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
}

const viewOptions: ViewOption[] = [
  { id: "feed", label: "Topics", icon: Rss },
  { id: "personas", label: "Personas", icon: Users },
  { id: "popular", label: "Popular", icon: TrendingUp },
];

interface AnimatedViewSwitcherProps {
  currentView: ViewMode;
  onViewChange: (view: ViewMode) => void;
}

export function AnimatedViewSwitcher({
  currentView,
  onViewChange,
}: AnimatedViewSwitcherProps) {
  const currentIndex = viewOptions.findIndex((opt) => opt.id === currentView);

  return (
    <div className="relative bg-muted rounded-lg p-1">
      <div className="relative flex gap-1">
        {/* Animated background indicator */}
        <motion.div
          className="absolute top-0 bottom-0 bg-background border border-border rounded-md shadow-sm"
          initial={false}
          animate={{
            left: `${currentIndex * (100 / viewOptions.length)}%`,
            width: `${100 / viewOptions.length}%`,
          }}
          transition={{
            type: "spring",
            stiffness: 300,
            damping: 30,
          }}
          style={{
            margin: "4px",
            width: `calc(${100 / viewOptions.length}% - 8px)`,
          }}
        />

        {/* View options */}
        {viewOptions.map((option) => {
          const Icon = option.icon;
          const isActive = option.id === currentView;

          return (
            <button
              key={option.id}
              onClick={() => onViewChange(option.id)}
              className={cn(
                "relative z-10 flex-1 flex items-center justify-center gap-2 px-3 py-2.5 rounded-md transition-colors",
                "font-medium text-sm whitespace-nowrap",
                isActive
                  ? "text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <Icon className={cn("h-4 w-4 shrink-0", isActive && "text-primary")} />
              <span className="truncate">{option.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

