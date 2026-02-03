import { Suspense } from "react";
import { Loader2 } from "lucide-react";

export default function AgentsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-background flex items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin" />
        </div>
      }
    >
      {children}
    </Suspense>
  );
}

