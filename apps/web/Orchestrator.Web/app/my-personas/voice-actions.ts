"use server";

import { apiGet, apiPost, apiPostFormData } from "@/lib/api-client-server";

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

export interface RecordConsentResponse {
  consentRecordId: string;
}

export interface CloneVoiceResponse {
  voiceId: string;
  voiceName: string;
}

export async function getAvailableVoices(): Promise<AvailableVoicesResponse> {
  return apiGet<AvailableVoicesResponse>("/Voice/elevenlabs");
}

export async function getStockVoices(): Promise<AvailableVoicesResponse> {
  return apiGet<AvailableVoicesResponse>("/Voice/elevenlabs/stock");
}

export async function recordConsent(personaId: string, consentTextVersion?: string, attested: boolean = true): Promise<RecordConsentResponse> {
  return apiPost<RecordConsentResponse>("/Voice/consent", {
    personaId,
    consentTextVersion: consentTextVersion ?? null,
    attested,
  });
}

export async function selectPersonaVoice(
  personaId: string,
  voiceProvider: string,
  voiceType: string,
  voiceId: string,
  voiceName?: string
): Promise<void> {
  await apiPost(`/Persona/${personaId}/voice/select`, {
    voiceProvider,
    voiceType,
    voiceId,
    voiceName: voiceName ?? voiceId,
  });
}

export async function cloneVoice(
  personaId: string,
  voiceName: string,
  consentRecordId: string,
  sampleDurationSeconds: number,
  file: File,
  styleLane?: string | null
): Promise<CloneVoiceResponse> {
  const formData = new FormData();
  formData.append("personaId", personaId);
  formData.append("voiceName", voiceName);
  formData.append("consentRecordId", consentRecordId);
  formData.append("sampleDurationSeconds", String(sampleDurationSeconds));
  if (styleLane) formData.append("styleLane", styleLane);
  formData.append("file", file);
  return apiPostFormData<CloneVoiceResponse>("/Voice/clone", formData);
}
