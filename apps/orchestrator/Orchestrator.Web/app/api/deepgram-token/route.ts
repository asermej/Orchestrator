import { NextResponse } from "next/server";

export async function POST() {
  const apiKey = process.env.DEEPGRAM_API_KEY;
  if (!apiKey || apiKey === "REPLACE_ME") {
    return NextResponse.json(
      { error: "Deepgram API key is not configured" },
      { status: 500 }
    );
  }

  try {
    const response = await fetch("https://api.deepgram.com/v1/auth/grant", {
      method: "POST",
      headers: {
        Authorization: `Token ${apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ ttl_seconds: 120 }),
    });

    if (!response.ok) {
      const text = await response.text().catch(() => "");
      console.error("Deepgram token grant failed:", response.status, text);
      return NextResponse.json(
        { error: "Failed to obtain Deepgram token" },
        { status: 502 }
      );
    }

    const data = await response.json();
    const token = data.access_token;
    if (!token) {
      console.error("Deepgram response missing access_token:", JSON.stringify(data));
      return NextResponse.json(
        { error: "Deepgram returned an invalid token response" },
        { status: 502 }
      );
    }

    return NextResponse.json({ token });
  } catch (err) {
    console.error("Deepgram token request error:", err);
    return NextResponse.json(
      { error: "Failed to reach Deepgram API" },
      { status: 502 }
    );
  }
}
