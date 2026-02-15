import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";

/**
 * Route Handler to set the candidate session cookie.
 * Called by the client component after receiving the token from the server-rendered page.
 */
export async function POST(request: NextRequest) {
  const { token } = await request.json();

  if (!token || typeof token !== "string") {
    return NextResponse.json({ error: "Missing token" }, { status: 400 });
  }

  const cookieStore = await cookies();
  cookieStore.set("candidate_session", token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict",
    maxAge: 7200, // 2 hours
    path: "/",
  });

  return NextResponse.json({ ok: true });
}
