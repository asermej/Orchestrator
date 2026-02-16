"use client";

import { useState, useEffect } from "react";
import { testAtsApi } from "@/lib/test-ats-api";
import { useMeContext, useMe } from "@/components/app-shell";

interface MeUser {
  id: string;
  name?: string | null;
  email?: string | null;
}

interface MeResponse {
  user: MeUser;
}

export default function ProfilePage() {
  const meCtx = useMeContext();
  const me = useMe();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Use the already-loaded me data from AppShell context if available,
    // otherwise fall back to an API call
    if (me?.user) {
      setName(me.user.name ?? "");
      setEmail(me.user.email ?? "");
      setLoading(false);
      return;
    }

    const fetchMe = async () => {
      try {
        const data = await testAtsApi.get<MeResponse>("/api/v1/me");
        setName(data.user.name ?? "");
        setEmail(data.user.email ?? "");
      } catch {
        setError("Failed to load profile.");
      } finally {
        setLoading(false);
      }
    };
    fetchMe();
  }, [me]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      const data = await testAtsApi.put<MeResponse>("/api/v1/me", {
        name: name.trim() || null,
        email: email.trim() || null,
      });
      setName(data.user.name ?? "");
      setEmail(data.user.email ?? "");
      setSuccess("Profile updated successfully.");
      meCtx?.refreshMe();
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Failed to update profile."
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <div className="mb-6">
        <a
          href="/settings"
          className="text-sm text-indigo-600 hover:text-indigo-800"
        >
          &larr; Back to Settings
        </a>
      </div>

      <h1 className="text-2xl font-bold text-slate-900 mb-4">Profile</h1>

      <div className="max-w-xl">
        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <h2 className="text-lg font-semibold text-slate-900 mb-2">
            Personal Information
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Update your name and email address.
          </p>

          {loading ? (
            <div className="text-sm text-slate-400">Loading profile...</div>
          ) : (
            <form onSubmit={handleSave} className="space-y-4">
              <div>
                <label
                  htmlFor="profile-name"
                  className="block text-sm font-medium text-slate-700 mb-1"
                >
                  Name
                </label>
                <input
                  id="profile-name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                  placeholder="Your full name"
                />
              </div>

              <div>
                <label
                  htmlFor="profile-email"
                  className="block text-sm font-medium text-slate-700 mb-1"
                >
                  Email
                </label>
                <input
                  id="profile-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                  placeholder="you@example.com"
                />
              </div>

              {success && (
                <div className="bg-green-50 border border-green-200 rounded-lg p-3">
                  <p className="text-green-700 text-sm">{success}</p>
                </div>
              )}

              {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-3">
                  <p className="text-red-700 text-sm">{error}</p>
                </div>
              )}

              <button
                type="submit"
                disabled={saving}
                className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {saving ? "Saving..." : "Save Changes"}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
