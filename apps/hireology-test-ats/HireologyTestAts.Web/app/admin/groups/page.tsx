"use client";

import { useEffect, useState, useCallback } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface Group {
  id: string;
  rootOrganizationId?: string | null;
  name: string;
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
}

interface GroupUserListResponse {
  items: GroupUser[];
}

export default function AdminGroupsPage() {
  const [groups, setGroups] = useState<Group[]>([]);
  const [admins, setAdmins] = useState<Record<string, GroupUser[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [newAdminEmail, setNewAdminEmail] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [assigningAdminId, setAssigningAdminId] = useState<string | null>(null);
  const [assignAdminEmail, setAssignAdminEmail] = useState("");

  const loadAdminsForGroups = useCallback(async (groupList: Group[]) => {
    const adminMap: Record<string, GroupUser[]> = {};
    await Promise.all(
      groupList.map(async (g) => {
        try {
          const res = await testAtsApi.get<GroupUserListResponse>(
            `/api/v1/groups/${g.id}/users`
          );
          adminMap[g.id] = res.items.filter((u) => u.isAdmin);
        } catch {
          adminMap[g.id] = [];
        }
      })
    );
    setAdmins(adminMap);
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await testAtsApi.get<Group[]>("/api/v1/groups");
      setGroups(list);
      await loadAdminsForGroups(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load groups");
    } finally {
      setLoading(false);
    }
  }, [loadAdminsForGroups]);

  useEffect(() => {
    load();
  }, [load]);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setError(null);
    try {
      await testAtsApi.post("/api/v1/groups", {
        name: newName.trim(),
        adminEmail: newAdminEmail.trim() || undefined,
      });
      setNewName("");
      setNewAdminEmail("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create");
    }
  };

  const update = async (id: string) => {
    if (!editName.trim()) return;
    setError(null);
    try {
      await testAtsApi.put(`/api/v1/groups/${id}`, { name: editName.trim() });
      setEditingId(null);
      setEditName("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to update");
    }
  };

  const remove = async (id: string) => {
    if (
      !confirm(
        "Delete this group? This will also delete all organizations within it."
      )
    )
      return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/v1/groups/${id}`);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to delete");
    }
  };

  const assignAdmin = async (groupId: string) => {
    if (!assignAdminEmail.trim()) return;
    setError(null);
    try {
      await testAtsApi.post(`/api/v1/groups/${groupId}/users/invite`, {
        email: assignAdminEmail.trim(),
        isAdmin: true,
      });
      setAssigningAdminId(null);
      setAssignAdminEmail("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to assign admin");
    }
  };

  const removeAdmin = async (groupId: string, userId: string) => {
    if (!confirm("Remove this user as group admin?")) return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/v1/groups/${groupId}/users/${userId}`);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to remove admin");
    }
  };

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/admin" className="text-slate-500 hover:text-slate-700">
          &larr; Admin
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Groups</h1>
      </div>

      <p className="text-slate-600 mb-6 text-sm">
        Creating a group automatically creates a root organization with the same
        name. You can then add child organizations under it.
      </p>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      <form onSubmit={create} className="mb-8 flex flex-col gap-3">
        <div className="flex gap-2">
          <input
            type="text"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Group name (e.g. Autonation)"
            className="px-3 py-2 border border-slate-300 rounded-lg flex-1 max-w-sm"
          />
          <input
            type="email"
            value={newAdminEmail}
            onChange={(e) => setNewAdminEmail(e.target.value)}
            placeholder="Admin email (optional)"
            className="px-3 py-2 border border-slate-300 rounded-lg flex-1 max-w-sm"
          />
          <button
            type="submit"
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            Create group
          </button>
        </div>
        <p className="text-xs text-slate-500">
          If an admin email is provided, that user will be set up as the group
          admin and can invite other users.
        </p>
      </form>

      {loading ? (
        <p className="text-slate-500">Loading...</p>
      ) : groups.length === 0 ? (
        <p className="text-slate-500">No groups yet.</p>
      ) : (
        <div className="space-y-4">
          {groups.map((g) => {
            const groupAdmins = admins[g.id] ?? [];
            return (
              <div
                key={g.id}
                className="border border-slate-200 rounded-lg overflow-hidden"
              >
                {/* Group header row */}
                <div className="flex items-center justify-between px-4 py-3 bg-slate-50 border-b border-slate-200">
                  <div className="flex items-center gap-4">
                    {editingId === g.id ? (
                      <div className="flex items-center gap-2">
                        <input
                          type="text"
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          className="px-2 py-1 border border-slate-300 rounded text-sm"
                          autoFocus
                        />
                        <button
                          type="button"
                          onClick={() => update(g.id)}
                          className="text-sm text-indigo-600 hover:underline"
                        >
                          Save
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            setEditingId(null);
                            setEditName("");
                          }}
                          className="text-sm text-slate-500 hover:underline"
                        >
                          Cancel
                        </button>
                      </div>
                    ) : (
                      <span className="font-semibold text-slate-900">
                        {g.name}
                      </span>
                    )}
                    <span className="text-xs text-slate-400 font-mono">
                      {g.rootOrganizationId
                        ? g.rootOrganizationId.slice(0, 8) + "..."
                        : ""}
                    </span>
                  </div>
                  {editingId !== g.id && (
                    <div className="flex items-center gap-3 text-sm">
                      <a
                        href={`/admin/organizations?groupId=${g.id}`}
                        className="text-indigo-600 hover:underline"
                      >
                        View orgs
                      </a>
                      <button
                        type="button"
                        onClick={() => {
                          setEditingId(g.id);
                          setEditName(g.name);
                        }}
                        className="text-indigo-600 hover:underline"
                      >
                        Rename
                      </button>
                      <button
                        type="button"
                        onClick={() => remove(g.id)}
                        className="text-red-600 hover:underline"
                      >
                        Delete
                      </button>
                    </div>
                  )}
                </div>

                {/* Admins section */}
                <div className="px-4 py-3">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">
                      Admins
                    </span>
                  </div>

                  {groupAdmins.length === 0 && assigningAdminId !== g.id ? (
                    <div className="flex items-center gap-3">
                      <span className="text-sm text-slate-400 italic">
                        No admins assigned
                      </span>
                      <button
                        type="button"
                        onClick={() => {
                          setAssigningAdminId(g.id);
                          setAssignAdminEmail("");
                        }}
                        className="text-xs text-emerald-600 hover:underline font-medium"
                      >
                        + Add admin
                      </button>
                    </div>
                  ) : (
                    <>
                      <ul className="space-y-1">
                        {groupAdmins.map((gu) => (
                          <li
                            key={gu.user.id}
                            className="flex items-center justify-between py-1 group"
                          >
                            <div className="flex items-center gap-2">
                              <span className="inline-block w-6 h-6 rounded-full bg-emerald-100 text-emerald-700 text-xs font-bold flex items-center justify-center">
                                {(
                                  gu.user.name?.[0] ||
                                  gu.user.email?.[0] ||
                                  "?"
                                ).toUpperCase()}
                              </span>
                              <span className="text-sm text-slate-800">
                                {gu.user.name || gu.user.email || "—"}
                              </span>
                              {gu.user.email &&
                                gu.user.email !== gu.user.name && (
                                  <span className="text-xs text-slate-400">
                                    {gu.user.email}
                                  </span>
                                )}
                              {!gu.user.auth0Sub && (
                                <span className="text-xs text-amber-500 italic">
                                  (pending)
                                </span>
                              )}
                            </div>
                            <button
                              type="button"
                              onClick={() => removeAdmin(g.id, gu.user.id)}
                              className="text-xs text-red-500 hover:underline opacity-0 group-hover:opacity-100 transition-opacity"
                            >
                              Remove
                            </button>
                          </li>
                        ))}
                      </ul>

                      {/* Add another admin */}
                      {assigningAdminId === g.id ? (
                        <div className="flex items-center gap-2 mt-2">
                          <input
                            type="email"
                            value={assignAdminEmail}
                            onChange={(e) =>
                              setAssignAdminEmail(e.target.value)
                            }
                            placeholder="admin@example.com"
                            className="px-2 py-1 border border-slate-300 rounded text-sm w-56"
                            autoFocus
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                e.preventDefault();
                                assignAdmin(g.id);
                              }
                              if (e.key === "Escape") {
                                setAssigningAdminId(null);
                                setAssignAdminEmail("");
                              }
                            }}
                          />
                          <button
                            type="button"
                            onClick={() => assignAdmin(g.id)}
                            className="text-xs text-indigo-600 hover:underline font-medium"
                          >
                            Add
                          </button>
                          <button
                            type="button"
                            onClick={() => {
                              setAssigningAdminId(null);
                              setAssignAdminEmail("");
                            }}
                            className="text-xs text-slate-500 hover:underline"
                          >
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <button
                          type="button"
                          onClick={() => {
                            setAssigningAdminId(g.id);
                            setAssignAdminEmail("");
                          }}
                          className="text-xs text-emerald-600 hover:underline font-medium mt-2"
                        >
                          + Add admin
                        </button>
                      )}
                    </>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
