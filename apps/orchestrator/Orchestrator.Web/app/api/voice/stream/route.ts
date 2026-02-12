import { NextRequest, NextResponse } from "next/server";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Streaming endpoint for interview voice synthesis.
 * No authentication required - used by public interview pages.
 * Proxies streaming response from backend Agent/{id}/voice/stream endpoint.
 * Audio chunks are forwarded as they arrive for low-latency playback.
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

    // Call the backend streaming voice endpoint
    const streamUrl = `${API_BASE}/api/v1/Agent/${agentId}/voice/stream`;
    const backendResponse = await fetch(streamUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
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

    // Stream the response body directly to the client
    const responseBody = backendResponse.body;
    if (!responseBody) {
      return NextResponse.json(
        { error: "No response body from backend" },
        { status: 500 }
      );
    }

    // Return streaming response with appropriate headers
    // Do NOT set Transfer-Encoding: chunked - Next.js handles this automatically
    return new NextResponse(responseBody, {
      status: 200,
      headers: {
        "Content-Type": "audio/mpeg",
        "Cache-Control": "no-cache",
      },
    });
  } catch (error) {
    // Don't log full error details if it contains binary data
    const errorMessage = error instanceof Error ? error.message : "Internal server error";
    // Only log a summary, not the full error object which may contain binary data
    console.error("[voice/stream] Error:", errorMessage);
    return NextResponse.json(
      {
        error: errorMessage,
      },
      { status: 500 }
    );
  }
}
