"use client";

import { useEffect, useState, createContext, useContext } from "react";
import { testAtsApi } from "@/lib/test-ats-api";
import { AppSidebar } from "./app-sidebar";
import { Briefcase, Users, Globe, Settings, ArrowRight } from "lucide-react";

// ── Types (shared with sidebar) ────────────────────────────────────────────

export interface GroupItem {
  id: string;
  rootOrganizationId?: string | null;
  name: string;
}

export interface OrganizationItem {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

export interface MeResponse {
  user: { id: string; name?: string | null; email?: string | null };
  isSuperadmin: boolean;
  isGroupAdmin: boolean;
  adminGroupIds: string[];
  accessibleGroups: GroupItem[];
  accessibleOrganizations: OrganizationItem[];
  currentContext: { selectedOrganizationId?: string | null };
}

// ── Context so children can access me data ─────────────────────────────────

const MeContext = createContext<MeResponse | null>(null);
export function useMe() {
  return useContext(MeContext);
}

// ── Component ──────────────────────────────────────────────────────────────

export function AppShell({ children }: { children: React.ReactNode }) {
  const [me, setMe] = useState<MeResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const loadMe = async () => {
    setLoading(true);
    try {
      const data = await testAtsApi.get<MeResponse>("/api/v1/me");
      setMe(data);
    } catch {
      setMe(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMe();
  }, []);

  // ── Loading state ──────────────────────────────────────────────────────

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="text-center">
          <div className="inline-flex h-10 w-10 items-center justify-center rounded-lg bg-indigo-100 mb-4">
            <Briefcase className="h-5 w-5 text-indigo-600" />
          </div>
          <p className="text-sm text-slate-500">Loading...</p>
        </div>
      </div>
    );
  }

  // ── Unauthenticated: full-page login ───────────────────────────────────

  if (!me) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100">
        <div className="w-full max-w-md px-6">
          <div className="rounded-2xl bg-white shadow-xl shadow-slate-200/50 border border-slate-200/60 p-8">
            {/* Logo / Branding */}
            <div className="text-center mb-8">
              <div className="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-600 mb-4">
                <Briefcase className="h-7 w-7 text-white" />
              </div>
              <h1 className="text-2xl font-bold text-slate-900">
                Hireology Test ATS
              </h1>
              <p className="mt-2 text-sm text-slate-500">
                Test applicant tracking system for Orchestrator integration
              </p>
            </div>

            {/* Login button */}
            <a
              href="/api/auth/login"
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-indigo-600 px-4 py-3 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 transition-colors"
            >
              Sign in to continue
              <ArrowRight className="h-4 w-4" />
            </a>

            {/* Feature hints */}
            <div className="mt-8 grid grid-cols-2 gap-3">
              {[
                { icon: Briefcase, label: "Jobs" },
                { icon: Users, label: "Applicants" },
                { icon: Settings, label: "Settings" },
                { icon: Globe, label: "Careers" },
              ].map((item) => (
                <div
                  key={item.label}
                  className="flex items-center gap-2 rounded-lg bg-slate-50 px-3 py-2"
                >
                  <item.icon className="h-3.5 w-3.5 text-slate-400" />
                  <span className="text-xs text-slate-500">{item.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  // ── Authenticated: sidebar + content ───────────────────────────────────

  return (
    <MeContext.Provider value={me}>
      <div className="flex min-h-screen">
        <AppSidebar me={me} onMeChange={loadMe} />
        <main className="flex-1 overflow-auto px-8 py-8 max-w-6xl">
          {children}
        </main>
      </div>
    </MeContext.Provider>
  );
}
