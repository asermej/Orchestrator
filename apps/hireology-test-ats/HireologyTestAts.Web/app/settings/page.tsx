"use client";

import { useState, useEffect } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface WebhookStatus {
  webhookUrl: string | null;
  configured: boolean;
}

export default function SettingsPage() {
  const [loading, setLoading] = useState(true);
  const [configuring, setConfiguring] = useState(false);
  const [webhookStatus, setWebhookStatus] = useState<WebhookStatus | null>(
    null
  );
  const [error, setError] = useState<string | null>(null);

  // Fetch webhook status on page load
  useEffect(() => {
    const fetchStatus = async () => {
      try {
        const status = await testAtsApi.get<WebhookStatus>(
          "/api/v1/settings/webhook-status"
        );
        setWebhookStatus(status);
      } catch (e) {
        // If we can't fetch status, that's okay - just show the configure button
        setWebhookStatus(null);
      } finally {
        setLoading(false);
      }
    };
    fetchStatus();
  }, []);

  const handleConfigureWebhook = async () => {
    setConfiguring(true);
    setError(null);
    try {
      const result = await testAtsApi.post<WebhookStatus>(
        "/api/v1/settings/configure-webhook",
        {}
      );
      setWebhookStatus(result);
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
        {/* Profile Link */}
        <a
          href="/settings/profile"
          className="block bg-white border border-slate-200 rounded-lg p-5 mb-6 hover:border-indigo-300 hover:bg-indigo-50/30 transition-colors group"
        >
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-900 group-hover:text-indigo-900">
                Profile
              </h2>
              <p className="text-sm text-slate-600">
                Update your name and email address.
              </p>
            </div>
            <svg
              className="w-5 h-5 text-slate-400 group-hover:text-indigo-500"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 5l7 7-7 7"
              />
            </svg>
          </div>
        </a>

        {/* Webhook Configuration */}
        <div className="bg-white border border-slate-200 rounded-lg p-5 mb-6">
          <h2 className="text-lg font-semibold text-slate-900 mb-2">
            Webhook Configuration
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Register this ATS&apos;s webhook URL with the Orchestrator so that
            interview completion results are automatically sent back here.
          </p>

          {loading ? (
            <div className="text-sm text-slate-400">
              Checking webhook status...
            </div>
          ) : webhookStatus?.configured ? (
            <div>
              <div className="bg-green-50 border border-green-200 rounded-lg p-3 mb-3">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-green-500" />
                  <p className="text-green-800 text-sm font-medium">
                    Webhook registered
                  </p>
                </div>
                <p className="text-green-700 text-xs mt-1 font-mono pl-4">
                  {webhookStatus.webhookUrl}
                </p>
              </div>
              <button
                onClick={handleConfigureWebhook}
                disabled={configuring}
                className="px-4 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {configuring ? "Reconfiguring..." : "Reconfigure Webhook"}
              </button>
            </div>
          ) : (
            <div>
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-3">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-amber-500" />
                  <p className="text-amber-800 text-sm font-medium">
                    Webhook not registered
                  </p>
                </div>
                <p className="text-amber-700 text-xs mt-1 pl-4">
                  Interview results will not be sent back to this ATS until the
                  webhook is configured.
                </p>
              </div>
              <button
                onClick={handleConfigureWebhook}
                disabled={configuring}
                className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {configuring ? "Configuring..." : "Configure Webhook"}
              </button>
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
