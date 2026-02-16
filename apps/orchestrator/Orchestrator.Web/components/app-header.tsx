"use client";

import { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { ArrowLeft, ChevronDown, MapPin } from "lucide-react";
import { selectOrganization } from "@/lib/group-context-actions";

interface GroupInfo {
  id: string;
  name: string;
}

interface OrgItem {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

interface OrgTreeNode extends OrgItem {
  children: OrgTreeNode[];
  depth: number;
}

interface AppHeaderProps {
  groupInfo?: GroupInfo | null;
  returnUrl?: string | null;
  organizations?: OrgItem[];
  selectedOrgId?: string | null;
  currentGroupRootOrganizationId?: string | null;
}

export function AppHeader({
  groupInfo,
  returnUrl,
  organizations = [],
  selectedOrgId,
  currentGroupRootOrganizationId = null,
}: AppHeaderProps) {
  const [switcherOpen, setSwitcherOpen] = useState(false);

  const rootOrgIds = new Set<string>();
  if (currentGroupRootOrganizationId) rootOrgIds.add(currentGroupRootOrganizationId);

  function buildOrgTree(orgs: OrgItem[]): OrgTreeNode[] {
    // Exclude root org(s): by id when we have currentGroupRootOrganizationId, and always exclude
    // orgs with no parent so the group/root never appears as a selectable option (section header only).
    const nonRoot = orgs.filter(
      (o) => !rootOrgIds.has(o.id) && o.parentOrganizationId != null && o.parentOrganizationId !== ""
    );
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

  const tree = buildOrgTree(organizations);
  const flatOrgs = flattenTree(tree);

  // Auto-select the first organization when none is selected (matches ATS behavior
  // where a location is always selected — there is no "All locations" option).
  const autoSelectDone = useRef(false);
  useEffect(() => {
    if (!selectedOrgId && flatOrgs.length > 0 && !autoSelectDone.current) {
      autoSelectDone.current = true;
      selectOrganization(flatOrgs[0].id);
    }
  }, [selectedOrgId, flatOrgs]);

  const selectedOrg = selectedOrgId
    ? organizations.find((o) => o.id === selectedOrgId)
    : null;

  const locationLabel = selectedOrg
    ? selectedOrg.name +
      (selectedOrg.city || selectedOrg.state
        ? ` — ${[selectedOrg.city, selectedOrg.state].filter(Boolean).join(", ")}`
        : "")
    : flatOrgs.length > 0
      ? flatOrgs[0].name +
        (flatOrgs[0].city || flatOrgs[0].state
          ? ` — ${[flatOrgs[0].city, flatOrgs[0].state].filter(Boolean).join(", ")}`
          : "")
      : groupInfo?.name ?? "No group";

  const handleSelectOrg = async (orgId: string) => {
    setSwitcherOpen(false);
    await selectOrganization(orgId);
  };

  const backToHireologyUrl =
    returnUrl && selectedOrgId
      ? `${returnUrl}${returnUrl.includes("?") ? "&" : "?"}organizationId=${encodeURIComponent(selectedOrgId)}`
      : returnUrl;

  return (
    <header className="flex h-14 items-center border-b bg-sidebar text-sidebar-foreground px-4 shrink-0">
      {/* Left section: back link + branding */}
      <div className="flex items-center gap-4">
        {backToHireologyUrl && (
          <a
            href={backToHireologyUrl}
            className="flex items-center gap-1.5 text-xs font-medium text-sidebar-foreground/50 hover:text-sidebar-foreground/80 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Hireology</span>
          </a>
        )}
        {returnUrl && (
          <div className="h-5 w-px bg-sidebar-foreground/15" />
        )}
        <Link href="/my-agents" className="text-base font-semibold tracking-tight hover:opacity-80 transition-opacity">
          AI Assistants
        </Link>
      </div>

      {/* Right section: org switcher + user */}
      <div className="ml-auto flex items-center gap-3">
        {/* Organization switcher: show when we have orgs or when we have group context (so dropdown + Back link are consistent) */}
        {(organizations.length > 0 || groupInfo) && (
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
                  {(flatOrgs.length > 0 || groupInfo) && (
                    <>
                      {groupInfo && (
                        <div className="px-3 py-1 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                          {groupInfo.name}
                        </div>
                      )}
                      {flatOrgs.map((org) => {
                        const isSelected = selectedOrgId === org.id;
                        const hasChildren = org.children.length > 0;
                        const indent = 12 + (org.depth + 1) * 16;
                        return (
                          <button
                            key={org.id}
                            type="button"
                            onClick={() => handleSelectOrg(org.id)}
                            className={`w-full py-1.5 pr-3 text-left text-xs flex items-center gap-2 transition-colors ${
                              isSelected
                                ? "bg-indigo-50 text-indigo-900"
                                : "text-slate-700 hover:bg-slate-50"
                            }`}
                            style={{ paddingLeft: `${indent}px` }}
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
                    </>
                  )}
                </div>
              </>
            )}
          </div>
        )}

        {/* Group badge (when no orgs or as subtle context) */}
        {organizations.length === 0 && groupInfo && (
          <div className="flex items-center gap-2 rounded-lg border border-sidebar-foreground/10 bg-sidebar-accent/30 px-3 py-1.5">
            <span className="text-xs font-medium text-sidebar-foreground/60">Group:</span>
            <span className="text-xs font-semibold truncate max-w-[200px]" title={groupInfo.name}>
              {groupInfo.name}
            </span>
          </div>
        )}

      </div>
    </header>
  );
}
