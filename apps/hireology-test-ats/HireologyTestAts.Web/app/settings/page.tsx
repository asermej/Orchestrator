"use client";

import { useMe } from "@/components/app-shell";

export default function SettingsPage() {
  const me = useMe();
  const isSuperadmin = me?.isSuperadmin ?? false;

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900 mb-4">Settings</h1>

      <div className="max-w-xl">
        {/* Webhook Configuration — superadmin diagnostic panel */}
        {isSuperadmin && (
          <div className="bg-white border border-slate-200 rounded-lg p-5 mb-6">
            <h2 className="text-lg font-semibold text-slate-900 mb-2">
              Webhook Configuration
            </h2>
            <p className="text-sm text-slate-600 mb-4">
              Global webhook endpoint &mdash; auto-configured during group sync.
              Webhooks are registered automatically whenever a group is created
              or updated. No manual setup is required.
            </p>

            <div className="bg-green-50 border border-green-200 rounded-lg p-3">
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-green-500" />
                <p className="text-green-800 text-sm font-medium">
                  Auto-configured
                </p>
              </div>
              <p className="text-green-700 text-xs mt-1 pl-4">
                Webhook URL is set to{" "}
                <span className="font-mono">
                  {"<ats-base-url>"}/api/v1/webhooks/orchestrator
                </span>{" "}
                for each group during sync.
              </p>
            </div>

            <p className="text-xs text-slate-400 mt-3">
              If webhooks are not arriving, re-save the group from the Group
              Admin page to trigger a re-sync.
            </p>
          </div>
        )}

        {/* Environment Info */}
        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <h2 className="text-lg font-semibold text-slate-900 mb-2">
            Environment
          </h2>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-slate-500">Orchestrator API</span>
              <span className="text-slate-700 font-mono text-xs">
                Configured in appsettings
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Test ATS API</span>
              <span className="text-slate-700 font-mono text-xs">
                http://localhost:5001
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Test ATS Web</span>
              <span className="text-slate-700 font-mono text-xs">
                http://localhost:3001
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
