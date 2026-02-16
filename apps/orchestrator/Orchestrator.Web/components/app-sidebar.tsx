"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  Bot,
  BookOpen,
  Settings2,
  Mic,
  Briefcase,
  LogOut,
  Home,
  ShieldAlert,
} from "lucide-react";
import type { UserProfile } from "@auth0/nextjs-auth0/client";

interface GroupInfo {
  id: string;
  name: string;
}

interface AppSidebarProps {
  user?: UserProfile | null;
  groupInfo?: GroupInfo | null;
}

const navItems = [
  { href: "/", label: "Home", icon: Home },
  { href: "/my-agents", label: "My Agents", icon: Bot },
  { href: "/interview-guides", label: "Interview Guides", icon: BookOpen },
  { href: "/interview-configurations", label: "Interview Configs", icon: Settings2 },
  { href: "/interviews", label: "Interviews", icon: Mic },
  { href: "/jobs", label: "Jobs", icon: Briefcase },
];

export function AppSidebar({ user, groupInfo }: AppSidebarProps) {
  const pathname = usePathname();

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(href + "/");
  };

  return (
    <aside className="flex h-screen w-60 flex-col border-r bg-sidebar text-sidebar-foreground sticky top-0">
      {/* Branding */}
      <div className="flex h-14 items-center border-b px-5">
        <Link href="/" className="text-lg font-semibold tracking-tight hover:opacity-80 transition-opacity">
          Orchestrator
        </Link>
      </div>

      {/* Group context */}
      {groupInfo ? (
        <div className="border-b px-5 py-3">
          <p className="text-[10px] font-medium uppercase tracking-wider text-sidebar-foreground/50">
            Group
          </p>
          <p className="text-sm font-medium truncate mt-0.5" title={groupInfo.name}>
            {groupInfo.name}
          </p>
        </div>
      ) : (
        <div className="border-b px-5 py-3">
          <p className="text-xs text-sidebar-foreground/50">
            No group selected
          </p>
        </div>
      )}

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
        {groupInfo && (
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

      {/* User section */}
      {user && (
        <div className="border-t p-3">
          <div className="flex items-center gap-3 rounded-md px-3 py-2">
            <Avatar className="h-8 w-8 shrink-0">
              <AvatarImage src={user.picture || ""} alt={user.name || ""} />
              <AvatarFallback className="text-xs">
                {(user.name?.charAt(0) || user.email?.charAt(0) || "U").toUpperCase()}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1 min-w-0">
              <p className="truncate text-sm font-medium leading-none">
                {user.name || user.email || "User"}
              </p>
              <p className="truncate text-xs text-sidebar-foreground/60 mt-1">
                {user.email || ""}
              </p>
            </div>
          </div>
          <a
            href="/api/auth/logout"
            className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-sidebar-foreground/70 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground transition-colors mt-1"
          >
            <LogOut className="h-4 w-4 shrink-0" />
            <span>Log out</span>
          </a>
        </div>
      )}
    </aside>
  );
}
