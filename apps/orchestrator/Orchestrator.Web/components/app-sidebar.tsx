"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import {
  Bot,
  BookOpen,
  Settings2,
  Mic,
  Briefcase,
  ShieldAlert,
  LogOut,
} from "lucide-react";

interface UserInfo {
  name?: string | null;
  email?: string | null;
  picture?: string | null;
}

interface AppSidebarProps {
  hasGroup?: boolean;
  user?: UserInfo | null;
  displayName?: string | null;
}

const navItems = [
  { href: "/my-agents", label: "My Agents", icon: Bot },
  { href: "/interview-guides", label: "Interview Guides", icon: BookOpen },
  { href: "/interview-configurations", label: "Interview Configs", icon: Settings2 },
  { href: "/interviews", label: "Interviews", icon: Mic },
  { href: "/jobs", label: "Jobs", icon: Briefcase },
];

export function AppSidebar({ hasGroup, user, displayName }: AppSidebarProps) {
  const pathname = usePathname();
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const isActive = (href: string) =>
    pathname === href || pathname.startsWith(href + "/");

  return (
    <aside className="flex w-56 flex-col border-r bg-sidebar text-sidebar-foreground shrink-0">
      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <ul className="space-y-1">
          {navItems.map((item) => {
            const active = isActive(item.href);
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    active
                      ? "bg-sidebar-accent text-sidebar-accent-foreground"
                      : "text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground"
                  )}
                >
                  <item.icon className="h-4 w-4 shrink-0" />
                  <span>{item.label}</span>
                </Link>
              </li>
            );
          })}
        </ul>

        {/* Admin section */}
        {hasGroup && (
          <>
            <div className="mt-6 mb-2 px-3">
              <p className="text-[10px] font-medium uppercase tracking-wider text-sidebar-foreground/40">
                Admin
              </p>
            </div>
            <ul className="space-y-1">
              <li>
                <Link
                  href="/admin/orphaned-entities"
                  className={cn(
                    "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive("/admin/orphaned-entities")
                      ? "bg-sidebar-accent text-sidebar-accent-foreground"
                      : "text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground"
                  )}
                >
                  <ShieldAlert className="h-4 w-4 shrink-0" />
                  <span>Orphaned Entities</span>
                </Link>
              </li>
            </ul>
          </>
        )}
      </nav>

      {/* User section at bottom — matches hireology-test-ats look and feel */}
      {user && (
        <div className="relative border-t border-slate-200 p-3">
          <button
            type="button"
            onClick={() => setUserMenuOpen(!userMenuOpen)}
            className="flex w-full items-center gap-2.5 rounded-md px-2 py-2 hover:bg-slate-100 transition-colors"
          >
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 text-[10px] font-semibold">
              {(displayName?.charAt(0) || user.name?.charAt(0) || user.email?.charAt(0) || "U").toUpperCase()}
            </span>
            <div className="flex-1 min-w-0 text-left">
              <div className="text-sm font-medium text-slate-700 truncate">
                {displayName || user.name || "User"}
              </div>
              {user.email && (
                <div className="text-[10px] text-slate-400 truncate">
                  {user.email}
                </div>
              )}
            </div>
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
              </div>
            </>
          )}
        </div>
      )}
    </aside>
  );
}
