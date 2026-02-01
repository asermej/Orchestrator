"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { useRouter } from "next/navigation";
import { motion, AnimatePresence } from "framer-motion";
import { TopicFeedCard } from "@/components/topic-feed-card";
import { PersonaFeedCard } from "@/components/persona-feed-card";
import { PopularTopicsList } from "@/components/popular-topics-list";
import { AnimatedViewSwitcher } from "@/components/animated-view-switcher";
import { CategorySelector } from "@/components/category-selector";
import { TagSelector } from "@/components/tag-selector";
import { SearchBar } from "@/components/search-bar";
import { SortSelector, topicSortOptions, personaSortOptions } from "@/components/sort-selector";
import { Loader2 } from "lucide-react";
import { useInfiniteScroll } from "@/hooks/use-infinite-scroll";
import {
  fetchTopicFeed,
  fetchPersonaFeed,
} from "./actions";

type ViewMode = "feed" | "personas" | "popular";

interface TopicFeedItem {
  id: string;
  name: string;
  description?: string;
  isPublic: boolean;
  personaId: string;
  author?: {
    id: string;
    firstName: string;
    lastName: string;
    profileImageUrl?: string;
    topicCount?: number;
    messageCount?: number;
  };
  chatCount: number;
  category: {
    id: string;
    name: string;
  };
  tags: Array<{
    id: string;
    name: string;
  }>;
  createdAt: string;
}

interface PersonaFeedItem {
  id: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  profileImageUrl?: string;
  topicCount: number;
  chatCount: number;
  messageCount: number;
  categories: Array<{
    id: string;
    name: string;
  }>;
  createdAt: string;
}

interface Category {
  id: string;
  name: string;
}

interface Tag {
  id: string;
  name: string;
}

interface HomeClientProps {
  user: any;
  initialTopics: {
    items: TopicFeedItem[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  };
  initialPersonas: {
    items: PersonaFeedItem[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  };
  categories: Category[];
  tags: Tag[];
}

export function HomeClient({
  user,
  initialTopics,
  initialPersonas,
  categories,
  tags,
}: HomeClientProps) {
  const router = useRouter();

  const [mode, setMode] = useState<ViewMode>("feed");

  // Topic Feed state
  const [feedSearchTerm, setFeedSearchTerm] = useState("");
  const [feedCategoryId, setFeedCategoryId] = useState<string | undefined>(undefined);
  const [feedTagIds, setFeedTagIds] = useState<string[]>([]);
  const [feedSortBy, setFeedSortBy] = useState("recent");

  // Personas state
  const [personaSearchTerm, setPersonaSearchTerm] = useState("");
  const [personaCategoryId, setPersonaCategoryId] = useState<string | undefined>(undefined);
  const [personaSortBy, setPersonaSortBy] = useState("recent");

  // Popular Topics state
  const [popularSearchTerm, setPopularSearchTerm] = useState("");
  const [popularCategoryId, setPopularCategoryId] = useState<string | undefined>(undefined);
  const [popularTagIds, setPopularTagIds] = useState<string[]>([]);
  const [popularSortBy, setPopularSortBy] = useState("popular");

  // Fetch function for topic feed (new topics)
  const fetchTopics = useCallback(
    async (page: number) => {
      return await fetchTopicFeed(
        page,
        10,
        feedCategoryId,
        feedTagIds,
        feedSearchTerm,
        feedSortBy,
        true // Only show public topics
      );
    },
    [feedCategoryId, feedTagIds, feedSearchTerm, feedSortBy]
  );

  // Fetch function for personas
  const fetchPersonas = useCallback(
    async (page: number) => {
      return await fetchPersonaFeed(
        page,
        10,
        personaSearchTerm,
        personaCategoryId,
        personaSortBy
      );
    },
    [personaSearchTerm, personaCategoryId, personaSortBy]
  );

  // Fetch function for popular topics
  const fetchPopularTopics = useCallback(
    async (page: number) => {
      return await fetchTopicFeed(
        page,
        10,
        popularCategoryId,
        popularTagIds,
        popularSearchTerm,
        popularSortBy,
        true // Only show public topics
      );
    },
    [popularCategoryId, popularTagIds, popularSearchTerm, popularSortBy]
  );

  // Use infinite scroll hook for topic feed
  const {
    items: topics,
    loading: topicsLoading,
    hasMore: topicsHasMore,
    error: topicsError,
    reset: resetTopics,
    observerRef: topicsObserverRef,
  } = useInfiniteScroll<TopicFeedItem>({
    fetchFunction: fetchTopics,
    pageSize: 10,
    initialData: initialTopics,
  });

  // Use infinite scroll hook for personas
  const {
    items: personas,
    loading: personasLoading,
    hasMore: personasHasMore,
    error: personasError,
    reset: resetPersonas,
    observerRef: personasObserverRef,
  } = useInfiniteScroll<PersonaFeedItem>({
    fetchFunction: fetchPersonas,
    pageSize: 10,
    initialData: initialPersonas,
  });

  // Use infinite scroll hook for popular topics
  const {
    items: popularTopics,
    loading: popularLoading,
    hasMore: popularHasMore,
    error: popularError,
    reset: resetPopular,
    observerRef: popularObserverRef,
  } = useInfiniteScroll<TopicFeedItem>({
    fetchFunction: fetchPopularTopics,
    pageSize: 10,
    initialData: initialTopics, // Reuse initial data
  });

  // Track if this is the first mount to avoid resetting on initial load
  const isFirstMountTopics = useRef(true);
  const isFirstMountPersonas = useRef(true);
  const isFirstMountPopular = useRef(true);

  // Reset topics when filters change (but not on initial mount)
  useEffect(() => {
    if (isFirstMountTopics.current) {
      isFirstMountTopics.current = false;
      return;
    }
    resetTopics();
  }, [feedCategoryId, feedTagIds, feedSearchTerm, feedSortBy, resetTopics]);

  // Reset personas when filters change (but not on initial mount)
  useEffect(() => {
    if (isFirstMountPersonas.current) {
      isFirstMountPersonas.current = false;
      return;
    }
    resetPersonas();
  }, [personaSearchTerm, personaCategoryId, personaSortBy, resetPersonas]);

  // Reset popular topics when filters change (but not on initial mount)
  useEffect(() => {
    if (isFirstMountPopular.current) {
      isFirstMountPopular.current = false;
      return;
    }
    resetPopular();
  }, [popularCategoryId, popularTagIds, popularSearchTerm, popularSortBy, resetPopular]);

  const handleViewChange = (newMode: ViewMode) => {
    setMode(newMode);
  };

  // Animation variants for view transitions
  const viewVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: -20 },
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Main Content */}
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <div className="grid grid-cols-1 lg:grid-cols-7 gap-6">
          {/* Left Sidebar - View Switcher & Filters */}
          <aside className="lg:col-span-2 space-y-4">
            {/* Animated View Switcher */}
            <AnimatedViewSwitcher
              currentView={mode}
              onViewChange={handleViewChange}
            />

            {/* Filters Card */}
            <div className="bg-card rounded-lg border p-4 space-y-4">
              <h3 className="font-semibold text-lg">Filters</h3>

              {/* Topic Feed Filters */}
              {mode === "feed" && (
                <div className="space-y-3">
                  <SearchBar
                    value={feedSearchTerm}
                    onChange={setFeedSearchTerm}
                    placeholder="Search topics..."
                  />
                  <CategorySelector
                    availableCategories={categories}
                    selectedCategoryId={feedCategoryId}
                    onCategoryChange={setFeedCategoryId}
                  />
                  <TagSelector
                    availableTags={tags}
                    selectedTagIds={feedTagIds}
                    onTagChange={setFeedTagIds}
                  />
                  <SortSelector
                    value={feedSortBy}
                    onChange={setFeedSortBy}
                    options={topicSortOptions}
                    className="w-full"
                  />
                </div>
              )}

              {/* Personas Filters */}
              {mode === "personas" && (
                <div className="space-y-3">
                  <SearchBar
                    value={personaSearchTerm}
                    onChange={setPersonaSearchTerm}
                    placeholder="Search personas..."
                  />
                  <CategorySelector
                    availableCategories={categories}
                    selectedCategoryId={personaCategoryId}
                    onCategoryChange={setPersonaCategoryId}
                  />
                  <SortSelector
                    value={personaSortBy}
                    onChange={setPersonaSortBy}
                    options={personaSortOptions}
                    className="w-full"
                  />
                </div>
              )}

              {/* Popular Topics Filters */}
              {mode === "popular" && (
                <div className="space-y-3">
                  <SearchBar
                    value={popularSearchTerm}
                    onChange={setPopularSearchTerm}
                    placeholder="Search popular topics..."
                  />
                  <CategorySelector
                    availableCategories={categories}
                    selectedCategoryId={popularCategoryId}
                    onCategoryChange={setPopularCategoryId}
                  />
                  <TagSelector
                    availableTags={tags}
                    selectedTagIds={popularTagIds}
                    onTagChange={setPopularTagIds}
                  />
                  <SortSelector
                    value={popularSortBy}
                    onChange={setPopularSortBy}
                    options={topicSortOptions}
                    className="w-full"
                  />
                </div>
              )}
            </div>
          </aside>

          {/* Main Content Area */}
          <main className="lg:col-span-5">
            <div className="bg-card rounded-lg border p-6">
              <AnimatePresence mode="wait">
              {/* Topic Feed View */}
              {mode === "feed" && (
                <motion.div
                  key="feed"
                  variants={viewVariants}
                  initial="hidden"
                  animate="visible"
                  exit="exit"
                  transition={{ duration: 0.3 }}
                >
                  {topicsError && (
                    <div className="text-center py-8 text-destructive">
                      <p>Error loading topics. Please try again.</p>
                    </div>
                  )}

                  {topics.length === 0 && !topicsLoading && (
                    <div className="text-center py-12">
                      <p className="text-muted-foreground">
                        No topics found. Try adjusting your filters.
                      </p>
                    </div>
                  )}

                  {topics.length > 0 && (
                    <div className="space-y-4">
                      {topics.map((topic) => (
                        <TopicFeedCard
                          key={topic.id}
                          topic={topic}
                        />
                      ))}
                    </div>
                  )}

                  {/* Loading Indicator */}
                  {topicsLoading && (
                    <div className="flex justify-center py-4 mt-4">
                      <Loader2 className="h-6 w-6 animate-spin" />
                    </div>
                  )}

                  {/* Infinite Scroll Trigger */}
                  {topicsHasMore && !topicsLoading && (
                    <div ref={topicsObserverRef} className="h-4" />
                  )}
                </motion.div>
              )}

              {/* Personas View */}
              {mode === "personas" && (
                <motion.div
                  key="personas"
                  variants={viewVariants}
                  initial="hidden"
                  animate="visible"
                  exit="exit"
                  transition={{ duration: 0.3 }}
                  className="space-y-4"
                >
                  {personasError && (
                    <div className="text-center py-8 text-destructive">
                      <p>Error loading personas. Please try again.</p>
                    </div>
                  )}

                  {personas.length === 0 && !personasLoading && (
                    <div className="text-center py-12">
                      <p className="text-muted-foreground">
                        No personas found. Try adjusting your search.
                      </p>
                    </div>
                  )}

                  {personas.map((persona) => (
                    <PersonaFeedCard
                      key={persona.id}
                      persona={persona}
                    />
                  ))}

                  {/* Loading Indicator */}
                  {personasLoading && (
                    <div className="flex justify-center py-4">
                      <Loader2 className="h-6 w-6 animate-spin" />
                    </div>
                  )}

                  {/* Infinite Scroll Trigger */}
                  {personasHasMore && !personasLoading && (
                    <div ref={personasObserverRef} className="h-4" />
                  )}
                </motion.div>
              )}

              {/* Popular Topics View */}
              {mode === "popular" && (
                <motion.div
                  key="popular"
                  variants={viewVariants}
                  initial="hidden"
                  animate="visible"
                  exit="exit"
                  transition={{ duration: 0.3 }}
                  className="space-y-4"
                >
                  {popularError && (
                    <div className="text-center py-8 text-destructive">
                      <p>Error loading popular topics. Please try again.</p>
                    </div>
                  )}

                  {popularTopics.length === 0 && !popularLoading && (
                    <div className="text-center py-12">
                      <p className="text-muted-foreground">
                        No popular topics found. Try adjusting your filters.
                      </p>
                    </div>
                  )}

                  {popularTopics.length > 0 && (
                    <PopularTopicsList
                      topics={popularTopics}
                      onTopicClick={(topicId, personaId) =>
                        router.push(`/personas/${personaId}/chat`)
                      }
                    />
                  )}

                  {/* Loading Indicator */}
                  {popularLoading && (
                    <div className="flex justify-center py-4">
                      <Loader2 className="h-6 w-6 animate-spin" />
                    </div>
                  )}

                  {/* Infinite Scroll Trigger */}
                  {popularHasMore && !popularLoading && (
                    <div ref={popularObserverRef} className="h-4" />
                  )}
                </motion.div>
              )}
            </AnimatePresence>
            </div>
          </main>
        </div>
      </div>
    </div>
  );
}
