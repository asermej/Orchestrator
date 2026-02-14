"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface GroupItem {
  id: string;
  rootOrganizationId?: string | null;
  name: string;
}

interface OrganizationItem {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

interface MeResponse {
  user: { id: string; name?: string | null; email?: string | null };
  isSuperadmin: boolean;
  isGroupAdmin: boolean;
  adminGroupIds: string[];
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

  const setContext = async (organizationId: string | null) => {
    try {
      await testAtsApi.put("/api/v1/me/context", {
        selectedOrganizationId: organizationId || undefined,
      });
      await loadMe();
      setSwitcherOpen(false);
      window.dispatchEvent(new CustomEvent("test-ats-context-changed"));
    } catch {
      // ignore
    }
  };

  // Collect root org IDs so we can exclude them from the location picker
  const rootOrgIds = new Set(
    (me?.accessibleGroups ?? [])
      .map((g) => g.rootOrganizationId)
      .filter(Boolean) as string[]
  );

  // Build a tree of non-root orgs for each group so we can show hierarchy
  interface OrgTreeNode extends OrganizationItem {
    children: OrgTreeNode[];
    depth: number;
  }

  function buildOrgTree(orgs: OrganizationItem[]): OrgTreeNode[] {
    const nonRoot = orgs.filter((o) => !rootOrgIds.has(o.id));
    const map = new Map<string, OrgTreeNode>();
    for (const o of nonRoot) {
      map.set(o.id, { ...o, children: [], depth: 0 });
    }
    const roots: OrgTreeNode[] = [];
    for (const o of nonRoot) {
      const node = map.get(o.id)!;
      // If the parent is another non-root org, nest under it
      if (o.parentOrganizationId && map.has(o.parentOrganizationId)) {
        map.get(o.parentOrganizationId)!.children.push(node);
      } else {
        // Parent is root org or null — this is a top-level location
        roots.push(node);
      }
    }
    // Set depths recursively
    function setDepths(nodes: OrgTreeNode[], depth: number) {
      for (const n of nodes) {
        n.depth = depth;
        n.children.sort((a, b) => a.name.localeCompare(b.name));
        setDepths(n.children, depth + 1);
      }
    }
    roots.sort((a, b) => a.name.localeCompare(b.name));
    setDepths(roots, 0);
    return roots;
  }

  // Flatten tree into an ordered list with depth for rendering
  function flattenTree(nodes: OrgTreeNode[]): OrgTreeNode[] {
    const result: OrgTreeNode[] = [];
    function walk(list: OrgTreeNode[]) {
      for (const n of list) {
        result.push(n);
        walk(n.children);
      }
    }
    walk(nodes);
    return result;
  }

  const selectedOrg = me?.currentContext?.selectedOrganizationId
    ? me.accessibleOrganizations.find(
        (o) => o.id === me.currentContext.selectedOrganizationId
      )
    : null;
  const displayName = selectedOrg
    ? selectedOrg.name +
      (selectedOrg.city || selectedOrg.state
        ? ` — ${[selectedOrg.city, selectedOrg.state].filter(Boolean).join(", ")}`
        : "")
    : "Select location";

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
          {me?.isGroupAdmin && (
            <a href="/group-admin" className="text-emerald-600 hover:text-emerald-800 font-medium">
              Group Admin
            </a>
          )}
          {me?.isSuperadmin && (
            <a href="/admin" className="text-amber-600 hover:text-amber-800 font-medium">
              Admin
            </a>
          )}
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
                  className={`flex items-center gap-2 px-3 py-2 rounded-lg border text-left text-sm min-w-[220px] max-w-[280px] transition-colors ${
                    selectedOrg
                      ? "border-indigo-300 bg-indigo-50 text-indigo-900 hover:bg-indigo-100"
                      : "border-slate-300 bg-white text-slate-500 hover:bg-slate-50"
                  }`}
                >
                  {selectedOrg && (
                    <span className="w-2 h-2 rounded-full bg-indigo-500 shrink-0" />
                  )}
                  <span className="truncate flex-1 font-medium">{displayName}</span>
                  <svg className="w-4 h-4 text-slate-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                {switcherOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-10"
                      aria-hidden
                      onClick={() => setSwitcherOpen(false)}
                    />
                    <div className="absolute right-0 top-full mt-1 bg-white border border-slate-200 rounded-lg shadow-lg z-20 max-h-80 overflow-auto min-w-[280px]">
                      <div className="px-3 py-2 border-b border-slate-100">
                        <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">
                          Select a location
                        </span>
                      </div>
                      {me.accessibleGroups.length === 0 && me.accessibleOrganizations.length === 0 ? (
                        <div className="px-3 py-4 text-slate-500 text-sm">
                          No locations assigned. Ask an admin to assign you to a group or location in Management → Users.
                        </div>
                      ) : (
                      <>
                      {me.accessibleGroups.map((group) => {
                        const groupOrgs = me.accessibleOrganizations.filter(
                          (o) => o.groupId === group.id
                        );
                        const tree = buildOrgTree(groupOrgs);
                        const flat = flattenTree(tree);
                        if (flat.length === 0) return null;
                        return (
                          <div key={group.id} className="py-1">
                            <div className="px-3 py-1.5 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                              {group.name}
                            </div>
                            {flat.map((org) => {
                              const isSelected =
                                me.currentContext?.selectedOrganizationId === org.id;
                              const hasChildren = org.children.length > 0;
                              const indent = org.depth * 20;
                              return (
                                <button
                                  key={org.id}
                                  type="button"
                                  onClick={() => setContext(org.id)}
                                  className={`w-full py-2 pr-3 text-left text-sm flex items-center gap-2 transition-colors ${
                                    isSelected
                                      ? "bg-indigo-50 text-indigo-900"
                                      : "text-slate-700 hover:bg-slate-50"
                                  }`}
                                  style={{ paddingLeft: `${12 + indent}px` }}
                                >
                                  <span
                                    className={`w-4 h-4 rounded-full border-2 shrink-0 flex items-center justify-center ${
                                      isSelected
                                        ? "border-indigo-500"
                                        : "border-slate-300"
                                    }`}
                                  >
                                    {isSelected && (
                                      <span className="w-2 h-2 rounded-full bg-indigo-500" />
                                    )}
                                  </span>
                                  <span className={isSelected ? "font-medium" : ""}>
                                    {org.name}
                                    {hasChildren && (
                                      <span className="text-xs text-slate-400 font-normal ml-1">
                                        ▸ {org.children.length}
                                      </span>
                                    )}
                                    {(org.city || org.state) && (
                                      <span className="text-slate-400 font-normal">
                                        {" — "}
                                        {[org.city, org.state]
                                          .filter(Boolean)
                                          .join(", ")}
                                      </span>
                                    )}
                                  </span>
                                </button>
                              );
                            })}
                          </div>
                        );
                      })}
                      {(() => {
                        const ungrouped = me.accessibleOrganizations.filter(
                          (o) =>
                            !rootOrgIds.has(o.id) &&
                            !me.accessibleGroups.some((g) => g.id === o.groupId)
                        );
                        if (ungrouped.length === 0) return null;
                        const tree = buildOrgTree(ungrouped);
                        const flat = flattenTree(tree);
                        return (
                          <div className="border-t border-slate-100 pt-1">
                            <div className="px-3 py-1.5 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                              Other locations
                            </div>
                            {flat.map((org) => {
                              const isSelected =
                                me.currentContext?.selectedOrganizationId === org.id;
                              const hasChildren = org.children.length > 0;
                              const indent = org.depth * 20;
                              return (
                                <button
                                  key={org.id}
                                  type="button"
                                  onClick={() => setContext(org.id)}
                                  className={`w-full py-2 pr-3 text-left text-sm flex items-center gap-2 transition-colors ${
                                    isSelected
                                      ? "bg-indigo-50 text-indigo-900"
                                      : "text-slate-700 hover:bg-slate-50"
                                  }`}
                                  style={{ paddingLeft: `${12 + indent}px` }}
                                >
                                  <span
                                    className={`w-4 h-4 rounded-full border-2 shrink-0 flex items-center justify-center ${
                                      isSelected
                                        ? "border-indigo-500"
                                        : "border-slate-300"
                                    }`}
                                  >
                                    {isSelected && (
                                      <span className="w-2 h-2 rounded-full bg-indigo-500" />
                                    )}
                                  </span>
                                  <span className={isSelected ? "font-medium" : ""}>
                                    {org.name}
                                    {hasChildren && (
                                      <span className="text-xs text-slate-400 font-normal ml-1">
                                        ▸ {org.children.length}
                                      </span>
                                    )}
                                  </span>
                                </button>
                              );
                            })}
                          </div>
                        );
                      })()}
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
