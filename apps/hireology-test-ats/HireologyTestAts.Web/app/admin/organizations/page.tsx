"use client";

import { Suspense, useEffect, useState, useCallback } from "react";
import { useSearchParams } from "next/navigation";
import { testAtsApi } from "@/lib/test-ats-api";

interface Group {
  id: string;
  rootOrganizationId?: string | null;
  name: string;
}

interface Organization {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

interface TreeNode extends Organization {
  children: TreeNode[];
  depth: number;
}

function buildTree(orgs: Organization[]): TreeNode[] {
  const map = new Map<string, TreeNode>();
  const roots: TreeNode[] = [];

  // Create nodes
  for (const org of orgs) {
    map.set(org.id, { ...org, children: [], depth: 0 });
  }

  // Build hierarchy
  for (const org of orgs) {
    const node = map.get(org.id)!;
    if (org.parentOrganizationId && map.has(org.parentOrganizationId)) {
      const parent = map.get(org.parentOrganizationId)!;
      node.depth = parent.depth + 1;
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }

  // Fix depths recursively
  function setDepths(nodes: TreeNode[], depth: number) {
    for (const n of nodes) {
      n.depth = depth;
      setDepths(n.children, depth + 1);
    }
  }
  setDepths(roots, 0);

  return roots;
}

export default function AdminOrganizationsPage() {
  return (
    <Suspense fallback={<p className="text-slate-500">Loading...</p>}>
      <AdminOrganizationsContent />
    </Suspense>
  );
}

function AdminOrganizationsContent() {
  const searchParams = useSearchParams();
  const initialGroupId = searchParams.get("groupId") || "";

  const [groups, setGroups] = useState<Group[]>([]);
  const [selectedGroupId, setSelectedGroupId] = useState(initialGroupId);
  const [orgs, setOrgs] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Create form
  const [showCreate, setShowCreate] = useState(false);
  const [createParentId, setCreateParentId] = useState<string>("");
  const [createName, setCreateName] = useState("");
  const [createCity, setCreateCity] = useState("");
  const [createState, setCreateState] = useState("");

  // Editing
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editCity, setEditCity] = useState("");
  const [editState, setEditState] = useState("");

  // Moving
  const [movingId, setMovingId] = useState<string | null>(null);
  const [moveTargetId, setMoveTargetId] = useState<string>("");

  // Collapsed nodes
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const loadGroups = useCallback(async () => {
    try {
      const list = await testAtsApi.get<Group[]>("/api/v1/groups");
      setGroups(list);
      if (!selectedGroupId && list.length > 0) {
        setSelectedGroupId(list[0].id);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load groups");
    }
  }, [selectedGroupId]);

  const loadOrgs = useCallback(async () => {
    if (!selectedGroupId) return;
    setLoading(true);
    setError(null);
    try {
      const list = await testAtsApi.get<Organization[]>(
        `/api/v1/organizations/tree?groupId=${selectedGroupId}`
      );
      setOrgs(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load organizations");
    } finally {
      setLoading(false);
    }
  }, [selectedGroupId]);

  useEffect(() => {
    loadGroups();
  }, [loadGroups]);

  useEffect(() => {
    if (selectedGroupId) {
      loadOrgs();
    }
  }, [selectedGroupId, loadOrgs]);

  const toggleCollapse = (id: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const createOrg = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!createName.trim() || !selectedGroupId) return;
    setError(null);
    try {
      await testAtsApi.post("/api/v1/organizations", {
        groupId: selectedGroupId,
        parentOrganizationId: createParentId || undefined,
        name: createName.trim(),
        city: createCity.trim() || undefined,
        state: createState.trim() || undefined,
      });
      setCreateName("");
      setCreateCity("");
      setCreateState("");
      setCreateParentId("");
      setShowCreate(false);
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create");
    }
  };

  const deleteOrg = async (id: string, name: string) => {
    if (!confirm(`Delete organization "${name}"?`)) return;
    setError(null);
    try {
      await testAtsApi.delete(`/api/v1/organizations/${id}`);
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to delete");
    }
  };

  const startCreateChild = (parentId: string) => {
    setCreateParentId(parentId);
    setShowCreate(true);
    setCreateName("");
    setCreateCity("");
    setCreateState("");
  };

  const startEdit = (org: Organization) => {
    setEditingId(org.id);
    setEditName(org.name);
    setEditCity(org.city ?? "");
    setEditState(org.state ?? "");
  };

  const saveEdit = async () => {
    if (!editingId || !editName.trim()) return;
    setError(null);
    try {
      await testAtsApi.put(`/api/v1/organizations/${editingId}`, {
        name: editName.trim(),
        city: editCity.trim() || null,
        state: editState.trim() || null,
      });
      setEditingId(null);
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to update");
    }
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditName("");
    setEditCity("");
    setEditState("");
  };

  // Collect all descendant IDs for a given org (to exclude from move targets)
  const getDescendantIds = (orgId: string): Set<string> => {
    const ids = new Set<string>();
    const collect = (parentId: string) => {
      for (const o of orgs) {
        if (o.parentOrganizationId === parentId) {
          ids.add(o.id);
          collect(o.id);
        }
      }
    };
    collect(orgId);
    return ids;
  };

  // Get valid move targets for a given org (exclude self, descendants, and current parent)
  const getMoveTargets = (orgId: string) => {
    const org = orgs.find((o) => o.id === orgId);
    if (!org) return [];
    const excluded = getDescendantIds(orgId);
    excluded.add(orgId);
    return orgs.filter((o) => !excluded.has(o.id));
  };

  const startMove = (org: Organization) => {
    setMovingId(org.id);
    setMoveTargetId(org.parentOrganizationId ?? "__root__");
  };

  const cancelMove = () => {
    setMovingId(null);
    setMoveTargetId("");
  };

  const saveMove = async () => {
    if (!movingId) return;
    const newParentId = moveTargetId === "__root__" ? null : moveTargetId || null;
    setError(null);
    try {
      await testAtsApi.post(`/api/v1/organizations/${movingId}/move`, {
        newParentOrganizationId: newParentId,
      });
      setMovingId(null);
      setMoveTargetId("");
      await loadOrgs();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to move organization");
    }
  };

  const tree = buildTree(orgs);

  function renderNode(node: TreeNode, isRoot = false): React.ReactNode {
    const isCollapsed = collapsed.has(node.id);
    const hasChildren = node.children.length > 0;
    const isEditing = editingId === node.id;
    const isMoving = movingId === node.id;

    return (
      <div key={node.id} className={isRoot ? "" : "border-l-2 border-slate-200 ml-4"}>
        <div className={`flex items-center gap-2 py-2 pl-3 pr-2 hover:bg-slate-50 group ${isRoot ? "border-b border-slate-100 mb-1" : ""}`}>
          {hasChildren ? (
            <button
              type="button"
              onClick={() => toggleCollapse(node.id)}
              className="w-5 h-5 flex items-center justify-center text-slate-400 hover:text-slate-600 text-xs"
            >
              {isCollapsed ? "+" : "-"}
            </button>
          ) : (
            <span className="w-5 h-5 flex items-center justify-center text-slate-300 text-xs">
              &middot;
            </span>
          )}
          {isEditing ? (
            <div className="flex items-center gap-2 flex-1">
              <input
                type="text"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="px-2 py-1 border border-slate-300 rounded text-sm w-48"
                autoFocus
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    saveEdit();
                  }
                  if (e.key === "Escape") cancelEdit();
                }}
              />
              <input
                type="text"
                value={editCity}
                onChange={(e) => setEditCity(e.target.value)}
                placeholder="City"
                className="px-2 py-1 border border-slate-300 rounded text-sm w-28"
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    saveEdit();
                  }
                  if (e.key === "Escape") cancelEdit();
                }}
              />
              <input
                type="text"
                value={editState}
                onChange={(e) => setEditState(e.target.value)}
                placeholder="State"
                className="px-2 py-1 border border-slate-300 rounded text-sm w-16"
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    saveEdit();
                  }
                  if (e.key === "Escape") cancelEdit();
                }}
              />
              <button
                type="button"
                onClick={saveEdit}
                className="text-xs text-indigo-600 hover:underline font-medium"
              >
                Save
              </button>
              <button
                type="button"
                onClick={cancelEdit}
                className="text-xs text-slate-500 hover:underline"
              >
                Cancel
              </button>
            </div>
          ) : isMoving ? (
            <div className="flex items-center gap-2 flex-1">
              <span className="font-medium text-sm">{node.name}</span>
              <span className="text-xs text-slate-500">→ Move to:</span>
              <select
                value={moveTargetId}
                onChange={(e) => setMoveTargetId(e.target.value)}
                className="px-2 py-1 border border-slate-300 rounded text-sm"
                autoFocus
              >
                <option value="__root__">— Root level (no parent) —</option>
                {getMoveTargets(node.id).map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                    {o.city || o.state
                      ? ` (${[o.city, o.state].filter(Boolean).join(", ")})`
                      : ""}
                  </option>
                ))}
              </select>
              <button
                type="button"
                onClick={saveMove}
                className="text-xs text-indigo-600 hover:underline font-medium"
              >
                Move
              </button>
              <button
                type="button"
                onClick={cancelMove}
                className="text-xs text-slate-500 hover:underline"
              >
                Cancel
              </button>
            </div>
          ) : (
            <>
              <span className={`font-medium text-sm ${isRoot ? "text-slate-900" : ""}`}>{node.name}</span>
              {isRoot && (
                <span className="text-xs text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded">
                  root org
                </span>
              )}
              {(node.city || node.state) && (
                <span className="text-slate-500 text-xs">
                  {[node.city, node.state].filter(Boolean).join(", ")}
                </span>
              )}
              <div className="ml-auto flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  type="button"
                  onClick={() => startEdit(node)}
                  className="text-xs text-indigo-600 hover:underline"
                >
                  Edit
                </button>
                {!isRoot && (
                  <button
                    type="button"
                    onClick={() => startMove(node)}
                    className="text-xs text-indigo-600 hover:underline"
                  >
                    Move
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => startCreateChild(node.id)}
                  className="text-xs text-indigo-600 hover:underline"
                >
                  + Child
                </button>
                {!isRoot && (
                  <button
                    type="button"
                    onClick={() => deleteOrg(node.id, node.name)}
                    className="text-xs text-red-600 hover:underline"
                  >
                    Delete
                  </button>
                )}
              </div>
            </>
          )}
        </div>
        {!isCollapsed &&
          hasChildren &&
          node.children.map((child) => renderNode(child))}
      </div>
    );
  }

  const selectedGroup = groups.find((g) => g.id === selectedGroupId);

  return (
    <div>
      <div className="flex items-center gap-4 mb-6">
        <a href="/admin" className="text-slate-500 hover:text-slate-700">
          ← Admin
        </a>
        <h1 className="text-2xl font-bold text-slate-900">Organizations</h1>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-amber-50 border border-amber-200 rounded-lg text-amber-800">
          {error}
        </div>
      )}

      {/* Group selector */}
      <div className="mb-6 flex items-center gap-3">
        <label className="text-sm font-medium text-slate-700">Group:</label>
        <select
          value={selectedGroupId}
          onChange={(e) => setSelectedGroupId(e.target.value)}
          className="px-3 py-2 border border-slate-300 rounded-lg min-w-[200px]"
        >
          <option value="">-- Select group --</option>
          {groups.map((g) => (
            <option key={g.id} value={g.id}>
              {g.name}
            </option>
          ))}
        </select>
        {selectedGroupId && selectedGroup?.rootOrganizationId && (
          <button
            type="button"
            onClick={() => {
              startCreateChild(selectedGroup.rootOrganizationId!);
            }}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm"
          >
            + Add organization
          </button>
        )}
      </div>

      {/* Create form */}
      {showCreate && selectedGroupId && (
        <form
          onSubmit={createOrg}
          className="mb-6 p-4 border border-slate-200 rounded-lg bg-white space-y-3"
        >
          <h3 className="font-medium text-slate-900">
            Create organization
            {createParentId && (
              <span className="text-slate-500 font-normal text-sm">
                {" "}
                — child of{" "}
                {orgs.find((o) => o.id === createParentId)?.name || "..."}
              </span>
            )}
          </h3>
          <div className="flex gap-3">
            <input
              type="text"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              placeholder="Name (required)"
              className="px-3 py-2 border border-slate-300 rounded-lg flex-1"
              autoFocus
            />
            <input
              type="text"
              value={createCity}
              onChange={(e) => setCreateCity(e.target.value)}
              placeholder="City"
              className="px-3 py-2 border border-slate-300 rounded-lg w-40"
            />
            <input
              type="text"
              value={createState}
              onChange={(e) => setCreateState(e.target.value)}
              placeholder="State"
              className="px-3 py-2 border border-slate-300 rounded-lg w-24"
            />
          </div>
          <div className="flex gap-2">
            <button
              type="submit"
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm"
            >
              Create
            </button>
            <button
              type="button"
              onClick={() => setShowCreate(false)}
              className="px-4 py-2 text-slate-500 hover:text-slate-700 text-sm"
            >
              Cancel
            </button>
          </div>
        </form>
      )}

      {/* Organization tree */}
      {!selectedGroupId ? (
        <p className="text-slate-500">Select a group to view its organizations.</p>
      ) : loading ? (
        <p className="text-slate-500">Loading...</p>
      ) : orgs.length === 0 ? (
        <p className="text-slate-500">
          No organizations in {selectedGroup?.name || "this group"} yet.
        </p>
      ) : (
        <div className="border border-slate-200 rounded-lg p-4 bg-white">
          <h3 className="text-sm font-medium text-slate-500 mb-3 uppercase tracking-wide">
            {selectedGroup?.name} — Organization Tree
          </h3>
          {tree.map((root) => renderNode(root, root.id === selectedGroup?.rootOrganizationId))}
        </div>
      )}
    </div>
  );
}
