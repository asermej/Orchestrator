import { NextRequest, NextResponse } from "next/server";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL?.replace(/\/api\/v1\/?$/, "") ||
  "http://localhost:5000";

/**
 * Generates a question and streams TTS audio in a single request.
 * Returns the generated question text in X-Generated-Question header
 * and streams audio/mpeg in the body.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json().catch(() => ({}));
    const { interviewId, competencyId, includeTransition, previousCompetencyName } = body;

    if (!interviewId || !competencyId) {
      return NextResponse.json(
        { error: "Missing interviewId or competencyId" },
        { status: 400 }
      );
    }

    const url = `${API_BASE}/api/v1/Interview/${interviewId}/runtime/generate-question-audio`;
    const backendResponse = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        competencyId,
        includeTransition: includeTransition || false,
        previousCompetencyName,
      }),
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

    const generatedQuestion = backendResponse.headers.get("X-Generated-Question") || "";
    const competencyIdHeader = backendResponse.headers.get("X-Competency-Id") || "";

    return new NextResponse(responseBody, {
      status: 200,
      headers: {
        "Content-Type": "audio/mpeg",
        "Cache-Control": "no-cache",
        "X-Generated-Question": generatedQuestion,
        "X-Competency-Id": competencyIdHeader,
        "Access-Control-Expose-Headers": "X-Generated-Question, X-Competency-Id",
      },
    });
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : "Internal server error";
    console.error("[voice/generate-and-stream] Error:", errorMessage);
    return NextResponse.json({ error: errorMessage }, { status: 500 });
  }
}
