import { NextRequest, NextResponse } from "next/server";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Public endpoint for interview voice synthesis.
 * No authentication required - used by public interview pages.
 * Proxies to backend Agent/{id}/voice/test endpoint.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json().catch(() => ({}));
    const { agentId, text } = body;

    if (!agentId) {
      return NextResponse.json(
        { error: "Missing agentId" },
        { status: 400 }
      );
    }

    if (!text) {
      return NextResponse.json(
        { error: "Missing text" },
        { status: 400 }
      );
    }

    // Call the backend voice test endpoint (which handles ElevenLabs)
    const testUrl = `${API_BASE}/api/v1/Agent/${agentId}/voice/test`;
    const backendResponse = await fetch(testUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        // For public interview endpoints, we use a service account or skip auth
        // The backend should allow unauthenticated voice test for configured agents
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
    console.error("[voice/speak]", error);
    return NextResponse.json(
      {
        error:
          error instanceof Error ? error.message : "Internal server error",
      },
      { status: 500 }
    );
  }
}
