"use client";

import { useEffect, useState } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

interface MeResponse {
  isSuperadmin: boolean;
}

export default function AdminPage() {
  const [authorized, setAuthorized] = useState<boolean | null>(null);

  useEffect(() => {
    testAtsApi
      .get<MeResponse>("/api/v1/me")
      .then((me) => setAuthorized(me.isSuperadmin))
      .catch(() => setAuthorized(false));
  }, []);

  if (authorized === null) {
    return <p className="text-slate-500">Loading...</p>;
  }

  if (!authorized) {
    return (
      <div className="p-8">
        <h1 className="text-2xl font-bold text-red-600 mb-2">Access Denied</h1>
        <p className="text-slate-600">
          You need superadmin privileges to access this page.
        </p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900 mb-2">Admin Panel</h1>
      <p className="text-slate-600 mb-6">
        Superadmin tools for managing the platform.
      </p>
      <ul className="space-y-3">
        <li>
          <a
            href="/admin/superadmins"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Superadmins
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — Promote or demote superadmin users.
          </span>
        </li>
        <li>
          <a
            href="/admin/groups"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Groups
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — Create and manage groups (each auto-creates a root organization).
          </span>
        </li>
        <li>
          <a
            href="/admin/organizations"
            className="text-indigo-600 hover:text-indigo-800 font-medium"
          >
            Organizations
          </a>
          <span className="text-slate-500 text-sm ml-2">
            — View organization hierarchy and create nested organizations.
          </span>
        </li>
      </ul>
    </div>
  );
}
