import { auth0, getAccessToken } from "@/lib/auth0";
import { NextResponse } from "next/server";

export async function GET() {
  const token = await getAccessToken();
  if (!token) {
    return NextResponse.json({ error: "Not authenticated" }, { status: 401 });
  }

  // Include user profile info from the session (ID token) so the API
  // can get email/name even when they're not in the access token
  let email: string | undefined;
  let name: string | undefined;
  try {
    const session = await auth0.getSession();
    if (session?.user) {
      email = session.user.email ?? undefined;
      name = session.user.name ?? undefined;
    }
  } catch {
    // non-fatal: access token is still valid
  }

  return NextResponse.json({ accessToken: token, email, name });
}
