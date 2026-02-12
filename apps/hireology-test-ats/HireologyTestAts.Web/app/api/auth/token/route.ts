import { getAccessToken } from "@/lib/auth0";
import { NextResponse } from "next/server";

export async function GET() {
  const token = await getAccessToken();
  if (!token) {
    return NextResponse.json({ error: "Not authenticated" }, { status: 401 });
  }
  return NextResponse.json({ accessToken: token });
}
