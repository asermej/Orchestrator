"use client";

import { useEffect, useState, useCallback } from "react";
import { useParams } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";

/* ------------------------------------------------------------------ */
/* Types                                                               */
/* ------------------------------------------------------------------ */

interface GroupItem {
  id: string;
  name: string;
  rootOrganizationId?: string | null;
}

interface Organization {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

interface OrgAccessEntry {
  organizationId: string;
  includeChildren: boolean;
}

interface UserResource {
  id: string;
  auth0Sub: string;
  email?: string | null;
  name?: string | null;
}

interface GroupUser {
  user: UserResource;
  isAdmin: boolean;
  organizationAccess: OrgAccessEntry[];
}

interface GroupUserListResponse {
  items: GroupUser[];
}

/* ------------------------------------------------------------------ */
/* Organization tree helpers                                           */
/* ------------------------------------------------------------------ */

interface OrgTreeNode {
  org: Organization;
  children: OrgTreeNode[];
  depth: number;
}

function buildTree(orgs: Organization[]): OrgTreeNode[] {
  const map = new Map<string, OrgTreeNode>();
  const roots: OrgTreeNode[] = [];

  for (const org of orgs) {
    map.set(org.id, { org, children: [], depth: 0 });
  }

  for (const org of orgs) {
    const node = map.get(org.id)!;
    if (org.parentOrganizationId && map.has(org.parentOrganizationId)) {
      const parent = map.get(org.parentOrganizationId)!;
      parent.children.push(node);
      node.depth = parent.depth + 1;
    } else {
      roots.push(node);
    }
  }

  // Fix depths with BFS
  const queue = [...roots];
  while (queue.length) {
    const cur = queue.shift()!;
    for (const ch of cur.children) {
      ch.depth = cur.depth + 1;
      queue.push(ch);
    }
  }

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

/* ------------------------------------------------------------------ */
/* Component                                                           */
/* ------------------------------------------------------------------ */

export default function GroupDetailPage() {
  const params = useParams();
  const groupId = params.groupId as string;

  const [group, setGroup] = useState<GroupItem | null>(null);
  const [users, setUsers] = useState<GroupUser[]>([]);
  const [organizations, setOrganizations] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Invite form
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteIsAdmin, setInviteIsAdmin] = useState(false);
  const [inviteOrgAccess, setInviteOrgAccess] = useState<Map<string, boolean>>(
    new Map()
  ); // orgId → includeChildren
  const [inviteSubmitting, setInviteSubmitting] = useState(false);

  // Edit access
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editOrgAccess, setEditOrgAccess] = useState<Map<string, boolean>>(
    new Map()
  );

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [groupData, usersData, orgsData] = await Promise.all([
        testAtsApi.get<GroupItem>(`/api/v1/groups/${groupId}`),
        testAtsApi.get<GroupUserListResponse>(
          `/api/v1/groups/${groupId}/users`
        ),
        testAtsApi.get<Organization[]>(
          `/api/v1/organizations?groupId=${groupId}`
        ),
      ]);
      setGroup(groupData);
      setUsers(usersData.items);
      setOrganizations(orgsData);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  }, [groupId]);

  useEffect(() => {
    load();
  }, [load]);

  /* ---- Invite ---- */

  const toggleInviteOrg = (orgId: string) => {
    setInviteOrgAccess((prev) => {
      const next = new Map(prev);
      if (next.has(orgId)) {
        next.delete(orgId);
      } else {
        next.set(orgId, false);
      }
      return next;
    });
  };

  const toggleInviteIncludeChildren = (orgId: string) => {
    setInviteOrgAccess((prev) => {
      const next = new Map(prev);
      if (next.has(orgId)) {
        next.set(orgId, !next.get(orgId));
      }
      return next;
    });
  };

  const submitInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteEmail.trim()) return;
    setError(null);
    setInviteSubmitting(true);
    try {
      const orgAccess: OrgAccessEntry[] = [];
      inviteOrgAccess.forEach((includeChildren, orgId) => {
        orgAccess.push({ organizationId: orgId, includeChildren });
      });

      await testAtsApi.post(`/api/v1/groups/${groupId}/users/invite`, {
        email: inviteEmail.trim(),
        isAdmin: inviteIsAdmin,
        organizationAccess: orgAccess.length > 0 ? orgAccess : undefined,
      });
      setInviteEmail("");
      setInviteIsAdmin(false);
      setInviteOrgAccess(new Map());
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to invite user");
    } finally {
      setInviteSubmitting(false);
    }
  };

  /* ---- Edit access ---- */

  const openEditAccess = (gu: GroupUser) => {
    setEditingUserId(gu.user.id);
    const map = new Map<string, boolean>();
    for (const entry of gu.organizationAccess) {
      map.set(entry.organizationId, entry.includeChildren);
    }
    setEditOrgAccess(map);
  };

  const toggleEditOrg = (orgId: string) => {
    setEditOrgAccess((prev) => {
      const next = new Map(prev);
      if (next.has(orgId)) {
        next.delete(orgId);
      } else {
        next.set(orgId, false);
      }
      return next;
    });
  };

  const toggleEditIncludeChildren = (orgId: string) => {
    setEditOrgAccess((prev) => {
      const next = new Map(prev);
      if (next.has(orgId)) {
        next.set(orgId, !next.get(orgId));
      }
      return next;
    });
  };

  const saveAccess = async () => {
    if (!editingUserId) return;
    setError(null);
    try {
      const entries: OrgAccessEntry[] = [];
      editOrgAccess.forEach((includeChildren, orgId) => {
        entries.push({ organizationId: orgId, includeChildren });
      });
      await testAtsApi.put(
        `/api/v1/groups/${groupId}/users/${editingUserId}/access`,
        { organizationAccess: entries }
      );
      setEditingUserId(null);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to save access");
    }
  };

  /* ---- Remove ---- */

  const removeUser = async (userId: string) => {
    if (!confirm("Remove this user from the group?")) return;
    setError(null);
    try {
      await testAtsApi.delete(
        `/api/v1/groups/${groupId}/users/${userId}`
      );
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to remove user");
    }
  };

  /* ---- Tree render ---- */

  const tree = buildTree(organizations);
  const flatOrgs = flattenTree(tree);

  const renderOrgPicker = (
    accessMap: Map<string, boolean>,
    toggleOrg: (id: string) => void,
    toggleChildren: (id: string) => void
  ) => (
    <div className="max-h-64 overflow-auto border border-slate-200 rounded-lg p-2">
      {flatOrgs.length === 0 ? (
        <p className="text-slate-500 text-sm p-2">
          No organizations in this group.
        </p>
      ) : (
        <ul className="space-y-1">
          {flatOrgs.map((node) => {
            const isSelected = accessMap.has(node.org.id);
            const includeChildren = accessMap.get(node.org.id) ?? false;
            const hasChildren = node.children.length > 0;
            return (
              <li
                key={node.org.id}
                style={{ paddingLeft: `${node.depth * 20}px` }}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => toggleOrg(node.org.id)}
                    className="rounded"
                  />
                  <span className="text-sm">{node.org.name}</span>
                  {isSelected && hasChildren && (
                    <label className="flex items-center gap-1 ml-2 text-xs text-indigo-600 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={includeChildren}
                        onChange={() => toggleChildren(node.org.id)}
                        className="rounded"
                      />
                      <span>+ all children</span>
                    </label>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );

  /* ---- Render ---- */

  if (loading) {
    return (
      <div>
        <h1 className="text-2xl font-bold text-slate-900 mb-4">
          Group Details
        </h1>
        <p className="text-slate-500">Loading...</p>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {group?.name ?? "Group"}
        </h1>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      {/* Invite form */}
      <div className="mb-8 p-4 border border-slate-200 rounded-lg bg-slate-50">
        <h2 className="font-semibold text-slate-900 mb-3">
          Invite a user by email
        </h2>
        <p className="text-sm text-slate-600 mb-4">
          The user will be pre-provisioned. When they log in for the first time,
          they will automatically have the access you configure here.
        </p>
        <form onSubmit={submitInvite} className="space-y-4">
          <div className="flex gap-3 items-end">
            <div className="flex-1 max-w-sm">
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Email address
              </label>
              <input
                type="email"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                placeholder="user@example.com"
                className="w-full px-3 py-2 border border-slate-300 rounded-lg"
                required
              />
            </div>
            <label className="flex items-center gap-2 cursor-pointer pb-2">
              <input
                type="checkbox"
                checked={inviteIsAdmin}
                onChange={(e) => setInviteIsAdmin(e.target.checked)}
                className="rounded"
              />
              <span className="text-sm font-medium text-slate-700">
                Group admin
              </span>
            </label>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">
              Organization access (optional &mdash; select specific locations)
            </label>
            {renderOrgPicker(
              inviteOrgAccess,
              toggleInviteOrg,
              toggleInviteIncludeChildren
            )}
          </div>

          <button
            type="submit"
            disabled={inviteSubmitting || !inviteEmail.trim()}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {inviteSubmitting ? "Inviting..." : "Invite user"}
          </button>
        </form>
      </div>

      {/* Users list */}
      <h2 className="font-semibold text-slate-900 mb-3">
        Users in this group ({users.length})
      </h2>

      {users.length === 0 ? (
        <p className="text-slate-500 text-sm">
          No users in this group yet. Use the invite form above to add users.
        </p>
      ) : (
        <div className="border border-slate-200 rounded-lg overflow-hidden">
          <table className="w-full text-left">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  User
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  Role
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  Status
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  Org Access
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600 text-right">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {users.map((gu) => (
                <tr key={gu.user.id}>
                  <td className="px-4 py-3 text-sm">
                    <div className="font-medium">
                      {gu.user.name || gu.user.email || "—"}
                    </div>
                    {gu.user.email && gu.user.email !== gu.user.name && (
                      <div className="text-slate-500 text-xs">
                        {gu.user.email}
                      </div>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {gu.isAdmin ? (
                      <span className="inline-block px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 text-xs font-medium">
                        Admin
                      </span>
                    ) : (
                      <span className="text-slate-500 text-xs">Member</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {gu.user.auth0Sub ? (
                      <span className="text-green-600 text-xs">Active</span>
                    ) : (
                      <span className="text-amber-600 text-xs">
                        Pending (not yet logged in)
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {gu.organizationAccess.length === 0 ? (
                      <span className="text-slate-400 text-xs">
                        Group-level
                      </span>
                    ) : (
                      <span className="text-slate-600 text-xs">
                        {gu.organizationAccess.length} org(s)
                        {gu.organizationAccess.some((e) => e.includeChildren) &&
                          " (+ subtrees)"}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-right space-x-2">
                    <button
                      type="button"
                      onClick={() => openEditAccess(gu)}
                      className="text-indigo-600 hover:underline text-xs"
                    >
                      Edit access
                    </button>
                    <button
                      type="button"
                      onClick={() => removeUser(gu.user.id)}
                      className="text-red-600 hover:underline text-xs"
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit access modal */}
      {editingUserId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-lg max-h-[80vh] overflow-auto">
            <h3 className="font-semibold text-slate-900 mb-3">
              Edit organization access
            </h3>
            <p className="text-sm text-slate-600 mb-4">
              Select which organizations this user can access. Check &quot;+ all
              children&quot; to include all sub-organizations automatically
              (including future ones).
            </p>

            {renderOrgPicker(
              editOrgAccess,
              toggleEditOrg,
              toggleEditIncludeChildren
            )}

            <div className="mt-4 flex gap-2">
              <button
                type="button"
                onClick={saveAccess}
                className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
              >
                Save
              </button>
              <button
                type="button"
                onClick={() => setEditingUserId(null)}
                className="px-4 py-2 border border-slate-300 rounded-lg hover:bg-slate-100"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
