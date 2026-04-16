# Interview voice latency metrics

Single-line JSON logs tagged `[TIMING][METRICS_ROW]` (frontend) and `[INTERVIEW][METRICS_ROW]` (API / domain) share a **`correlationId`** (UUID generated per STT-final turn in the browser) so you can join browser and server timings for the same `respond-to-turn` request.

## Schemas

| `schema` | Source | When |
|----------|--------|------|
| `interview.respond_to_turn.fe` | `interview-experience.tsx` | After AI audio playback ends (MediaSource path) |
| `interview.respond_to_turn.api` | `InterviewController.RespondToTurn` | After response stream completes |
| `interview.respond_to_turn.pipeline` | `InterviewConversationManager.RespondToTurnAsync` | After LLM+TTS pipeline completes |

## Field definitions (FE `interview.respond_to_turn.fe`)

| Field | Meaning |
|-------|---------|
| `endpointingMs` | `sttDoneAt - lastSpeechAt` (Deepgram silence after last partial) |
| `fetchToHeadersMs` | Time to first byte (headers) from `fetch` start |
| `fetchToFirstAudioMs` | First MP3 buffer appended to `MediaSource` |
| `sttToFirstAudioMs` | Same, measured from STT final |
| `fetchToPlayMs` / `pipelineDelayMs` | First `audio.play()` from fetch / from STT final |
| `perceivedDelayMs` | `audio.play()` − `lastSpeechAt` |
| `sttToPlaybackEndMs` | Playback `ended` − STT final |

## Baseline (reference session, for regression comparison)

Captured from product logs (see plan): pipeline delay p50 ~2.8s, perceived ~3.2s; pre-generated vs streaming ~50/50.

## STT endpointing experiment

`use-deepgram-stt.ts` uses `endpointing=700` (ms). **Revert to 800** if candidates are cut off mid-thought. Keep `utterance_end_ms=1500` as fallback.
