"use client";

import { useState } from "react";
import Link from "next/link";
import { testAtsApi } from "@/lib/test-ats-api";
import { ChevronDown, MapPin } from "lucide-react";
import type {
  MeResponse,
  OrganizationItem,
} from "./app-shell";

// ── Types ──────────────────────────────────────────────────────────────────

interface OrgTreeNode extends OrganizationItem {
  children: OrgTreeNode[];
  depth: number;
}

interface AppHeaderProps {
  me: MeResponse;
  onMeChange: () => Promise<void>;
}

// ── Component ──────────────────────────────────────────────────────────────

export function AppHeader({ me, onMeChange }: AppHeaderProps) {
  const [switcherOpen, setSwitcherOpen] = useState(false);

  // ── Org tree helpers ───────────────────────────────────────────────────

  const rootOrgIds = new Set(
    (me.accessibleGroups ?? [])
      .map((g) => g.rootOrganizationId)
      .filter(Boolean) as string[]
  );

  function buildOrgTree(orgs: OrganizationItem[]): OrgTreeNode[] {
    const nonRoot = orgs.filter((o) => !rootOrgIds.has(o.id));
    const map = new Map<string, OrgTreeNode>();
    for (const o of nonRoot) {
      map.set(o.id, { ...o, children: [], depth: 0 });
    }
    const roots: OrgTreeNode[] = [];
    for (const o of nonRoot) {
      const node = map.get(o.id)!;
      if (o.parentOrganizationId && map.has(o.parentOrganizationId)) {
        map.get(o.parentOrganizationId)!.children.push(node);
      } else {
        roots.push(node);
      }
    }
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

  // ── Context switching ──────────────────────────────────────────────────

  const setContext = async (organizationId: string | null) => {
    try {
      await testAtsApi.put("/api/v1/me/context", {
        selectedOrganizationId: organizationId || undefined,
      });
      await onMeChange();
      setSwitcherOpen(false);
      window.dispatchEvent(new CustomEvent("test-ats-context-changed"));
    } catch {
      // ignore
    }
  };

  // ── Derived state ──────────────────────────────────────────────────────

  const selectedOrg = me.currentContext?.selectedOrganizationId
    ? me.accessibleOrganizations.find(
        (o) => o.id === me.currentContext.selectedOrganizationId
      )
    : null;

  const locationLabel = selectedOrg
    ? selectedOrg.name +
      (selectedOrg.city || selectedOrg.state
        ? ` \u2014 ${[selectedOrg.city, selectedOrg.state].filter(Boolean).join(", ")}`
        : "")
    : "Select location";

  // ── Render ─────────────────────────────────────────────────────────────

  return (
    <header className="flex h-14 items-center border-b border-slate-200 bg-white px-4 shrink-0">
      {/* Left section: branding */}
      <div className="flex items-center gap-4">
        <Link
          href="/"
          className="text-base font-semibold tracking-tight text-slate-900 hover:opacity-80 transition-opacity"
        >
          Hireology Test ATS
        </Link>
      </div>

      {/* Right section: org switcher + user */}
      <div className="ml-auto flex items-center gap-3">
        {/* Location switcher */}
        <div className="relative">
          <button
            type="button"
            onClick={() => setSwitcherOpen(!switcherOpen)}
            className={`flex items-center gap-2 rounded-lg border px-3 py-1.5 text-left text-xs transition-colors ${
              selectedOrg
                ? "border-indigo-300 bg-indigo-50 text-indigo-900 hover:bg-indigo-100"
                : "border-slate-300 bg-white text-slate-500 hover:bg-slate-50"
            }`}
          >
            <MapPin className={`h-3.5 w-3.5 shrink-0 ${selectedOrg ? "text-indigo-500" : "text-slate-400"}`} />
            <span className="truncate max-w-[200px] font-medium">
              {locationLabel}
            </span>
            <ChevronDown
              className={`h-3.5 w-3.5 shrink-0 text-slate-400 transition-transform ${
                switcherOpen ? "rotate-180" : ""
              }`}
            />
          </button>

          {switcherOpen && (
            <>
              <div
                className="fixed inset-0 z-10"
                aria-hidden
                onClick={() => setSwitcherOpen(false)}
              />
              <div className="absolute right-0 top-full mt-1 w-72 bg-white border border-slate-200 rounded-lg shadow-lg z-20 max-h-72 overflow-auto">
                <div className="px-3 py-2 border-b border-slate-100">
                  <span className="text-[10px] font-medium text-slate-400 uppercase tracking-wider">
                    Select a location
                  </span>
                </div>
                {me.accessibleGroups.length === 0 &&
                me.accessibleOrganizations.length === 0 ? (
                  <div className="px-3 py-4 text-slate-500 text-xs">
                    No locations assigned. Ask an admin to assign you.
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
                          <div className="px-3 py-1 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                            {group.name}
                          </div>
                          {flat.map((org) => {
                            const isSelected =
                              me.currentContext?.selectedOrganizationId === org.id;
                            const hasChildren = org.children.length > 0;
                            const indent = org.depth * 16;
                            return (
                              <button
                                key={org.id}
                                type="button"
                                onClick={() => setContext(org.id)}
                                className={`w-full py-1.5 pr-3 text-left text-xs flex items-center gap-2 transition-colors ${
                                  isSelected
                                    ? "bg-indigo-50 text-indigo-900"
                                    : "text-slate-700 hover:bg-slate-50"
                                }`}
                                style={{ paddingLeft: `${12 + indent}px` }}
                              >
                                <span
                                  className={`w-3.5 h-3.5 rounded-full border-2 shrink-0 flex items-center justify-center ${
                                    isSelected ? "border-indigo-500" : "border-slate-300"
                                  }`}
                                >
                                  {isSelected && (
                                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                                  )}
                                </span>
                                <span className={isSelected ? "font-medium" : ""}>
                                  {org.name}
                                  {hasChildren && (
                                    <span className="text-[10px] text-slate-400 font-normal ml-1">
                                      ({org.children.length})
                                    </span>
                                  )}
                                  {(org.city || org.state) && (
                                    <span className="text-slate-400 font-normal">
                                      {" \u2014 "}
                                      {[org.city, org.state].filter(Boolean).join(", ")}
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
                          <div className="px-3 py-1 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                            Other locations
                          </div>
                          {flat.map((org) => {
                            const isSelected =
                              me.currentContext?.selectedOrganizationId === org.id;
                            const hasChildren = org.children.length > 0;
                            const indent = org.depth * 16;
                            return (
                              <button
                                key={org.id}
                                type="button"
                                onClick={() => setContext(org.id)}
                                className={`w-full py-1.5 pr-3 text-left text-xs flex items-center gap-2 transition-colors ${
                                  isSelected
                                    ? "bg-indigo-50 text-indigo-900"
                                    : "text-slate-700 hover:bg-slate-50"
                                }`}
                                style={{ paddingLeft: `${12 + indent}px` }}
                              >
                                <span
                                  className={`w-3.5 h-3.5 rounded-full border-2 shrink-0 flex items-center justify-center ${
                                    isSelected ? "border-indigo-500" : "border-slate-300"
                                  }`}
                                >
                                  {isSelected && (
                                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                                  )}
                                </span>
                                <span className={isSelected ? "font-medium" : ""}>
                                  {org.name}
                                  {hasChildren && (
                                    <span className="text-[10px] text-slate-400 font-normal ml-1">
                                      ({org.children.length})
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

      </div>
    </header>
  );
}
