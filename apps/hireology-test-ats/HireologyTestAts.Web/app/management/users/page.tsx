"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface User {
  id: string;
  auth0Sub: string;
  email?: string | null;
  name?: string | null;
}

interface UserWithAccess {
  user: User;
  groupIds: string[];
  organizationIds: string[];
}

interface Group {
  id: string;
  name: string;
}

interface Organization {
  id: string;
  groupId: string;
  name: string;
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [groups, setGroups] = useState<Group[]>([]);
  const [organizations, setOrganizations] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editGroupIds, setEditGroupIds] = useState<string[]>([]);
  const [editOrganizationIds, setEditOrganizationIds] = useState<string[]>([]);

  const pageSize = 20;

  const loadUsers = async () => {
    const res = await testAtsApi.get<{
      items: User[];
      totalCount: number;
      pageNumber: number;
      pageSize: number;
    }>(`/api/v1/users?pageNumber=${page}&pageSize=${pageSize}`);
    setUsers(res.items);
    setTotalCount(res.totalCount);
  };

  const loadGroupsAndOrgs = async () => {
    const [gList, oList] = await Promise.all([
      testAtsApi.get<Group[]>("/api/v1/groups"),
      testAtsApi.get<Organization[]>("/api/v1/organizations"),
    ]);
    setGroups(gList);
    setOrganizations(oList);
  };

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      await loadGroupsAndOrgs();
      await loadUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [page]);

  const openEdit = async (id: string) => {
    try {
      const data = await testAtsApi.get<UserWithAccess>(`/api/v1/users/${id}`);
      setEditingId(id);
      setEditGroupIds(data.groupIds);
      setEditOrganizationIds(data.organizationIds);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load user");
    }
  };

  const saveAccess = async () => {
    if (!editingId) return;
    setError(null);
    try {
      await testAtsApi.put(`/api/v1/users/${editingId}/access`, {
        groupIds: editGroupIds,
        organizationIds: editOrganizationIds,
      });
      setEditingId(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to save");
    }
  };

  const toggleGroup = (groupId: string) => {
    setEditGroupIds((prev) =>
      prev.includes(groupId) ? prev.filter((id) => id !== groupId) : [...prev, groupId]
    );
  };

  const toggleOrganization = (orgId: string) => {
    setEditOrganizationIds((prev) =>
      prev.includes(orgId) ? prev.filter((id) => id !== orgId) : [...prev, orgId]
    );
  };

  const groupName = (id: string) => groups.find((g) => g.id === id)?.name ?? id;
  const orgName = (id: string) => organizations.find((o) => o.id === id)?.name ?? id;

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/management" className="text-slate-500 hover:text-slate-700">
          ← Management
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Users</h1>
      </div>

      <p className="text-slate-600 mb-4">
        Users are created when they first log in. Assign groups (access to all locations in that
        group) or specific locations.
      </p>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      {editingId && (
        <div className="mb-6 p-4 border border-slate-200 rounded-lg bg-slate-50">
          <h2 className="font-medium text-slate-900 mb-2">Edit access</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <div className="text-sm font-medium text-slate-700 mb-2">Groups (full access to all locations in group)</div>
              <ul className="space-y-1">
                {groups.map((g) => (
                  <li key={g.id}>
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={editGroupIds.includes(g.id)}
                        onChange={() => toggleGroup(g.id)}
                      />
                      {g.name}
                    </label>
                  </li>
                ))}
              </ul>
            </div>
            <div>
              <div className="text-sm font-medium text-slate-700 mb-2">Locations (access to this location only)</div>
              <ul className="space-y-1 max-h-48 overflow-auto">
                {organizations.map((o) => (
                  <li key={o.id}>
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={editOrganizationIds.includes(o.id)}
                        onChange={() => toggleOrganization(o.id)}
                      />
                      {o.name} ({groupName(o.groupId)})
                    </label>
                  </li>
                ))}
              </ul>
            </div>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={saveAccess}
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
            >
              Save access
            </button>
            <button
              type="button"
              onClick={() => setEditingId(null)}
              className="px-4 py-2 border border-slate-300 rounded-lg hover:bg-slate-100"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-slate-500">Loading…</p>
      ) : (
        <>
          <ul className="space-y-2">
            {users.map((u) => (
              <li
                key={u.id}
                className="flex items-center justify-between py-2 border-b border-slate-100"
              >
                <div>
                  <span className="font-medium">{u.name || u.email || u.auth0Sub}</span>
                  {u.email && u.email !== u.name && (
                    <span className="text-slate-500 text-sm ml-2">{u.email}</span>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => openEdit(u.id)}
                  className="text-sm text-indigo-600 hover:underline"
                >
                  Edit access
                </button>
              </li>
            ))}
          </ul>
          {totalCount > pageSize && (
            <div className="mt-4 flex gap-2 items-center">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1 border border-slate-300 rounded disabled:opacity-50"
              >
                Previous
              </button>
              <span className="text-slate-600 text-sm">
                Page {page} of {Math.ceil(totalCount / pageSize)}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => p + 1)}
                disabled={page * pageSize >= totalCount}
                className="px-3 py-1 border border-slate-300 rounded disabled:opacity-50"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
