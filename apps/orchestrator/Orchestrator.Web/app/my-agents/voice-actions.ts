"use server";

import { apiGet, apiPost } from "@/lib/api-client-server";

export interface VoiceItem {
  id: string;
  name: string;
  previewText?: string | null;
  category?: string | null;
  description?: string | null;
  tags?: string[] | null;
  voiceType: string;
}

export interface AvailableVoicesResponse {
  curatedPrebuiltVoices: VoiceItem[];
  userVoices: VoiceItem[];
}

export async function getAvailableVoices(): Promise<AvailableVoicesResponse> {
  return apiGet<AvailableVoicesResponse>("/Voice/elevenlabs");
}

export async function getStockVoices(): Promise<AvailableVoicesResponse> {
  return apiGet<AvailableVoicesResponse>("/Voice/elevenlabs/stock");
}

export async function selectAgentVoice(
  agentId: string,
  voiceProvider: string,
  voiceType: string,
  voiceId: string,
  voiceName?: string
): Promise<void> {
  await apiPost(`/Agent/${agentId}/voice/select`, {
    voiceProvider,
    voiceType,
    voiceId,
    voiceName: voiceName ?? voiceId,
  });
}
