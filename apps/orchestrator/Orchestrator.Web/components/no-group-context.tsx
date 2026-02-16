import { ShieldAlert, ArrowLeft } from "lucide-react";

/**
 * Shown when a user is logged in but has no group context set.
 * This happens when they navigate directly to AI Assistants instead of
 * jumping from the ATS. The group context is required for authorization
 * and data filtering.
 */
export function NoGroupContext() {
  const atsUrl = process.env.NEXT_PUBLIC_ATS_URL || "http://localhost:3001";

  return (
    <div className="flex items-center justify-center min-h-[80vh]">
      <div className="max-w-md text-center px-6">
        <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-amber-100">
          <ShieldAlert className="h-8 w-8 text-amber-600" />
        </div>
        <h2 className="text-2xl font-semibold text-foreground mb-3">
          No Group Context
        </h2>
        <p className="text-muted-foreground mb-6 leading-relaxed">
          To use AI Assistants, please navigate here from Hireology. Your
          group and organization access will be set automatically based on your
          permissions.
        </p>
        <a
          href={atsUrl}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          Go to Hireology
        </a>
        <p className="mt-4 text-xs text-muted-foreground">
          Look for the &quot;AI Assistants&quot; button in the top bar after
          selecting a location.
        </p>
      </div>
    </div>
  );
}
