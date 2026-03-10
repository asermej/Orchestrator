import { NextRequest, NextResponse } from "next/server";

const ORCHESTRATOR_BASE_URL = process.env.ORCHESTRATOR_BASE_URL || "http://localhost:5000";
const ORCHESTRATOR_API_KEY = process.env.ORCHESTRATOR_API_KEY || "";

/**
 * Fetches competency-based interview responses (holistic scores, Q&A exchanges, audio)
 * from the Orchestrator ATS API and rewrites audio URLs to our local proxy route.
 */
export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  if (!id) {
    return NextResponse.json({ error: "Missing interview ID" }, { status: 400 });
  }

  if (!ORCHESTRATOR_API_KEY) {
    return NextResponse.json({ error: "Orchestrator API key not configured" }, { status: 500 });
  }

  try {
    const backendUrl = `${ORCHESTRATOR_BASE_URL}/api/v1/ats/interviews/${encodeURIComponent(id)}/competency-responses`;
    const response = await fetch(backendUrl, {
      headers: {
        "X-API-Key": ORCHESTRATOR_API_KEY,
      },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Competency responses not found" },
        { status: response.status }
      );
    }

    const data = await response.json();

    const rewritten = Array.isArray(data)
      ? data.map((cr: Record<string, unknown>) => ({
          ...cr,
          audioUrl:
            typeof cr.audioUrl === "string" && cr.audioUrl.startsWith("/api/v1/interview-audio/")
              ? cr.audioUrl.replace("/api/v1/interview-audio/", "/api/interview-audio/")
              : cr.audioUrl,
        }))
      : data;

    return NextResponse.json(rewritten);
  } catch (error) {
    console.error("[interview-competency-responses proxy] Error:", error);
    return NextResponse.json(
      { error: "Failed to fetch competency responses" },
      { status: 500 }
    );
  }
}
