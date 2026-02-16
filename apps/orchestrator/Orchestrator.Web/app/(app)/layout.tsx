import { redirect } from "next/navigation";
import { auth0 } from "@/lib/auth0";
import { AppHeader } from "@/components/app-header";
import { AppSidebar } from "@/components/app-sidebar";
import { NoGroupContext } from "@/components/no-group-context";
import { getGroupId, getReturnUrl, getSelectedOrgId } from "@/lib/group-context";

interface GroupInfo {
  id: string;
  name: string;
}

/** Auth0 user may include given_name, family_name, nickname from OIDC profile. */
type Auth0User = { name?: string | null; email?: string | null; given_name?: string | null; family_name?: string | null; nickname?: string | null };

/**
 * Prefer API/user-record name when it looks like a real name (no @).
 * Otherwise derive from Auth0: given_name + family_name, nickname, or name if not email-like, else email local part.
 */
function displayNameForSidebar(apiUserName: string | null, auth0User: Auth0User): string {
  const looksLikeEmail = (s: string) => s.includes("@");
  if (apiUserName && apiUserName.trim() && !looksLikeEmail(apiUserName.trim())) {
    return apiUserName.trim();
  }
  const given = auth0User.given_name?.trim();
  const family = auth0User.family_name?.trim();
  if (given || family) {
    return [given, family].filter(Boolean).join(" ");
  }
  if (auth0User.nickname?.trim()) {
    return auth0User.nickname.trim();
  }
  const name = auth0User.name?.trim();
  if (name && !looksLikeEmail(name)) {
    return name;
  }
  const email = auth0User.email?.trim();
  if (email) {
    const local = email.split("@")[0];
    if (local) return local;
  }
  return "User";
}

export interface OrgItem {
  id: string;
  groupId: string;
  parentOrganizationId?: string | null;
  name: string;
  city?: string | null;
  state?: string | null;
}

export default async function AppLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth0.getSession();

  // Unauthenticated users get no chrome (landing page, etc.)
  if (!session?.user) {
    return <>{children}</>;
  }

  // Check for group context cookie and ATS return URL
  const groupId = await getGroupId();
  const returnUrl = await getReturnUrl() || process.env.NEXT_PUBLIC_ATS_URL || null;

  // No cookie at all -- user hasn't come through /sso from the ATS
  if (!groupId) {
    return (
      <div className="flex flex-col min-h-screen">
        <AppHeader groupInfo={null} returnUrl={returnUrl} />
        <div className="flex flex-1 overflow-hidden">
          <AppSidebar hasGroup={false} user={session.user} displayName={displayNameForSidebar(null, session.user)} />
          <main className="flex-1 overflow-auto bg-background">
            <NoGroupContext />
          </main>
        </div>
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
    redirect("/api/auth/login?returnTo=/my-agents");
  }

  // Cookie holds the ATS (external) group ID. Look up the Orchestrator group
  // by external ID to get the display name, and the user's display name from
  // the Orchestrator user record (Auth0 often defaults name to email).
  let groupInfo: GroupInfo = { id: groupId, name: groupId };
  let userName: string | null = null;
  let organizations: OrgItem[] = [];
  let currentGroupRootOrganizationId: string | null = null;
  const selectedOrgId = await getSelectedOrgId();
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
      userName = data.userName ?? data.UserName ?? null;
      const rootId = data.currentGroupRootOrganizationId ?? data.CurrentGroupRootOrganizationId;
      if (rootId != null) currentGroupRootOrganizationId = rootId;
      const rawOrgs = data.accessibleOrganizations ?? data.AccessibleOrganizations;
      if (Array.isArray(rawOrgs)) {
        organizations = rawOrgs.map((o: Record<string, unknown>) => ({
          id: String(o.id ?? o.Id),
          groupId: String(o.groupId ?? o.GroupId),
          parentOrganizationId: o.parentOrganizationId != null ? String(o.parentOrganizationId) : (o.ParentOrganizationId != null ? String(o.ParentOrganizationId) : null),
          name: String(o.name ?? o.Name),
          city: o.city != null ? String(o.city) : (o.City != null ? String(o.City) : null),
          state: o.state != null ? String(o.state) : (o.State != null ? String(o.State) : null),
        }));
      }
    }
  } catch {
    // Fetch failed -- header will use Auth0 values as fallback
  }

  return (
    <div className="flex flex-col min-h-screen">
      <AppHeader groupInfo={groupInfo} returnUrl={returnUrl} organizations={organizations} selectedOrgId={selectedOrgId} currentGroupRootOrganizationId={currentGroupRootOrganizationId} />
      <div className="flex flex-1 overflow-hidden">
        <AppSidebar hasGroup={true} user={session.user} displayName={displayNameForSidebar(userName, session.user)} />
        <main className="flex-1 overflow-auto bg-background">
          {children}
        </main>
      </div>
    </div>
  );
}
