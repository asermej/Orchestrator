"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface GroupItem {
  id: string;
  name: string;
}

interface OrganizationItem {
  id: string;
  groupId: string;
  name: string;
  city?: string | null;
  state?: string | null;
}

interface MeResponse {
  user: { id: string; name?: string | null; email?: string | null };
  accessibleGroups: GroupItem[];
  accessibleOrganizations: OrganizationItem[];
  currentContext: { selectedOrganizationId?: string | null };
}

export function Header() {
  const [me, setMe] = useState<MeResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [switcherOpen, setSwitcherOpen] = useState(false);

  const loadMe = async () => {
    setLoading(true);
    try {
      const data = await testAtsApi.get<MeResponse>("/api/me");
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

  const setContext = async (organizationId: string | null) => {
    try {
      await testAtsApi.put("/api/me/context", {
        selectedOrganizationId: organizationId || undefined,
      });
      await loadMe();
      setSwitcherOpen(false);
      window.dispatchEvent(new CustomEvent("test-ats-context-changed"));
    } catch {
      // ignore
    }
  };

  const selectedOrg = me?.currentContext?.selectedOrganizationId
    ? me.accessibleOrganizations.find(
        (o) => o.id === me.currentContext.selectedOrganizationId
      )
    : null;
  const displayName =
    selectedOrg?.name +
    (selectedOrg?.city || selectedOrg?.state
      ? ` ${[selectedOrg.city, selectedOrg.state].filter(Boolean).join(", ")}`
      : "") ||
    "Select location";

  return (
    <nav className="border-b border-slate-200 bg-white">
      <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between gap-6">
        <div className="flex items-center gap-6">
          <a href="/" className="font-semibold text-slate-900">
            Hireology Test ATS
          </a>
          <a href="/jobs" className="text-slate-600 hover:text-slate-900">
            Jobs
          </a>
          <a href="/applicants" className="text-slate-600 hover:text-slate-900">
            Applicants
          </a>
          <a href="/settings" className="text-slate-600 hover:text-slate-900">
            Settings
          </a>
          <a href="/management" className="text-slate-600 hover:text-slate-900">
            Management
          </a>
          <a href="/careers" className="text-slate-600 hover:text-slate-900">
            Careers
          </a>
        </div>
        <div className="flex items-center gap-4">
          {loading ? (
            <span className="text-slate-500 text-sm">Loading…</span>
          ) : me ? (
            <>
              <div className="relative">
                <button
                  type="button"
                  onClick={() => setSwitcherOpen(!switcherOpen)}
                  className="flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 bg-white text-left text-sm min-w-[200px] hover:bg-slate-50"
                >
                  <span className="truncate flex-1">{displayName}</span>
                  <span className="text-slate-400">▼</span>
                </button>
                {switcherOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-10"
                      aria-hidden
                      onClick={() => setSwitcherOpen(false)}
                    />
                    <div className="absolute right-0 top-full mt-1 py-1 bg-white border border-slate-200 rounded-lg shadow-lg z-20 max-h-80 overflow-auto min-w-[260px]">
                      {me.accessibleGroups.length === 0 && me.accessibleOrganizations.length === 0 ? (
                        <div className="px-3 py-4 text-slate-500 text-sm">
                          No locations assigned. Ask an admin to assign you to a group or location in Management → Users.
                        </div>
                      ) : (
                      <>
                      {me.accessibleGroups.map((group) => {
                        const orgs = me.accessibleOrganizations.filter(
                          (o) => o.groupId === group.id
                        );
                        return (
                          <div key={group.id} className="py-1">
                            <div className="px-3 py-1 text-xs font-medium text-slate-500 uppercase tracking-wide">
                              {group.name} ({orgs.length})
                            </div>
                            {orgs.map((org) => (
                              <button
                                key={org.id}
                                type="button"
                                onClick={() => setContext(org.id)}
                                className="w-full px-3 py-2 text-left text-sm hover:bg-slate-100 flex items-center gap-2"
                              >
                                {me.currentContext?.selectedOrganizationId ===
                                org.id ? (
                                  <span className="text-green-600">✓</span>
                                ) : null}
                                <span>
                                  {org.name}
                                  {(org.city || org.state) &&
                                    ` — ${[org.city, org.state].filter(Boolean).join(", ")}`}
                                </span>
                              </button>
                            ))}
                          </div>
                        );
                      })}
                      {me.accessibleOrganizations.filter(
                        (o) =>
                          !me.accessibleGroups.some((g) => g.id === o.groupId)
                      ).length > 0 && (
                        <div className="border-t border-slate-100 pt-1">
                          <div className="px-3 py-1 text-xs font-medium text-slate-500">
                            Other locations
                          </div>
                          {me.accessibleOrganizations
                            .filter(
                              (o) =>
                                !me.accessibleGroups.some(
                                  (g) => g.id === o.groupId
                                )
                            )
                            .map((org) => (
                              <button
                                key={org.id}
                                type="button"
                                onClick={() => setContext(org.id)}
                                className="w-full px-3 py-2 text-left text-sm hover:bg-slate-100"
                              >
                                {org.name}
                              </button>
                            ))}
                        </div>
                      )}
                      </>
                      )}
                    </div>
                  </>
                )}
              </div>
              <span className="text-slate-600 text-sm">
                {me.user.name || me.user.email || "User"}
              </span>
              <a
                href="/api/auth/logout"
                className="text-slate-500 hover:text-slate-700 text-sm"
              >
                Log out
              </a>
            </>
          ) : (
            <a
              href="/api/auth/login"
              className="px-3 py-2 rounded-lg bg-indigo-600 text-white text-sm hover:bg-indigo-700"
            >
              Log in
            </a>
          )}
        </div>
      </div>
    </nav>
  );
}
