import { useState, useEffect, useCallback, useRef } from "react";

interface UseInfiniteScrollOptions<T> {
  fetchFunction: (page: number) => Promise<{ items: T[]; totalCount: number; pageSize: number }>;
  pageSize?: number;
  initialPage?: number;
  initialData?: { items: T[]; totalCount: number; pageNumber: number; pageSize: number };
}

interface UseInfiniteScrollReturn<T> {
  items: T[];
  loading: boolean;
  hasMore: boolean;
  error: string | null;
  loadMore: () => void;
  reset: () => void;
  observerRef: (node: HTMLElement | null) => void;
}

export function useInfiniteScroll<T>({
  fetchFunction,
  pageSize = 10,
  initialPage = 1,
  initialData,
}: UseInfiniteScrollOptions<T>): UseInfiniteScrollReturn<T> {
  const [items, setItems] = useState<T[]>(initialData?.items || []);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(initialData?.pageNumber || initialPage);
  const [totalCount, setTotalCount] = useState(initialData?.totalCount || 0);
  const [hasMore, setHasMore] = useState(() => {
    if (initialData) {
      const totalPages = Math.ceil(initialData.totalCount / initialData.pageSize);
      return initialData.pageNumber < totalPages;
    }
    return true;
  });

  const observer = useRef<IntersectionObserver | null>(null);
  const loadingRef = useRef(false);

  // Ref to track if this is the initial load
  const isInitialMount = useRef(true);

  const loadPage = useCallback(
    async (page: number, append = true) => {
      if (loadingRef.current) return;

      try {
        loadingRef.current = true;
        setLoading(true);
        setError(null);

        const result = await fetchFunction(page);

        setItems((prevItems) => (append ? [...prevItems, ...result.items] : result.items));
        setTotalCount(result.totalCount);
        setCurrentPage(page);

        // Calculate if there are more pages
        const totalPages = Math.ceil(result.totalCount / result.pageSize);
        setHasMore(page < totalPages);
      } catch (err) {
        setError(err instanceof Error ? err.message : "An error occurred");
        setHasMore(false);
      } finally {
        setLoading(false);
        loadingRef.current = false;
      }
    },
    [fetchFunction]
  );

  // Store initialData in a ref to avoid re-running effect on re-renders
  const hasInitialDataRef = useRef(!!initialData);

  // Initial load - skip if initialData provided
  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      // Only fetch if no initialData was provided
      if (!hasInitialDataRef.current) {
        loadPage(initialPage, false);
      }
    }
  }, [loadPage, initialPage]); // Removed initialData from deps

  const loadMore = useCallback(() => {
    if (hasMore && !loading) {
      loadPage(currentPage + 1, true);
    }
  }, [hasMore, loading, currentPage, loadPage]);

  const reset = useCallback(() => {
    setItems([]);
    setCurrentPage(initialPage);
    setTotalCount(0);
    setHasMore(true);
    setError(null);
    loadPage(initialPage, false);
  }, [loadPage, initialPage]);

  // Intersection Observer callback
  const observerRef = useCallback(
    (node: HTMLElement | null) => {
      if (loading) return;
      if (observer.current) observer.current.disconnect();

      observer.current = new IntersectionObserver(
        (entries) => {
          if (entries[0].isIntersecting && hasMore && !loadingRef.current) {
            loadMore();
          }
        },
        {
          rootMargin: "200px", // Start loading 200px before the element is visible
        }
      );

      if (node) observer.current.observe(node);
    },
    [loading, hasMore, loadMore]
  );

  // Cleanup
  useEffect(() => {
    return () => {
      if (observer.current) {
        observer.current.disconnect();
      }
    };
  }, []);

  return {
    items,
    loading,
    hasMore,
    error,
    loadMore,
    reset,
    observerRef,
  };
}

