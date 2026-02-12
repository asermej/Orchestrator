import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth0";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Proxies POST /api/agents/[id]/voice/test to the backend Agent/{id}/voice/test endpoint.
 * Returns audio/mpeg for the agent's current voice speaking the given text.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id: agentId } = await params;
    const body = await request.json().catch(() => ({}));
    const text = body?.text ?? "Hello! I'm your AI interviewer.";

    const accessToken = await getAccessToken();
    if (!accessToken) {
      return NextResponse.json(
        { error: "Not authenticated" },
        { status: 401 }
      );
    }

    const testUrl = `${API_BASE}/api/v1/Agent/${agentId}/voice/test`;
    const backendResponse = await fetch(testUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ text }),
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        {
          error:
            errorData.error ??
            errorData.message ??
            `Backend error: ${backendResponse.status}`,
        },
        { status: backendResponse.status }
      );
    }

    const contentType =
      backendResponse.headers.get("content-type") || "audio/mpeg";
    const arrayBuffer = await backendResponse.arrayBuffer();

    return new NextResponse(arrayBuffer, {
      status: 200,
      headers: { "content-type": contentType },
    });
  } catch (error) {
    console.error("[agents/voice/test]", error);
    return NextResponse.json(
      {
        error:
          error instanceof Error ? error.message : "Internal server error",
      },
      { status: 500 }
    );
  }
}
