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

  // Cookie holds the ATS (external) group ID. Look up the Orchestrator group
  // by external ID to get the display name.
  let groupInfo: GroupInfo = { id: groupId, name: groupId };
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const base = apiUrl.endsWith("/api/v1") ? apiUrl : `${apiUrl}/api/v1`;
    const tokenData = await auth0.getAccessToken();
    const res = await fetch(`${base}/Group/by-external-id/${groupId}`, {
      headers: {
        Authorization: `Bearer ${tokenData.token}`,
        "X-Group-Id": groupId,
      },
      cache: "no-store",
    });
    if (res.ok) {
      const data = await res.json();
      groupInfo = { id: data.id, name: data.name };
    }
  } catch {
    // Group name fetch failed -- sidebar will show the group ID instead
  }

  return (
    <div className="flex min-h-screen">
      <AppSidebar user={session.user} groupInfo={groupInfo} />
      <main className="flex-1 overflow-auto bg-background">
        {children}
      </main>
    </div>
  );
}
