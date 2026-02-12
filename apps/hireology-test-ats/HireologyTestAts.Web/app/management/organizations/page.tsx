"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface Group {
  id: string;
  name: string;
}

interface Organization {
  id: string;
  groupId: string;
  name: string;
  city?: string | null;
  state?: string | null;
}

export default function OrganizationsPage() {
  const [groups, setGroups] = useState<Group[]>([]);
  const [organizations, setOrganizations] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [groupIdFilter, setGroupIdFilter] = useState<string>("");
  const [formGroupId, setFormGroupId] = useState("");
  const [formName, setFormName] = useState("");
  const [formCity, setFormCity] = useState("");
  const [formState, setFormState] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editGroupId, setEditGroupId] = useState("");
  const [editName, setEditName] = useState("");
  const [editCity, setEditCity] = useState("");
  const [editState, setEditState] = useState("");

  const loadGroups = async () => {
    const list = await testAtsApi.get<Group[]>("/api/v1/groups");
    setGroups(list);
    if (list.length && !formGroupId) setFormGroupId(list[0].id);
  };

  const loadOrgs = async () => {
    const url = groupIdFilter
      ? `/api/v1/organizations?groupId=${groupIdFilter}`
      : "/api/v1/organizations";
    const list = await testAtsApi.get<Organization[]>(url);
    setOrganizations(list);
  };

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      await loadGroups();
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [groupIdFilter]);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    const groupId = formGroupId || groups[0]?.id;
    if (!formName.trim() || !groupId) return;
    setError(null);
    try {
      await testAtsApi.post("/api/v1/organizations", {
        groupId: groupId,
        name: formName.trim(),
        city: formCity.trim() || undefined,
        state: formState.trim() || undefined,
      });
      setFormName("");
      setFormCity("");
      setFormState("");
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create");
    }
  };

  const update = async (id: string) => {
    if (!editName.trim()) return;
    setError(null);
    try {
      await testAtsApi.put(`/api/v1/organizations/${id}`, {
        groupId: editGroupId,
        name: editName.trim(),
        city: editCity || undefined,
        state: editState || undefined,
      });
      setEditingId(null);
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to update");
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Delete this location?")) return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/v1/organizations/${id}`);
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to delete");
    }
  };

  const groupName = (id: string) => groups.find((g) => g.id === id)?.name ?? id;

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/management" className="text-slate-500 hover:text-slate-700">
          ← Management
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Locations (Organizations)</h1>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      <div className="mb-4">
        <label className="block text-sm font-medium text-slate-700 mb-1">
          Filter by group
        </label>
        <select
          value={groupIdFilter}
          onChange={(e) => setGroupIdFilter(e.target.value)}
          className="px-3 py-2 border border-slate-300 rounded-lg"
        >
          <option value="">All groups</option>
          {groups.map((g) => (
            <option key={g.id} value={g.id}>
              {g.name}
            </option>
          ))}
        </select>
      </div>

      {groups.length === 0 ? (
        <p className="mb-6 text-slate-600">
          Create a group first under <a href="/management/groups" className="text-indigo-600 hover:underline">Groups</a> before adding locations.
        </p>
      ) : (
      <form onSubmit={create} className="mb-8 p-4 border border-slate-200 rounded-lg space-y-2">
        <h2 className="font-medium text-slate-900">Add location</h2>
        <div className="flex flex-wrap gap-2 items-end">
          <div>
            <label className="block text-xs text-slate-500">Group</label>
            <select
              value={formGroupId || groups[0]?.id}
              onChange={(e) => setFormGroupId(e.target.value)}
              className="px-3 py-2 border border-slate-300 rounded-lg"
              required
            >
              {groups.map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-slate-500">Name</label>
            <input
              type="text"
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              placeholder="Location name"
              className="px-3 py-2 border border-slate-300 rounded-lg"
              required
            />
          </div>
          <div>
            <label className="block text-xs text-slate-500">City</label>
            <input
              type="text"
              value={formCity}
              onChange={(e) => setFormCity(e.target.value)}
              placeholder="City"
              className="px-3 py-2 border border-slate-300 rounded-lg"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-500">State</label>
            <input
              type="text"
              value={formState}
              onChange={(e) => setFormState(e.target.value)}
              placeholder="State"
              className="px-3 py-2 border border-slate-300 rounded-lg"
            />
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            Add location
          </button>
        </div>
      </form>
      )}

      {loading ? (
        <p className="text-slate-500">Loading…</p>
      ) : (
        <ul className="space-y-2">
          {organizations.map((org) => (
            <li
              key={org.id}
              className="flex items-center gap-4 py-2 border-b border-slate-100"
            >
              {editingId === org.id ? (
                <>
                  <select
                    value={editGroupId}
                    onChange={(e) => setEditGroupId(e.target.value)}
                    className="px-3 py-2 border border-slate-300 rounded-lg"
                  >
                    {groups.map((g) => (
                      <option key={g.id} value={g.id}>
                        {g.name}
                      </option>
                    ))}
                  </select>
                  <input
                    type="text"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    className="px-3 py-2 border border-slate-300 rounded-lg flex-1 max-w-xs"
                  />
                  <input
                    type="text"
                    value={editCity}
                    onChange={(e) => setEditCity(e.target.value)}
                    placeholder="City"
                    className="px-3 py-2 border border-slate-300 rounded-lg w-32"
                  />
                  <input
                    type="text"
                    value={editState}
                    onChange={(e) => setEditState(e.target.value)}
                    placeholder="State"
                    className="px-3 py-2 border border-slate-300 rounded-lg w-24"
                  />
                  <button
                    type="button"
                    onClick={() => update(org.id)}
                    className="text-indigo-600 hover:underline"
                  >
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditingId(null)}
                    className="text-slate-500 hover:underline"
                  >
                    Cancel
                  </button>
                </>
              ) : (
                <>
                  <span className="text-slate-500 text-sm w-40">{groupName(org.groupId)}</span>
                  <span className="font-medium">{org.name}</span>
                  {(org.city || org.state) && (
                    <span className="text-slate-500 text-sm">
                      {[org.city, org.state].filter(Boolean).join(", ")}
                    </span>
                  )}
                  <button
                    type="button"
                    onClick={() => {
                      setEditingId(org.id);
                      setEditGroupId(org.groupId);
                      setEditName(org.name);
                      setEditCity(org.city ?? "");
                      setEditState(org.state ?? "");
                    }}
                    className="text-sm text-indigo-600 hover:underline"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => remove(org.id)}
                    className="text-sm text-red-600 hover:underline"
                  >
                    Delete
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
