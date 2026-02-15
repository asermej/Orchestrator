import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth0";

const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

/**
 * Proxies interview audio requests to the backend API with authentication.
 * The browser's <audio> element cannot send Bearer tokens, so this route
 * fetches the audio server-side and streams it back to the client.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ key: string }> }
) {
  const { key } = await params;

  if (!key) {
    return NextResponse.json({ error: "Missing audio key" }, { status: 400 });
  }

  try {
    const accessToken = await getAccessToken();

    const backendUrl = `${apiUrl}/api/v1/interview-audio/${encodeURIComponent(key)}`;
    const response = await fetch(backendUrl, {
      headers: {
        ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
      },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Audio not found" },
        { status: response.status }
      );
    }

    const contentType = response.headers.get("Content-Type") || "audio/webm";
    const audioBuffer = await response.arrayBuffer();

    return new NextResponse(audioBuffer, {
      status: 200,
      headers: {
        "Content-Type": contentType,
        "Cache-Control": "private, max-age=3600",
      },
    });
  } catch (error) {
    console.error("[interview-audio proxy] Error fetching audio:", error);
    return NextResponse.json(
      { error: "Failed to fetch audio" },
      { status: 500 }
    );
  }
}
