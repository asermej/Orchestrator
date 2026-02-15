"use client";

import { useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

export default function SettingsPage() {
  const [configuring, setConfiguring] = useState(false);
  const [webhookResult, setWebhookResult] = useState<{
    webhookUrl: string;
    configured: boolean;
  } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleConfigureWebhook = async () => {
    setConfiguring(true);
    setError(null);
    try {
      const result = await testAtsApi.post<{
        webhookUrl: string;
        configured: boolean;
      }>("/api/v1/settings/configure-webhook", {});
      setWebhookResult(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to configure webhook");
    } finally {
      setConfiguring(false);
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900 mb-4">Settings</h1>

      <div className="max-w-xl">
        {/* Webhook Configuration */}
        <div className="bg-white border border-slate-200 rounded-lg p-5 mb-6">
          <h2 className="text-lg font-semibold text-slate-900 mb-2">
            Webhook Configuration
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Register this ATS&apos;s webhook URL with the Orchestrator so that
            interview completion results are automatically sent back here.
          </p>

          <button
            onClick={handleConfigureWebhook}
            disabled={configuring}
            className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {configuring ? "Configuring..." : "Configure Webhook"}
          </button>

          {webhookResult && (
            <div className="mt-4 bg-green-50 border border-green-200 rounded-lg p-3">
              <p className="text-green-800 text-sm font-medium">
                Webhook configured successfully!
              </p>
              <p className="text-green-700 text-xs mt-1 font-mono">
                {webhookResult.webhookUrl}
              </p>
            </div>
          )}

          {error && (
            <div className="mt-4 bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          )}
        </div>

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
