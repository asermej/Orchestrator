"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";
import {
  Briefcase,
  Users,
  Globe,
  Settings,
  ShieldCheck,
  UserCog,
  LogOut,
  ExternalLink,
  ChevronDown,
  MapPin,
} from "lucide-react";
import type {
  MeResponse,
  OrganizationItem,
} from "./app-shell";

// ── Types ──────────────────────────────────────────────────────────────────

interface OrgTreeNode extends OrganizationItem {
  children: OrgTreeNode[];
  depth: number;
}

interface AppSidebarProps {
  me: MeResponse;
  onMeChange: () => Promise<void>;
}

// ── Nav items ──────────────────────────────────────────────────────────────

const mainNavItems = [
  { href: "/jobs", label: "Jobs", icon: Briefcase },
  { href: "/applicants", label: "Applicants", icon: Users },
  { href: "/careers", label: "Careers", icon: Globe },
];

// ── Component ──────────────────────────────────────────────────────────────

export function AppSidebar({ me, onMeChange }: AppSidebarProps) {
  const pathname = usePathname();
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

  const adminGroupIdSet = new Set(me.adminGroupIds ?? []);
  const isGroupAdminForSelectedOrg =
    selectedOrg != null && adminGroupIdSet.has(selectedOrg.groupId);

  const roleBadge: { label: string; className: string } | null =
    me.isSuperadmin
      ? {
          label: "Superadmin",
          className: "bg-amber-100 text-amber-800 border-amber-200",
        }
      : isGroupAdminForSelectedOrg
        ? {
            label: "Group Admin",
            className: "bg-emerald-100 text-emerald-800 border-emerald-200",
          }
        : null;

  const displayName = selectedOrg
    ? selectedOrg.name +
      (selectedOrg.city || selectedOrg.state
        ? ` \u2014 ${[selectedOrg.city, selectedOrg.state].filter(Boolean).join(", ")}`
        : "")
    : "Select location";

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(href + "/");
  };

  // ── Render ─────────────────────────────────────────────────────────────

  return (
    <aside className="flex h-screen w-60 flex-col border-r border-slate-200 bg-white sticky top-0 shrink-0">
      {/* ── Branding ──────────────────────────────────────────────────── */}
      <div className="flex h-14 items-center border-b border-slate-200 px-5">
        <Link
          href="/"
          className="text-lg font-semibold tracking-tight text-slate-900 hover:opacity-80 transition-opacity"
        >
          Hireology Test ATS
        </Link>
      </div>

      {/* ── Location switcher ─────────────────────────────────────────── */}
      <div className="border-b border-slate-200 px-3 py-3">
        <div className="relative">
          <button
            type="button"
            onClick={() => setSwitcherOpen(!switcherOpen)}
            className={`flex w-full items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
              selectedOrg
                ? "border-indigo-300 bg-indigo-50 text-indigo-900 hover:bg-indigo-100"
                : "border-slate-300 bg-white text-slate-500 hover:bg-slate-50"
            }`}
          >
            {selectedOrg ? (
              <MapPin className="h-3.5 w-3.5 shrink-0 text-indigo-500" />
            ) : (
              <MapPin className="h-3.5 w-3.5 shrink-0 text-slate-400" />
            )}
            <span className="truncate flex-1 font-medium text-xs">
              {displayName}
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
              <div className="absolute left-0 right-0 top-full mt-1 bg-white border border-slate-200 rounded-lg shadow-lg z-20 max-h-72 overflow-auto">
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
                              me.currentContext?.selectedOrganizationId ===
                              org.id;
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
                                    isSelected
                                      ? "border-indigo-500"
                                      : "border-slate-300"
                                  }`}
                                >
                                  {isSelected && (
                                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                                  )}
                                </span>
                                <span
                                  className={isSelected ? "font-medium" : ""}
                                >
                                  {org.name}
                                  {hasChildren && (
                                    <span className="text-[10px] text-slate-400 font-normal ml-1">
                                      ({org.children.length})
                                    </span>
                                  )}
                                  {(org.city || org.state) && (
                                    <span className="text-slate-400 font-normal">
                                      {" \u2014 "}
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
                          <div className="px-3 py-1 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                            Other locations
                          </div>
                          {flat.map((org) => {
                            const isSelected =
                              me.currentContext?.selectedOrganizationId ===
                              org.id;
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
                                    isSelected
                                      ? "border-indigo-500"
                                      : "border-slate-300"
                                  }`}
                                >
                                  {isSelected && (
                                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-500" />
                                  )}
                                </span>
                                <span
                                  className={isSelected ? "font-medium" : ""}
                                >
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

      {/* ── Navigation ────────────────────────────────────────────────── */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        {/* Main nav */}
        <ul className="space-y-1">
          {mainNavItems.map((item) => {
            const active = isActive(item.href);
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    active
                      ? "bg-indigo-50 text-indigo-900"
                      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                  }`}
                >
                  <item.icon className="h-4 w-4 shrink-0" />
                  <span>{item.label}</span>
                </Link>
              </li>
            );
          })}
        </ul>

        {/* Group Admin section */}
        {me.isGroupAdmin && (
          <>
            <div className="mt-6 mb-2 px-3">
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">
                Group Admin
              </p>
            </div>
            <ul className="space-y-1">
              <li>
                <Link
                  href="/group-admin"
                  className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    isActive("/group-admin")
                      ? "bg-emerald-50 text-emerald-900"
                      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                  }`}
                >
                  <UserCog className="h-4 w-4 shrink-0" />
                  <span>Group Admin</span>
                </Link>
              </li>
            </ul>
          </>
        )}

        {/* Superadmin section */}
        {me.isSuperadmin && (
          <>
            <div className="mt-6 mb-2 px-3">
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">
                Admin
              </p>
            </div>
            <ul className="space-y-1">
              <li>
                <Link
                  href="/admin"
                  className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    isActive("/admin")
                      ? "bg-amber-50 text-amber-900"
                      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                  }`}
                >
                  <ShieldCheck className="h-4 w-4 shrink-0" />
                  <span>Admin Dashboard</span>
                </Link>
              </li>
            </ul>
          </>
        )}

        {/* Orchestrator link */}
        {selectedOrg && (
          <>
            <div className="mt-6 mb-2 px-3">
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">
                Integrations
              </p>
            </div>
            <ul className="space-y-1">
              <li>
                <a
                  href={`${process.env.NEXT_PUBLIC_ORCHESTRATOR_URL || "http://localhost:3000"}/sso?groupId=${selectedOrg.groupId}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-violet-600 hover:bg-violet-50 hover:text-violet-800 transition-colors"
                >
                  <ExternalLink className="h-4 w-4 shrink-0" />
                  <span>Orchestrator</span>
                </a>
              </li>
            </ul>
          </>
        )}
      </nav>

      {/* ── Settings + User section ─────────────────────────────────── */}
      <div className="border-t border-slate-200 p-3">
        <Link
          href="/settings"
          className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
            isActive("/settings")
              ? "bg-indigo-50 text-indigo-900"
              : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          }`}
        >
          <Settings className="h-4 w-4 shrink-0" />
          <span>Settings</span>
        </Link>
      </div>
      <div className="border-t border-slate-200 p-3">
        <Link
          href="/settings/profile"
          className="flex items-center gap-3 rounded-md px-3 py-2 hover:bg-slate-100 transition-colors"
        >
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 text-xs font-semibold">
            {(
              me.user.name?.charAt(0) ||
              me.user.email?.charAt(0) ||
              "U"
            ).toUpperCase()}
          </span>
          <div className="flex-1 min-w-0">
            <p className="truncate text-sm font-medium text-slate-900 leading-none">
              {me.user.name || me.user.email || "User"}
            </p>
            {me.user.name && me.user.email && (
              <p className="truncate text-xs text-slate-500 mt-1">
                {me.user.email}
              </p>
            )}
          </div>
          {roleBadge && (
            <span
              className={`text-[10px] font-medium px-1.5 py-0.5 rounded-full border shrink-0 ${roleBadge.className}`}
            >
              {roleBadge.label}
            </span>
          )}
        </Link>
        <a
          href="/api/auth/logout"
          className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition-colors mt-1"
        >
          <LogOut className="h-4 w-4 shrink-0" />
          <span>Log out</span>
        </a>
      </div>
    </aside>
  );
}
