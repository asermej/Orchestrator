import { NextRequest, NextResponse } from "next/server";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * LATENCY-CRITICAL: Streaming conversation turn proxy.
 * Forwards the candidate's transcript to the backend, which generates an AI response
 * via streaming OpenAI → sentence buffer → WebSocket TTS, and streams audio/mpeg back.
 * Response metadata (text, type, follow-up target) is returned in headers.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json().catch(() => ({}));
    const { interviewId, ...turnPayload } = body;

    if (!interviewId || !turnPayload.candidateTranscript || !turnPayload.competencyId) {
      return NextResponse.json(
        { error: "Missing interviewId, candidateTranscript, or competencyId" },
        { status: 400 }
      );
    }

    const url = `${API_BASE}/api/v1/Interview/${interviewId}/runtime/respond-to-turn`;
    const backendResponse = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(turnPayload),
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        { error: errorData.error ?? `Backend error: ${backendResponse.status}` },
        { status: backendResponse.status }
      );
    }

    const responseBody = backendResponse.body;
    if (!responseBody) {
      return NextResponse.json(
        { error: "No response body from backend" },
        { status: 500 }
      );
    }

    const responseText = backendResponse.headers.get("X-Response-Text") || "";
    const responseType = backendResponse.headers.get("X-Response-Type") || "transition";
    const followUpTarget = backendResponse.headers.get("X-Follow-Up-Target") || "";
    const languageCode = backendResponse.headers.get("X-Language-Code") || "";

    const headers: Record<string, string> = {
      "Content-Type": "audio/mpeg",
      "Cache-Control": "no-cache",
      "X-Response-Text": responseText,
      "X-Response-Type": responseType,
      "Access-Control-Expose-Headers":
        "X-Response-Text, X-Response-Type, X-Follow-Up-Target, X-Language-Code",
    };

    if (followUpTarget) {
      headers["X-Follow-Up-Target"] = followUpTarget;
    }

    if (languageCode) {
      headers["X-Language-Code"] = languageCode;
    }

    return new NextResponse(responseBody, { status: 200, headers });
  } catch (error) {
    const errorMessage =
      error instanceof Error ? error.message : "Internal server error";
    console.error("[voice/respond-to-turn] Error:", errorMessage);
    return NextResponse.json({ error: errorMessage }, { status: 500 });
  }
}
