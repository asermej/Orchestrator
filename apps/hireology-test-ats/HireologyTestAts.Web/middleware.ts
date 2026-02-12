import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";
import { auth0 } from "./lib/auth0";

/** When Auth0 is not configured, skip login so the app runs without auth until you set .env.local */
const auth0Configured =
  typeof process.env.AUTH0_CLIENT_ID === "string" &&
  process.env.AUTH0_CLIENT_ID.length > 0;

export async function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  if (path === "/careers") return NextResponse.next();
  if (!auth0Configured) return NextResponse.next();
  return await auth0.middleware(request);
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
};
