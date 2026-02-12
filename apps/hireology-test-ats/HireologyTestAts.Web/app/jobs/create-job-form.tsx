"use client";

import { useState, useEffect } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface CreateJobFormProps {
  onClose: () => void;
  onSaved: () => void;
}

interface OrganizationItem {
  id: string;
  name: string;
  groupId: string;
}

interface MeResponse {
  currentContext: { selectedOrganizationId?: string | null };
  accessibleOrganizations: OrganizationItem[];
}

export function CreateJobForm({ onClose, onSaved }: CreateJobFormProps) {
  const [externalJobId, setExternalJobId] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [location, setLocation] = useState("");
  const [organizationId, setOrganizationId] = useState<string>("");
  const [organizations, setOrganizations] = useState<OrganizationItem[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    testAtsApi.get<MeResponse>("/api/v1/me").then((me) => {
      setOrganizations(me.accessibleOrganizations);
      if (me.currentContext?.selectedOrganizationId) {
        setOrganizationId(me.currentContext.selectedOrganizationId);
      } else if (me.accessibleOrganizations.length > 0) {
        setOrganizationId(me.accessibleOrganizations[0].id);
      }
    }).catch(() => {});
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!externalJobId.trim() || !title.trim()) {
      setError("External ID and Title are required");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await testAtsApi.post("/api/v1/jobs", {
        externalJobId: externalJobId.trim(),
        title: title.trim(),
        description: description.trim() || null,
        location: location.trim() || null,
        status: "active",
        organizationId: organizationId || null,
      });
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div
        className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-lg font-semibold text-slate-900 mb-4">Add job</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="p-2 rounded bg-red-50 text-red-800 text-sm">{error}</div>
          )}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">External job ID *</label>
            <input
              type="text"
              value={externalJobId}
              onChange={(e) => setExternalJobId(e.target.value)}
              className="w-full rounded border border-slate-300 px-3 py-2 text-slate-900"
              placeholder="e.g. job-001"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Title *</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full rounded border border-slate-300 px-3 py-2 text-slate-900"
              placeholder="Job title"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded border border-slate-300 px-3 py-2 text-slate-900"
              rows={3}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Location (address)</label>
            <input
              type="text"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              className="w-full rounded border border-slate-300 px-3 py-2 text-slate-900"
              placeholder="e.g. Remote"
            />
          </div>
          {organizations.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Organization</label>
              <select
                value={organizationId}
                onChange={(e) => setOrganizationId(e.target.value)}
                className="w-full rounded border border-slate-300 px-3 py-2 text-slate-900"
              >
                {organizations.map((org) => (
                  <option key={org.id} value={org.id}>
                    {org.name}
                  </option>
                ))}
              </select>
              <p className="text-xs text-slate-500 mt-1">Defaults to your current location when left as-is.</p>
            </div>
          )}
          <div className="flex gap-2 justify-end pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-slate-600 hover:text-slate-900"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
            >
              {saving ? "Saving…" : "Save & sync to Orchestrator"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
