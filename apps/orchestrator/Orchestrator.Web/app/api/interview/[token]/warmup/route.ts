import { NextRequest, NextResponse } from "next/server";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Public endpoint for warming up interview audio cache.
 * No authentication required - used by public interview pages.
 * Pre-generates TTS audio for all interview questions.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ token: string }> }
) {
  try {
    const { token } = await params;

    if (!token) {
      return NextResponse.json(
        { error: "Missing token" },
        { status: 400 }
      );
    }

    // Call the backend warmup endpoint
    const warmupUrl = `${API_BASE}/api/v1/interview/by-token/${token}/audio/warmup`;
    const backendResponse = await fetch(warmupUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
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

    const result = await backendResponse.json();
    return NextResponse.json(result);
  } catch (error) {
    console.error("[interview/warmup]", error);
    return NextResponse.json(
      {
        error:
          error instanceof Error ? error.message : "Internal server error",
      },
      { status: 500 }
    );
  }
}
