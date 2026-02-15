import { NextRequest, NextResponse } from "next/server";
import { BlobServiceClient } from "@azure/storage-blob";

const AZURE_STORAGE_CONNECTION_STRING = process.env.AZURE_STORAGE_CONNECTION_STRING || "";
const CONTAINER_NAME = "interview-recordings";

/**
 * Serves interview audio directly from Azure Blob Storage with Range request
 * support so the browser's <audio> element can seek and show accurate progress.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ key: string }> }
) {
  const { key } = await params;

  if (!key) {
    return NextResponse.json({ error: "Missing audio key" }, { status: 400 });
  }

  if (!AZURE_STORAGE_CONNECTION_STRING) {
    return NextResponse.json(
      { error: "Azure Storage connection string not configured" },
      { status: 500 }
    );
  }

  try {
    const blobServiceClient = BlobServiceClient.fromConnectionString(
      AZURE_STORAGE_CONNECTION_STRING
    );
    const containerClient = blobServiceClient.getContainerClient(CONTAINER_NAME);
    const blobClient = containerClient.getBlobClient(key);

    // Get blob properties (size, content type) without downloading
    const properties = await blobClient.getProperties().catch(() => null);
    if (!properties) {
      return NextResponse.json({ error: "Audio not found" }, { status: 404 });
    }

    const totalSize = properties.contentLength ?? 0;
    const contentType = properties.contentType || "audio/webm";

    // Parse Range header for partial content support
    const rangeHeader = request.headers.get("range");

    if (rangeHeader) {
      const match = rangeHeader.match(/bytes=(\d+)-(\d*)/);
      if (match) {
        const start = parseInt(match[1], 10);
        const end = match[2] ? parseInt(match[2], 10) : totalSize - 1;
        const chunkSize = end - start + 1;

        const downloadResponse = await blobClient.download(start, chunkSize);
        const readable = downloadResponse.readableStreamBody;
        if (!readable) {
          return NextResponse.json(
            { error: "Failed to read audio stream" },
            { status: 500 }
          );
        }

        const chunks: Uint8Array[] = [];
        for await (const chunk of readable as AsyncIterable<Uint8Array>) {
          chunks.push(chunk);
        }
        const buffer = Buffer.concat(chunks);

        return new NextResponse(buffer, {
          status: 206,
          headers: {
            "Content-Type": contentType,
            "Content-Length": chunkSize.toString(),
            "Content-Range": `bytes ${start}-${end}/${totalSize}`,
            "Accept-Ranges": "bytes",
            "Cache-Control": "private, max-age=3600",
          },
        });
      }
    }

    // No Range header — return the full file
    const downloadResponse = await blobClient.download();
    const readable = downloadResponse.readableStreamBody;
    if (!readable) {
      return NextResponse.json(
        { error: "Failed to read audio stream" },
        { status: 500 }
      );
    }

    const chunks: Uint8Array[] = [];
    for await (const chunk of readable as AsyncIterable<Uint8Array>) {
      chunks.push(chunk);
    }
    const buffer = Buffer.concat(chunks);

    return new NextResponse(buffer, {
      status: 200,
      headers: {
        "Content-Type": contentType,
        "Content-Length": totalSize.toString(),
        "Accept-Ranges": "bytes",
        "Cache-Control": "private, max-age=3600",
      },
    });
  } catch (error) {
    console.error("[interview-audio] Error fetching from blob storage:", error);
    return NextResponse.json(
      { error: "Failed to fetch audio" },
      { status: 500 }
    );
  }
}
