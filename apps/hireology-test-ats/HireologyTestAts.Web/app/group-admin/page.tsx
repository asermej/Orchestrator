"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";

interface GroupItem {
  id: string;
  name: string;
  rootOrganizationId?: string | null;
}

interface MeResponse {
  user: { id: string; name?: string | null; email?: string | null };
  isSuperadmin: boolean;
  isGroupAdmin: boolean;
  adminGroupIds: string[];
  accessibleGroups: GroupItem[];
}

export default function GroupAdminPage() {
  const router = useRouter();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [groups, setGroups] = useState<GroupItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const meData = await testAtsApi.get<MeResponse>("/api/v1/me");
        setMe(meData);

        if (!meData.isGroupAdmin && !meData.isSuperadmin) {
          setError("Access denied. You are not a group admin.");
          return;
        }

        // Superadmins can list all groups; group admins fetch only their own
        if (meData.isSuperadmin) {
          const allGroups = await testAtsApi.get<GroupItem[]>("/api/v1/groups");
          setGroups(allGroups);
        } else {
          const groupPromises = meData.adminGroupIds.map((id) =>
            testAtsApi.get<GroupItem>(`/api/v1/groups/${id}`)
          );
          const adminGroups = await Promise.all(groupPromises);

          // If the group admin has exactly one group, go straight to it
          if (adminGroups.length === 1) {
            router.replace(`/group-admin/${adminGroups[0].id}`);
            return;
          }

          setGroups(adminGroups);
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [router]);

  if (loading) {
    return (
      <div>
        <h1 className="text-2xl font-bold text-slate-900 mb-4">Group Admin</h1>
        <p className="text-slate-500">Loading...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <h1 className="text-2xl font-bold text-slate-900 mb-4">Group Admin</h1>
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900 mb-2">Group Admin</h1>
      <p className="text-slate-600 mb-6">
        Manage users and access for your groups. Invite new users by email and
        configure their organization access.
      </p>

      {groups.length === 0 ? (
        <p className="text-slate-500">
          No groups found. Contact a superadmin to be assigned as a group admin.
        </p>
      ) : (
        <div className="space-y-3">
          {groups.map((group) => (
            <a
              key={group.id}
              href={`/group-admin/${group.id}`}
              className="block p-4 border border-slate-200 rounded-lg hover:border-indigo-300 hover:bg-indigo-50/50 transition-colors"
            >
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="font-semibold text-slate-900">{group.name}</h2>
                  <p className="text-sm text-slate-500 mt-1">
                    Manage users, invite new members, and configure access
                  </p>
                </div>
                <span className="text-indigo-600 text-sm font-medium">
                  Manage &rarr;
                </span>
              </div>
            </a>
          ))}
        </div>
      )}
    </div>
  );
}
