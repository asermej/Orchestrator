import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth0";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";
const AUDIO_URL = `${API_BASE}/api/v1/conversation/respond/audio`;

/**
 * Proxies POST /api/voice/respond/audio to the backend conversation/respond/audio endpoint.
 * Runs on the server so we can use getAccessToken() (cookies/session).
 * Streams the audio response back to the client.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { agentId, message } = body;

    if (!agentId || !message) {
      return NextResponse.json(
        { error: "agentId and message are required" },
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

    const backendResponse = await fetch(AUDIO_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ agentId, message }),
      // @ts-expect-error Node 18+ ReadableStream from fetch is valid
      duplex: "half",
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        { error: errorData.message || errorData.error || `Backend error: ${backendResponse.status}` },
        { status: backendResponse.status }
      );
    }

    // Stream the response body back to the client.
    // Do NOT forward Transfer-Encoding: the backend sends chunked encoding, but
    // Node's fetch gives us the decoded (raw) body stream. If we forward
    // Transfer-Encoding: chunked, the client sees raw MP3 bytes and fails with
    // "Invalid character in chunk size". Let Next.js add chunked encoding when
    // it sends the stream to the client.
    const headers = new Headers();
    const contentType =
      backendResponse.headers.get("content-type") || "audio/mpeg";
    headers.set("content-type", contentType);

    return new NextResponse(backendResponse.body, {
      status: backendResponse.status,
      headers,
    });
  } catch (error) {
    console.error("[voice/respond/audio]", error);
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Internal server error" },
      { status: 500 }
    );
  }
}
