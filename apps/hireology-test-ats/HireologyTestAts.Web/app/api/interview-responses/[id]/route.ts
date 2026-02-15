import { NextRequest, NextResponse } from "next/server";

const ORCHESTRATOR_BASE_URL = process.env.ORCHESTRATOR_BASE_URL || "http://localhost:5000";
const ORCHESTRATOR_API_KEY = process.env.ORCHESTRATOR_API_KEY || "";

/**
 * Fetches interview responses (questions, transcripts, audio URLs) from the
 * Orchestrator ATS API and rewrites audio URLs to point to our local
 * /api/interview-audio/[key] route which reads directly from Azure Blob Storage.
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
    const backendUrl = `${ORCHESTRATOR_BASE_URL}/api/v1/ats/interviews/${encodeURIComponent(id)}/responses`;
    const response = await fetch(backendUrl, {
      headers: {
        "X-API-Key": ORCHESTRATOR_API_KEY,
      },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Responses not found" },
        { status: response.status }
      );
    }

    const data = await response.json();

    // Rewrite audio URLs from Orchestrator API paths to our local blob route
    // e.g., "/api/v1/interview-audio/abc.webm" → "/api/interview-audio/abc.webm"
    // Our /api/interview-audio/[key] route reads directly from Azure Blob Storage
    const rewritten = Array.isArray(data)
      ? data.map((r: Record<string, unknown>) => ({
          ...r,
          audioUrl:
            typeof r.audioUrl === "string" && r.audioUrl.startsWith("/api/v1/interview-audio/")
              ? r.audioUrl.replace("/api/v1/interview-audio/", "/api/interview-audio/")
              : r.audioUrl,
        }))
      : data;

    return NextResponse.json(rewritten);
  } catch (error) {
    console.error("[interview-responses proxy] Error:", error);
    return NextResponse.json(
      { error: "Failed to fetch responses" },
      { status: 500 }
    );
  }
}
