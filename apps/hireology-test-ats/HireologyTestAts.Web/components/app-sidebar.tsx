"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Briefcase,
  Bot,
  Globe,
  Settings,
  ShieldCheck,
  UserCog,
  LogOut,
} from "lucide-react";
import type { MeResponse } from "./app-shell";

// ── Nav items ──────────────────────────────────────────────────────────────

const mainNavItems = [
  { href: "/jobs", label: "Jobs", icon: Briefcase },
  { href: "/careers", label: "Careers", icon: Globe },
];

// ── Component ──────────────────────────────────────────────────────────────

interface AppSidebarProps {
  me: MeResponse;
}

export function AppSidebar({ me }: AppSidebarProps) {
  const pathname = usePathname();
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const roleBadge: { label: string; className: string } | null = (() => {
    const adminGroupIdSet = new Set(me.adminGroupIds ?? []);
    const selectedOrg = me.currentContext?.selectedOrganizationId
      ? me.accessibleOrganizations.find(
          (o) => o.id === me.currentContext.selectedOrganizationId
        )
      : null;
    const isGroupAdminForSelectedOrg =
      selectedOrg != null && adminGroupIdSet.has(selectedOrg.groupId);
    if (me.isSuperadmin) {
      return { label: "Superadmin", className: "bg-amber-100 text-amber-800 border-amber-200" };
    }
    if (isGroupAdminForSelectedOrg) {
      return { label: "Group Admin", className: "bg-emerald-100 text-emerald-800 border-emerald-200" };
    }
    return null;
  })();

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(href + "/");
  };

  const selectedOrg = me.currentContext?.selectedOrganizationId
    ? me.accessibleOrganizations.find(
        (o) => o.id === me.currentContext.selectedOrganizationId
      )
    : null;
  const groupId =
    selectedOrg?.groupId ??
    me.accessibleOrganizations[0]?.groupId ??
    me.accessibleGroups[0]?.id;
  const orchestratorBaseUrl =
    process.env.NEXT_PUBLIC_ORCHESTRATOR_URL || "http://localhost:3000";
  const aiAssistantsHref =
    groupId &&
    `${orchestratorBaseUrl}/sso?groupId=${groupId}&returnUrl=${encodeURIComponent(
      typeof window !== "undefined" ? window.location.origin + pathname : ""
    )}${selectedOrg?.id ? `&organizationId=${encodeURIComponent(selectedOrg.id)}` : ""}`;

  return (
    <aside className="flex w-56 flex-col border-r border-slate-200 bg-white shrink-0">
      {/* Navigation */}
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

        {/* Management section */}
        <div className="mt-6 mb-2 px-3">
          <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">
            Management
          </p>
        </div>
        <ul className="space-y-1">
          {aiAssistantsHref && (
            <li>
              <a
                href={aiAssistantsHref}
                className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900"
              >
                <Bot className="h-4 w-4 shrink-0" />
                <span>AI Assistants</span>
              </a>
            </li>
          )}
        </ul>

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
      </nav>

      {/* Settings */}
      <div className="border-t border-slate-200 px-3 py-2">
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

      {/* User section at bottom */}
      <div className="relative border-t border-slate-200 p-3">
        <button
          type="button"
          onClick={() => setUserMenuOpen(!userMenuOpen)}
          className="flex w-full items-center gap-2.5 rounded-md px-2 py-2 hover:bg-slate-100 transition-colors"
        >
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 text-[10px] font-semibold">
            {(me.user.name?.charAt(0) || me.user.email?.charAt(0) || "U").toUpperCase()}
          </span>
          <div className="flex-1 min-w-0 text-left">
            <div className="text-sm font-medium text-slate-700 truncate">
              {me.user.name || "User"}
            </div>
            {me.user.email && (
              <div className="text-[10px] text-slate-400 truncate">
                {me.user.email}
              </div>
            )}
          </div>
          {roleBadge && (
            <span
              className={`text-[10px] font-medium px-1.5 py-0.5 rounded-full border shrink-0 ${roleBadge.className}`}
            >
              {roleBadge.label}
            </span>
          )}
        </button>

        {userMenuOpen && (
          <>
            <div
              className="fixed inset-0 z-10"
              aria-hidden
              onClick={() => setUserMenuOpen(false)}
            />
            <div className="absolute left-3 right-3 bottom-full mb-1 bg-white border border-slate-200 rounded-lg shadow-lg z-20 overflow-hidden">
              <a
                href="/api/auth/logout"
                className="flex items-center gap-2 px-4 py-2.5 text-sm font-medium text-red-600 bg-red-50 hover:bg-red-100 transition-colors"
              >
                <LogOut className="h-4 w-4" />
                Logout
              </a>
              <div className="border-t border-slate-200" />
              {me.isGroupAdmin && (
                <Link
                  href="/group-admin"
                  onClick={() => setUserMenuOpen(false)}
                  className="flex items-center gap-2 px-4 py-2.5 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
                >
                  <UserCog className="h-4 w-4 shrink-0" />
                  Group Admin
                </Link>
              )}
              <Link
                href="/settings/profile"
                onClick={() => setUserMenuOpen(false)}
                className="flex items-center gap-2 px-4 py-2.5 text-sm text-slate-700 hover:bg-slate-50 transition-colors"
              >
                My Profile
              </Link>
            </div>
          </>
        )}
      </div>
    </aside>
  );
}
