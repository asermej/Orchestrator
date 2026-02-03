"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { TrendingUp, Search } from "lucide-react";
import { Input } from "@/components/ui/input";

interface TrendingTopic {
  id: string;
  name: string;
  postCount: number;
  category: string;
}

export function TrendingSidebar() {
  // Placeholder data - will be replaced with real data later
  const trendingTopics: TrendingTopic[] = [
    { id: "1", name: "AI & Machine Learning", postCount: 1243, category: "Technology" },
    { id: "2", name: "Personal Development", postCount: 892, category: "Education" },
    { id: "3", name: "Creative Writing", postCount: 756, category: "Arts" },
    { id: "4", name: "Business Strategy", postCount: 634, category: "Business" },
    { id: "5", name: "Health & Wellness", postCount: 521, category: "Lifestyle" },
  ];

  return (
    <div className="space-y-4">
      {/* Search Box */}
      <div className="bg-muted rounded-full px-4 py-2 flex items-center gap-2">
        <Search className="h-4 w-4 text-muted-foreground" />
        <Input
          type="text"
          placeholder="Search"
          className="border-0 bg-transparent focus-visible:ring-0 focus-visible:ring-offset-0 p-0 h-auto"
        />
      </div>

      {/* Subscribe to Premium */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Subscribe to Premium</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground mb-4">
            Subscribe to unlock new features and if eligible, receive a share of revenue.
          </p>
          <button className="bg-primary text-primary-foreground hover:bg-primary/90 px-4 py-2 rounded-full font-semibold text-sm">
            Subscribe
          </button>
        </CardContent>
      </Card>

      {/* Trending Topics */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <TrendingUp className="h-5 w-5" />
            What's happening
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {trendingTopics.map((topic) => (
            <div
              key={topic.id}
              className="px-4 py-3 hover:bg-muted/50 cursor-pointer transition-colors border-b last:border-0"
            >
              <p className="text-xs text-muted-foreground mb-1">
                {topic.category} · Trending
              </p>
              <p className="font-semibold text-sm mb-1">{topic.name}</p>
              <p className="text-xs text-muted-foreground">
                {topic.postCount.toLocaleString()} posts
              </p>
            </div>
          ))}
          <div className="px-4 py-3 text-sm text-primary hover:bg-muted/50 cursor-pointer">
            Show more
          </div>
        </CardContent>
      </Card>

      {/* Footer Links */}
      <div className="px-4 py-2">
        <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
          <a href="#" className="hover:underline">Terms of Service</a>
          <span>·</span>
          <a href="#" className="hover:underline">Privacy Policy</a>
          <span>·</span>
          <a href="#" className="hover:underline">Cookie Policy</a>
          <span>·</span>
          <a href="#" className="hover:underline">Accessibility</a>
          <span>·</span>
          <a href="#" className="hover:underline">Ads info</a>
        </div>
        <p className="text-xs text-muted-foreground mt-2">
          © 2026 Hireology
        </p>
      </div>
    </div>
  );
}

