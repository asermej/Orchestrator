import { NextRequest, NextResponse } from "next/server";

const ORCHESTRATOR_BASE_URL = process.env.ORCHESTRATOR_BASE_URL || "http://localhost:5000";
const ORCHESTRATOR_API_KEY = process.env.ORCHESTRATOR_API_KEY || "";

/**
 * Fetches interview result (summary, score, recommendation, per-question scores)
 * from the Orchestrator ATS API.
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
    const backendUrl = `${ORCHESTRATOR_BASE_URL}/api/v1/ats/interviews/${encodeURIComponent(id)}/result`;
    const response = await fetch(backendUrl, {
      headers: {
        "X-API-Key": ORCHESTRATOR_API_KEY,
      },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Result not found" },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error("[interview-results proxy] Error:", error);
    return NextResponse.json(
      { error: "Failed to fetch interview result" },
      { status: 500 }
    );
  }
}
