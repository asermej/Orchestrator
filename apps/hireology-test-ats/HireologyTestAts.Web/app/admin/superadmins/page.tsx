"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface Superadmin {
  id: string;
  auth0Sub: string;
  email?: string | null;
  name?: string | null;
  isSuperadmin: boolean;
}

interface User {
  id: string;
  auth0Sub: string;
  email?: string | null;
  name?: string | null;
}

interface UserListResponse {
  items: User[];
  totalCount: number;
}

export default function SuperadminsPage() {
  const [superadmins, setSuperadmins] = useState<Superadmin[]>([]);
  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [showUserPicker, setShowUserPicker] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [admins, usersResp] = await Promise.all([
        testAtsApi.get<Superadmin[]>("/api/v1/admin/superadmins"),
        testAtsApi.get<UserListResponse>("/api/v1/users?pageSize=200"),
      ]);
      setSuperadmins(admins);
      setAllUsers(usersResp.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const promote = async () => {
    if (!selectedUserId) return;
    setError(null);
    try {
      await testAtsApi.post("/api/v1/admin/superadmins", {
        userId: selectedUserId,
      });
      setSelectedUserId("");
      setShowUserPicker(false);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to promote");
    }
  };

  const demote = async (userId: string) => {
    if (!confirm("Remove superadmin privileges from this user?")) return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/v1/admin/superadmins/${userId}`);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to demote");
    }
  };

  const nonSuperadminUsers = allUsers.filter(
    (u) => !superadmins.some((s) => s.id === u.id)
  );

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/admin" className="text-slate-500 hover:text-slate-700">
          ← Admin
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Superadmins</h1>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      <div className="mb-8">
        {showUserPicker ? (
          <div className="flex gap-2 items-end">
            <div className="flex-1 max-w-sm">
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Select a user to promote
              </label>
              <select
                value={selectedUserId}
                onChange={(e) => setSelectedUserId(e.target.value)}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg"
              >
                <option value="">-- Select user --</option>
                {nonSuperadminUsers.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name || u.email || u.auth0Sub}
                  </option>
                ))}
              </select>
            </div>
            <button
              type="button"
              onClick={promote}
              disabled={!selectedUserId}
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
            >
              Promote
            </button>
            <button
              type="button"
              onClick={() => {
                setShowUserPicker(false);
                setSelectedUserId("");
              }}
              className="px-4 py-2 text-slate-500 hover:text-slate-700"
            >
              Cancel
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setShowUserPicker(true)}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            Add superadmin
          </button>
        )}
      </div>

      {loading ? (
        <p className="text-slate-500">Loading...</p>
      ) : superadmins.length === 0 ? (
        <p className="text-slate-500">No superadmins found.</p>
      ) : (
        <div className="border border-slate-200 rounded-lg overflow-hidden">
          <table className="w-full text-left">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  Name
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600">
                  Email
                </th>
                <th className="px-4 py-3 text-sm font-medium text-slate-600 text-right">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {superadmins.map((sa) => (
                <tr key={sa.id}>
                  <td className="px-4 py-3 text-sm">
                    {sa.name || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm text-slate-600">
                    {sa.email || "—"}
                  </td>
                  <td className="px-4 py-3 text-sm text-right">
                    <button
                      type="button"
                      onClick={() => demote(sa.id)}
                      className="text-red-600 hover:underline"
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
    </div>
  );
}
