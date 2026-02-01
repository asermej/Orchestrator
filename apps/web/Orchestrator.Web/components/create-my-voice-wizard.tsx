"use client";

import { useState, useRef, useCallback } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Loader2, Upload, Mic, MicOff, CheckCircle2, Volume2 } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";

function TestVoiceStep({ personaId, onDone }: { personaId: string; onDone: () => void }) {
  const [text, setText] = useState("Hey — I'm your Surrova persona voice.");
  const [loading, setLoading] = useState(false);
  return (
    <div className="space-y-3">
      <div className="space-y-2">
        <Label>Test phrase</Label>
        <Textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Enter a short phrase..."
          rows={2}
          className="resize-none"
        />
      </div>
      <div className="flex gap-2">
        <Button
          type="button"
          variant="outline"
          onClick={async () => {
            setLoading(true);
            try {
              const res = await fetch(`/api/personas/${personaId}/voice/test`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text: text || "Hey — I'm your Surrova persona voice." }),
              });
              if (!res.ok) {
                toast.error("Test failed");
                return;
              }
              const blob = await res.blob();
              const url = URL.createObjectURL(blob);
              const audio = new Audio(url);
              await audio.play();
              audio.onended = () => URL.revokeObjectURL(url);
            } catch {
              toast.error("Failed to play test");
            } finally {
              setLoading(false);
            }
          }}
          disabled={loading}
        >
          {loading ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Generating...
            </>
          ) : (
            <>
              <Volume2 className="h-4 w-4 mr-2" />
              Generate & play
            </>
          )}
        </Button>
        <Button type="button" onClick={onDone}>
          Done
        </Button>
      </div>
    </div>
  );
}
import { recordConsent, cloneVoice, selectPersonaVoice } from "@/app/my-personas/voice-actions";
import { toast } from "sonner";

const CONSENT_TEXT =
  "Your voice will be processed by ElevenLabs and used for text-to-speech for this persona only.";

const CONSENT_CHECKBOX_LABEL =
  "I am recording my own voice OR I have explicit permission to clone this voice.";

const STYLE_LANES = [
  { value: "conversational", label: "Conversational (default)" },
  { value: "narration", label: "Narration" },
  { value: "high_energy", label: "High-energy / Ad read" },
] as const;

const RECORDING_CHECKLIST = [
  "Quiet room, no fan or AC",
  "Consistent mic distance",
  "Avoid echo and reverb",
  "Don't whisper or shout",
];

const MIN_DURATION_SECONDS = 10;
const MAX_DURATION_SECONDS = 300;
const RECOMMENDED_MIN_SECONDS = 60;
const RECOMMENDED_MAX_SECONDS = 180;

const SCRIPTS: Record<"conversational" | "narration" | "high_energy", string> = {
  conversational: `Read naturally. Keep one consistent vibe. Avoid dramatic swings.

Hi, this is a longer sample so the model can really learn my voice. I'm going to read for about a minute and a half. Here's a sentence with some variety — we'll use numbers like 12, 47, 2026, and 3.14159. Words like synthesis, algorithm, nostalgia, and unequivocally are great for training. The key is to sound like myself the whole time, not like I'm reading a script. So I'll keep going. Imagine I'm just talking to a friend. Maybe we're catching up over coffee, or I'm explaining something I care about. I want the model to hear how I pause, how I emphasize certain words, how my tone stays in one lane. Another sentence with numbers: 100, 42, 1999. And a few more training words: specifically, generally, absolutely, typically. I'll throw in a question now and then — can you hear the difference? Good. One more stretch: the weather today is fine, the project is on track, and I'll check back in next week. Thanks for listening to the whole thing!`,
  narration: `Read in a calm, steady narration tone. Keep the same pace throughout.

Welcome. This is a narration sample. I'll speak clearly and evenly for about ninety seconds. We'll include numbers: 12, 47, 2026, and 3.14159. Use words like synthesis, algorithm, nostalgia, and unequivocally. Stay consistent. This style works well for audiobooks, documentaries, or any content that needs a clear, neutral delivery. I'll add a bit more. The goal is to maintain one steady rhythm. No sudden jumps in energy or volume. Think of a calm river — it flows at the same pace. Here are more numbers: 100, 42, 1999. And more vocabulary: specifically, generally, absolutely, typically. The listener should feel guided, not rushed. One more paragraph. The report was completed on time. The team reviewed the data. The results were unequivocally positive. Thank you for listening.`,
  high_energy: `Upbeat and energetic — but don't shout. One consistent energy level.

Hey! This is a high-energy sample. Keep it fun and lively for about a minute and a half. Numbers: 12, 47, 2026, 3.14159. Say synthesis, algorithm, nostalgia, unequivocally. Stay upbeat without yelling. I'm excited but in control — like a great host or a pep talk. Let's keep the energy up. Imagine you're announcing something cool, or hyping up a product, or just sharing good news. More numbers: 100, 42, 1999. And more words: specifically, generally, absolutely, typically. The key is one consistent high energy — no dropping into a monotone, no suddenly shouting. We're going for confident and fun. One more push: this is going to be great, the team nailed it, and we're ready to go. Let's do this!`,
};

function getAudioDurationSeconds(file: File): Promise<number> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const audio = new Audio(url);
    audio.onloadedmetadata = () => {
      URL.revokeObjectURL(url);
      resolve(Math.floor(audio.duration));
    };
    audio.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error("Could not read audio duration"));
    };
  });
}

export interface CreateMyVoiceWizardProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  personaId: string;
  personaName?: string;
  onSuccess?: () => void;
}

export function CreateMyVoiceWizard({
  open,
  onOpenChange,
  personaId,
  personaName = "My persona",
  onSuccess,
}: CreateMyVoiceWizardProps) {
  const [step, setStep] = useState<0 | 1 | 2 | 3 | 4>(0);
  const [consentChecked, setConsentChecked] = useState(false);
  const [consentRecordId, setConsentRecordId] = useState<string | null>(null);
  const [consentLoading, setConsentLoading] = useState(false);
  const [styleLane, setStyleLane] = useState<"conversational" | "narration" | "high_energy">("conversational");
  const [file, setFile] = useState<File | null>(null);
  const [durationSeconds, setDurationSeconds] = useState<number | null>(null);
  const [durationError, setDurationError] = useState<string | null>(null);
  const [durationOverride, setDurationOverride] = useState(false);
  const [voiceName, setVoiceName] = useState("");
  const [cloneLoading, setCloneLoading] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingElapsedSeconds, setRecordingElapsedSeconds] = useState(0);
  const [playbackPlaying, setPlaybackPlaying] = useState(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const playbackRef = useRef<HTMLAudioElement | null>(null);
  const recordingIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const resetWizard = useCallback(() => {
    setStep(0);
    setConsentChecked(false);
    setConsentRecordId(null);
    setStyleLane("conversational");
    setFile(null);
    setDurationSeconds(null);
    setDurationError(null);
    setDurationOverride(false);
    setVoiceName("");
  }, []);

  const handleClose = useCallback(
    (open: boolean) => {
      if (open) {
        onOpenChange(true);
        return;
      }
      const hasUnsavedRecording = isRecording || (step === 2 && file);
      if (hasUnsavedRecording && !window.confirm("Discard recording? Your progress will be lost.")) {
        return;
      }
      if (recordingIntervalRef.current) {
        clearInterval(recordingIntervalRef.current);
        recordingIntervalRef.current = null;
      }
      playbackRef.current?.pause();
      resetWizard();
      onOpenChange(false);
    },
    [onOpenChange, resetWizard, isRecording, step, file]
  );

  const handleConsentContinue = async () => {
    if (!consentChecked) {
      toast.error("Please agree to the consent to continue.");
      return;
    }
    setConsentLoading(true);
    try {
      const res = await recordConsent(personaId, undefined, true);
      setConsentRecordId(res.consentRecordId);
      setStep(1);
    } catch (err) {
      console.error("Record consent error", err);
      toast.error("Failed to record consent. Please try again.");
    } finally {
      setConsentLoading(false);
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;
    setDurationError(null);
    setDurationSeconds(null);
    setFile(selected);
    try {
      const duration = await getAudioDurationSeconds(selected);
      setDurationSeconds(duration);
      if (duration < MIN_DURATION_SECONDS)
        setDurationError(`Recording must be at least ${MIN_DURATION_SECONDS} seconds.`);
      else if (duration > MAX_DURATION_SECONDS)
        setDurationError(`Recording must be at most ${MAX_DURATION_SECONDS} seconds (5 minutes).`);
    } catch {
      setDurationError("Could not read audio. Use MP3 or WAV.");
    }
  };

  const startRecording = async () => {
    setFile(null);
    setDurationSeconds(null);
    setDurationError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream);
      chunksRef.current = [];
      recorder.ondataavailable = (e) => {
        if (e.data.size) chunksRef.current.push(e.data);
      };
      recorder.onstop = async () => {
        stream.getTracks().forEach((t) => t.stop());
        const blob = new Blob(chunksRef.current, { type: "audio/webm" });
        const duration = await new Promise<number>((resolve, reject) => {
          const url = URL.createObjectURL(blob);
          const audio = new Audio(url);
          audio.onloadedmetadata = () => {
            URL.revokeObjectURL(url);
            resolve(Math.floor(audio.duration));
          };
          audio.onerror = () => {
            URL.revokeObjectURL(url);
            reject(new Error("Could not read duration"));
          };
        });
        const f = new File([blob], "recording.webm", { type: "audio/webm" });
        setFile(f);
        setDurationSeconds(duration);
        if (duration < MIN_DURATION_SECONDS)
          setDurationError(`Recording must be at least ${MIN_DURATION_SECONDS} seconds.`);
        else if (duration > MAX_DURATION_SECONDS)
          setDurationError(`Recording must be at most ${MAX_DURATION_SECONDS} seconds.`);
        else setDurationError(null);
      };
      mediaRecorderRef.current = recorder;
      recorder.start();
      setRecordingElapsedSeconds(0);
      recordingIntervalRef.current = setInterval(() => {
        setRecordingElapsedSeconds((prev) => prev + 1);
      }, 1000);
      setIsRecording(true);
    } catch (err) {
      console.error("Recording error", err);
      toast.error("Could not access microphone. Please allow microphone access.");
    }
  };

  const stopRecording = () => {
    if (recordingIntervalRef.current) {
      clearInterval(recordingIntervalRef.current);
      recordingIntervalRef.current = null;
    }
    const recorder = mediaRecorderRef.current;
    if (recorder && recorder.state !== "inactive") {
      recorder.stop();
      setIsRecording(false);
    }
    setRecordingElapsedSeconds(0);
  };

  const handleReRecord = () => {
    setFile(null);
    setDurationSeconds(null);
    setDurationError(null);
    setDurationOverride(false);
    playbackRef.current?.pause();
    setPlaybackPlaying(false);
  };

  const canProceedFromStep2 =
    file &&
    durationSeconds !== null &&
    durationSeconds >= MIN_DURATION_SECONDS &&
    durationSeconds <= MAX_DURATION_SECONDS;

  const handleStep2Next = () => {
    if (canProceedFromStep2) {
      if (!voiceName.trim()) setVoiceName(`${personaName} Voice`);
      setStep(3);
    }
  };

  const handleCreateVoice = async () => {
    if (!consentRecordId || !file || durationSeconds === null || !voiceName.trim()) return;
    if (durationSeconds < MIN_DURATION_SECONDS || durationSeconds > MAX_DURATION_SECONDS) return;
    setCloneLoading(true);
    try {
      const res = await cloneVoice(
        personaId,
        voiceName.trim(),
        consentRecordId,
        durationSeconds,
        file,
        styleLane
      );
      await selectPersonaVoice(
        personaId,
        "elevenlabs",
        "user_cloned",
        res.voiceId,
        res.voiceName
      );
      toast.success(`Voice "${res.voiceName}" created and selected.`);
      handleClose(false);
      onSuccess?.();
    } catch (err) {
      console.error("Clone voice error", err);
      toast.error(err instanceof Error ? err.message : "Failed to create voice. Please try again.");
    } finally {
      setCloneLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create my voice</DialogTitle>
        </DialogHeader>

        {step === 0 && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground whitespace-pre-wrap">{CONSENT_TEXT}</p>
            <div className="flex items-start gap-2">
              <Checkbox
                id="consent"
                checked={consentChecked}
                onCheckedChange={(v) => setConsentChecked(v === true)}
              />
              <Label htmlFor="consent" className="text-sm font-normal cursor-pointer">
                {CONSENT_CHECKBOX_LABEL}
              </Label>
            </div>
            <Button
              onClick={handleConsentContinue}
              disabled={!consentChecked || consentLoading}
              className="w-full"
            >
              {consentLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                "Continue"
              )}
            </Button>
          </div>
        )}

        {step === 1 && (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Style lane</Label>
              <Select
                value={styleLane}
                onValueChange={(v) => setStyleLane(v as "conversational" | "narration" | "high_energy")}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {STYLE_LANES.map((lane) => (
                    <SelectItem key={lane.value} value={lane.value}>
                      {lane.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="rounded-lg border bg-muted/30 p-3 space-y-2">
              <p className="text-sm font-medium">Recording checklist</p>
              <ul className="text-sm text-muted-foreground space-y-1">
                {RECORDING_CHECKLIST.map((item) => (
                  <li key={item} className="flex items-center gap-2">
                    <CheckCircle2 className="h-4 w-4 shrink-0 text-muted-foreground" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
            <p className="text-sm text-muted-foreground">
              Aim for <strong>{RECOMMENDED_MIN_SECONDS}–{RECOMMENDED_MAX_SECONDS}s</strong> (recommended). Minimum {MIN_DURATION_SECONDS}s. Max {MAX_DURATION_SECONDS}s (avoid over 3 minutes).
            </p>
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => setStep(0)}>
                Back
              </Button>
              <Button type="button" onClick={() => setStep(2)} className="flex-1">
                Next
              </Button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <p className="text-sm font-medium">Script ({STYLE_LANES.find((l) => l.value === styleLane)?.label ?? styleLane})</p>
                <p className="text-xs text-muted-foreground">
                  Read naturally. Keep one consistent vibe. Avoid dramatic swings.
                </p>
                <div className="rounded-lg border bg-muted/30 p-3 max-h-48 overflow-y-auto">
                  <p className="text-sm whitespace-pre-wrap">{SCRIPTS[styleLane]}</p>
                </div>
              </div>
              <div className="space-y-2">
                <p className="text-sm font-medium">Record or upload</p>
                <p className="text-xs text-muted-foreground">
                  Aim for {RECOMMENDED_MIN_SECONDS}–{RECOMMENDED_MAX_SECONDS}s. Min {MIN_DURATION_SECONDS}s, max {MAX_DURATION_SECONDS}s.
                </p>
                <div className="flex flex-col gap-2">
                  <div className="flex items-center gap-2">
                    <Input
                      id="voice-file"
                      type="file"
                      accept="audio/*,.mp3,.wav,.webm,.m4a"
                      onChange={handleFileChange}
                      className="flex-1"
                    />
                    <Button
                      type="button"
                      variant={isRecording ? "destructive" : "outline"}
                      onClick={isRecording ? stopRecording : startRecording}
                      disabled={!!file && !isRecording}
                    >
                      {isRecording ? (
                        <>
                          <MicOff className="h-4 w-4 mr-2" />
                          Stop
                        </>
                      ) : (
                        <>
                          <Mic className="h-4 w-4 mr-2" />
                          Record
                        </>
                      )}
                    </Button>
                  </div>
                  {isRecording && (
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-mono tabular-nums">
                        {Math.floor(recordingElapsedSeconds / 60)}:{(recordingElapsedSeconds % 60).toString().padStart(2, "0")}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        Target: {RECOMMENDED_MIN_SECONDS}–{RECOMMENDED_MAX_SECONDS}s
                      </span>
                    </div>
                  )}
                  {file && !isRecording && (
                    <div className="space-y-2">
                      <p className="text-sm text-muted-foreground">
                        {file.name}
                        {durationSeconds !== null && ` · ${durationSeconds}s`}
                      </p>
                      <div className="flex gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            if (playbackRef.current) {
                              if (playbackPlaying) {
                                playbackRef.current.pause();
                                setPlaybackPlaying(false);
                              } else {
                                playbackRef.current.play();
                                setPlaybackPlaying(true);
                              }
                            } else {
                              const url = URL.createObjectURL(file);
                              const audio = new Audio(url);
                              playbackRef.current = audio;
                              audio.onplay = () => setPlaybackPlaying(true);
                              audio.onpause = () => setPlaybackPlaying(false);
                              audio.onended = () => {
                                setPlaybackPlaying(false);
                                URL.revokeObjectURL(url);
                                playbackRef.current = null;
                              };
                              audio.play();
                            }
                          }}
                        >
                          {playbackPlaying ? "Pause" : "Play"}
                        </Button>
                        <Button type="button" variant="outline" size="sm" onClick={handleReRecord}>
                          Re-record
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
                {durationError && (
                  <p className="text-sm text-destructive">{durationError}</p>
                )}
              </div>
            </div>
            {(durationSeconds !== null && durationSeconds < RECOMMENDED_MIN_SECONDS && durationSeconds >= MIN_DURATION_SECONDS) && (
              <div className="flex items-start gap-2">
                <Checkbox
                  id="duration-override"
                  checked={durationOverride}
                  onCheckedChange={(v) => setDurationOverride(v === true)}
                />
                <Label htmlFor="duration-override" className="text-sm font-normal cursor-pointer text-muted-foreground">
                  I understand quality may suffer with shorter audio
                </Label>
              </div>
            )}
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => setStep(1)}>
                Back
              </Button>
              <Button
                type="button"
                onClick={handleStep2Next}
                disabled={!canProceedFromStep2}
                className="flex-1"
              >
                Next
              </Button>
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Give your voice a name. It will appear in the voice list for this persona.
            </p>
            <div className="space-y-2">
              <Label htmlFor="voice-name">Voice name</Label>
              <Input
                id="voice-name"
                value={voiceName}
                onChange={(e) => setVoiceName(e.target.value)}
                placeholder={`e.g. ${personaName} Voice`}
                maxLength={100}
              />
            </div>
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => setStep(2)}>
                Back
              </Button>
              <Button
                onClick={handleCreateVoice}
                disabled={!voiceName.trim() || cloneLoading}
                className="flex-1"
              >
                {cloneLoading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  "Create voice"
                )}
              </Button>
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Your voice has been created. Test it with a short phrase below, or close to finish.
            </p>
            <TestVoiceStep personaId={personaId} onDone={() => handleClose(false)} />
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
