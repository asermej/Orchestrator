"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface Group {
  id: string;
  name: string;
}

export default function GroupsPage() {
  const [groups, setGroups] = useState<Group[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await testAtsApi.get<Group[]>("/api/groups");
      setGroups(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load groups");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setError(null);
    try {
      await testAtsApi.post("/api/groups", { name: newName.trim() });
      setNewName("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create");
    }
  };

  const update = async (id: string) => {
    if (!editName.trim()) return;
    setError(null);
    try {
      await testAtsApi.put(`/api/groups/${id}`, { name: editName.trim() });
      setEditingId(null);
      setEditName("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to update");
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Delete this group?")) return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/groups/${id}`);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to delete");
    }
  };

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/management" className="text-slate-500 hover:text-slate-700">
          ← Management
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Groups</h1>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      <form onSubmit={create} className="mb-8 flex gap-2">
        <input
          type="text"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="Group name"
          className="px-3 py-2 border border-slate-300 rounded-lg flex-1 max-w-xs"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
        >
          Add group
        </button>
      </form>

      {loading ? (
        <p className="text-slate-500">Loading…</p>
      ) : (
        <ul className="space-y-2">
          {groups.map((g) => (
            <li
              key={g.id}
              className="flex items-center gap-4 py-2 border-b border-slate-100"
            >
              {editingId === g.id ? (
                <>
                  <input
                    type="text"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    className="px-3 py-2 border border-slate-300 rounded-lg flex-1 max-w-xs"
                    autoFocus
                  />
                  <button
                    type="button"
                    onClick={() => update(g.id)}
                    className="px-3 py-1 text-indigo-600 hover:underline"
                  >
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setEditingId(null);
                      setEditName("");
                    }}
                    className="px-3 py-1 text-slate-500 hover:underline"
                  >
                    Cancel
                  </button>
                </>
              ) : (
                <>
                  <span className="font-medium">{g.name}</span>
                  <button
                    type="button"
                    onClick={() => {
                      setEditingId(g.id);
                      setEditName(g.name);
                    }}
                    className="text-sm text-indigo-600 hover:underline"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => remove(g.id)}
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
