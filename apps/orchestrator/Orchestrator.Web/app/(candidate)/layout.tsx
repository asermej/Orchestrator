import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Interview",
  description: "Complete your interview",
};

/**
 * Minimal layout for candidate-facing interview pages.
 * No sidebar, no admin navigation. Renders inside the root layout.
 */
export default function CandidateLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {children}
    </div>
  );
}
