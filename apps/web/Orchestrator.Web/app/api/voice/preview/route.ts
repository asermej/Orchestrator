import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth0";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";
const PREVIEW_URL = `${API_BASE}/api/v1/Voice/preview`;

/**
 * Proxies POST /api/voice/preview to the backend Voice/preview endpoint.
 * Returns audio/mpeg for the given voiceId and text.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { voiceId, text } = body;

    if (!voiceId) {
      return NextResponse.json(
        { error: "voiceId is required" },
        { status: 400 }
      );
    }

    const accessToken = await getAccessToken();
    if (!accessToken) {
      return NextResponse.json(
        { error: "Not authenticated" },
        { status: 401 }
      );
    }

    const backendResponse = await fetch(PREVIEW_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ voiceId, text: text ?? "Hey — I'm your Surrova persona voice." }),
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        { error: errorData.error ?? errorData.message ?? `Backend error: ${backendResponse.status}` },
        { status: backendResponse.status }
      );
    }

    const contentType = backendResponse.headers.get("content-type") || "audio/mpeg";
    const arrayBuffer = await backendResponse.arrayBuffer();

    return new NextResponse(arrayBuffer, {
      status: 200,
      headers: { "content-type": contentType },
    });
  } catch (error) {
    console.error("[voice/preview]", error);
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Internal server error" },
      { status: 500 }
    );
  }
}
