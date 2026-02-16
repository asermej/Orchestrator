import { redirect } from "next/navigation";
import { auth0 } from "@/lib/auth0";
import { AppSidebar } from "@/components/app-sidebar";
import { NoGroupContext } from "@/components/no-group-context";
import { getGroupId } from "@/lib/group-context";

interface GroupInfo {
  id: string;
  name: string;
}

export default async function AppLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth0.getSession();

  // Unauthenticated users get no sidebar (landing page, etc.)
  if (!session?.user) {
    return <>{children}</>;
  }

  // Check for group context cookie
  const groupId = await getGroupId();

  // No cookie at all -- user hasn't come through /sso from the ATS
  if (!groupId) {
    return (
      <div className="flex min-h-screen">
        <AppSidebar user={session.user} groupInfo={null} />
        <main className="flex-1 overflow-auto bg-background">
          <NoGroupContext />
        </main>
      </div>
    );
  }

  // Early token validation: if the user has a session and group context but
  // the access token can't be refreshed (e.g. stale refresh token from a
  // previous session while the Auth0 SSO cookie is still valid from the ATS),
  // force re-authentication. Auth0 will silently issue fresh tokens using the
  // existing SSO session without prompting for credentials.
  let accessToken: string;
  try {
    const tokenData = await auth0.getAccessToken();
    accessToken = tokenData.token;
  } catch {
    redirect("/api/auth/login?returnTo=/");
  }

  // Cookie holds the ATS (external) group ID. Look up the Orchestrator group
  // by external ID to get the display name, and the user's display name from
  // the Orchestrator user record (Auth0 often defaults name to email).
  let groupInfo: GroupInfo = { id: groupId, name: groupId };
  let userName: string | null = null;
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const base = apiUrl.endsWith("/api/v1") ? apiUrl : `${apiUrl}/api/v1`;
    const headers = {
      Authorization: `Bearer ${accessToken}`,
      "X-Group-Id": groupId,
    };

    const [groupRes, meCtxRes] = await Promise.all([
      fetch(`${base}/Group/by-external-id/${groupId}`, { headers, cache: "no-store" }),
      fetch(`${base}/User/me-context`, { headers, cache: "no-store" }),
    ]);

    if (groupRes.ok) {
      const data = await groupRes.json();
      groupInfo = { id: data.id, name: data.name };
    }
    if (meCtxRes.ok) {
      const data = await meCtxRes.json();
      if (data.userName) userName = data.userName;
    }
  } catch {
    // Fetch failed -- sidebar will use Auth0 values as fallback
  }

  return (
    <div className="flex min-h-screen">
      <AppSidebar user={session.user} groupInfo={groupInfo} displayName={userName} />
      <main className="flex-1 overflow-auto bg-background">
        {children}
      </main>
    </div>
  );
}
