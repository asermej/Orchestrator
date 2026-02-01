import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth0";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Proxies GET /api/voice/message/[messageId]/audio to the backend message/[id]/audio endpoint.
 * Runs on the server so we can use getAccessToken() (cookies/session).
 */
export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ messageId: string }> }
) {
  try {
    const { messageId } = await params;
    if (!messageId) {
      return NextResponse.json(
        { error: "messageId is required" },
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

    const backendUrl = `${API_BASE}/api/v1/message/${messageId}/audio`;
    const backendResponse = await fetch(backendUrl, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        {
          error:
            errorData.message ||
            errorData.error ||
            `Backend error: ${backendResponse.status}`,
        },
        { status: backendResponse.status }
      );
    }

    const blob = await backendResponse.blob();
    const headers = new Headers();
    headers.set("content-type", backendResponse.headers.get("content-type") || "audio/mpeg");

    return new NextResponse(blob, { status: 200, headers });
  } catch (error) {
    console.error("[voice/message/audio]", error);
    return NextResponse.json(
      {
        error: error instanceof Error ? error.message : "Internal server error",
      },
      { status: 500 }
    );
  }
}
