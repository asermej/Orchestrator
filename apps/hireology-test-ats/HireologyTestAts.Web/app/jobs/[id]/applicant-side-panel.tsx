"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { testAtsApi } from "@/lib/test-ats-api";

// ── Types ──────────────────────────────────────────────────────────────────────

interface AgentItem {
  id: string;
  displayName: string;
  profileImageUrl?: string | null;
}

interface InterviewGuideItem {
  id: string;
  name: string;
  description?: string | null;
  questionCount: number;
  isActive: boolean;
}

interface InterviewResponseData {
  id: string;
  interviewId: string;
  questionId?: string | null;
  questionText: string;
  transcript?: string | null;
  audioUrl?: string | null;
  durationSeconds?: number | null;
  responseOrder: number;
  isFollowUp: boolean;
  questionType: string;
  createdAt: string;
}

interface QuestionScoreData {
  questionIndex: number;
  question: string;
  score: number;
  maxScore: number;
  weight: number;
  feedback: string;
}

interface InterviewResultData {
  id: string;
  interviewId: string;
  summary?: string;
  score?: number;
  recommendation?: string;
  strengths?: string;
  areasForImprovement?: string;
  questionScores?: QuestionScoreData[];
  createdAt: string;
}

interface ApplicantItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  createdAt: string;
}

interface InterviewRequestItem {
  id: string;
  applicantId: string;
  jobId: string;
  orchestratorInterviewId?: string | null;
  inviteUrl?: string | null;
  shortCode?: string | null;
  status: string;
  score?: number | null;
  resultSummary?: string | null;
  resultRecommendation?: string | null;
  resultStrengths?: string | null;
  resultAreasForImprovement?: string | null;
  webhookReceivedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

interface Props {
  applicant: ApplicantItem;
  interviewRequest?: InterviewRequestItem;
  applicants: ApplicantItem[];
  currentIndex: number;
  onNavigate: (applicantId: string) => void;
  onClose: () => void;
  onInterviewSent: (request: InterviewRequestItem) => void;
  onRefreshInvite: (ir: InterviewRequestItem) => void;
}

// ── Placeholder data ───────────────────────────────────────────────────────────

const PLACEHOLDER_TAGS = [
  "2 Years Experience",
  "ABC Certified",
  "DEF Certified",
  "Within 50 Miles",
  "Previous Applicant",
];

const PLACEHOLDER_ACTIVITY = [
  { user: "Camilla Ward", action: "messaged the applicant.", date: "Mar 11th, 2024 at 12:38 pm", icon: "pencil" },
  { user: "Kathryn Hill", action: "reviewed the application.", date: "Mar 9th, 2024 at 10:38 am", icon: "pencil" },
  { user: "", action: "applied for the job.", date: "Mar 7th, 2024 at 10:28 am", icon: "flag", useSelf: true },
];

const PLACEHOLDER_QUESTIONS = [
  {
    question: "Do you like working in a fast paced environment with lots of collaboration?",
    response: "Yes, I thrive in collaborative settings. In my last role, I worked closely with cross-functional teams on tight deadlines and found the energy very motivating.",
  },
  {
    question: "What strategies do you use to manage your time effectively in a busy workplace?",
    response: "I rely on task prioritization and time-blocking. I start each day identifying the top three deliverables and protect focused work time on my calendar.",
  },
  {
    question: "Can you provide an example of a successful team project you contributed to?",
    response: "I led our Q4 product launch coordination between engineering, marketing, and sales. We delivered two weeks ahead of schedule and exceeded adoption targets by 40%.",
  },
  {
    question: "Do you like working in a fast paced environment with lots of collaboration?",
    response: "Absolutely. Fast-paced environments push me to stay organized and communicate proactively, which are strengths I've developed throughout my career.",
  },
];

// ── Score helpers ─────────────────────────────────────────────────────────────

function getQuestionScoreBadge(score: number, maxScore: number): { text: string; bg: string } {
  const pct = maxScore > 0 ? (score / maxScore) * 100 : 0;
  if (pct >= 80) return { text: "text-emerald-700", bg: "bg-emerald-50 border-emerald-200" };
  if (pct >= 60) return { text: "text-blue-700", bg: "bg-blue-50 border-blue-200" };
  if (pct >= 40) return { text: "text-amber-700", bg: "bg-amber-50 border-amber-200" };
  return { text: "text-red-700", bg: "bg-red-50 border-red-200" };
}

// ── Helper components ──────────────────────────────────────────────────────────

function Avatar({ firstName, lastName }: { firstName: string; lastName: string }) {
  const initials = `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
  return (
    <div className="w-12 h-12 rounded-full bg-slate-600 text-white flex items-center justify-center text-lg font-semibold shrink-0">
      {initials}
    </div>
  );
}

function InterviewStatusBadge({ status }: { status: string }) {
  const config: Record<string, { label: string; bg: string; text: string; icon: string }> = {
    not_started: { label: "Request Sent", bg: "bg-amber-50", text: "text-amber-700", icon: "clock" },
    in_progress: { label: "In Progress", bg: "bg-teal-50", text: "text-teal-700", icon: "clock" },
    completed: { label: "Complete", bg: "bg-teal-50", text: "text-teal-700", icon: "check" },
    link_expired: { label: "Link Expired", bg: "bg-red-50", text: "text-red-700", icon: "x" },
  };
  const c = config[status] || { label: status, bg: "bg-slate-100", text: "text-slate-600", icon: "clock" };

  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${c.bg} ${c.text}`}>
      {c.icon === "check" ? (
        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
        </svg>
      ) : c.icon === "clock" ? (
        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ) : (
        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
      )}
      {c.label}
    </span>
  );
}

function RecommendationBadge({ recommendation }: { recommendation: string }) {
  const config: Record<string, { label: string; className: string }> = {
    hire: { label: "Excellent Fit", className: "bg-green-100 text-green-800" },
    no_hire: { label: "No Hire", className: "bg-red-100 text-red-800" },
    further_review: { label: "Further Review", className: "bg-amber-100 text-amber-800" },
  };
  const c = config[recommendation] || { label: recommendation, className: "bg-slate-100 text-slate-700" };
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${c.className}`}>
      {c.label}
    </span>
  );
}

function ScoreGauge({ score }: { score: number }) {
  const pct = Math.min(100, Math.max(0, score));
  let barColor = "bg-red-500";
  if (score >= 70) barColor = "bg-teal-500";
  else if (score >= 40) barColor = "bg-amber-500";

  return (
    <div>
      <div className="text-sm font-semibold text-slate-700 mb-1">Interview Score</div>
      <div className="flex items-baseline gap-1 mb-2">
        <span className="text-3xl font-bold text-slate-900">{score}</span>
        <span className="text-sm text-slate-500">/100</span>
      </div>
      <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
        <div className={`h-full rounded-full ${barColor} transition-all`} style={{ width: `${pct}%` }} />
      </div>
      <div className="flex justify-between text-[10px] text-slate-400 mt-1">
        <span>Poor</span>
        <span>Excellent</span>
      </div>
    </div>
  );
}


/** Custom audio player that uses the known duration from the API. */
function AudioPlayer({ src, knownDuration }: { src: string; knownDuration?: number | null }) {
  const audioRef = useRef<HTMLAudioElement>(null);
  const progressRef = useRef<HTMLDivElement>(null);
  const [playing, setPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [volume, setVolume] = useState(1);

  // Use the API-provided duration (WebM files often lack duration metadata)
  const duration = knownDuration && knownDuration > 0 ? knownDuration : 0;

  const fmt = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = Math.floor(secs % 60);
    return `${m}:${s.toString().padStart(2, "0")}`;
  };

  const togglePlay = useCallback(() => {
    const el = audioRef.current;
    if (!el) return;
    if (el.paused) {
      el.play();
      setPlaying(true);
    } else {
      el.pause();
      setPlaying(false);
    }
  }, []);

  useEffect(() => {
    const el = audioRef.current;
    if (!el) return;
    const onTime = () => setCurrentTime(el.currentTime);
    const onEnded = () => { setPlaying(false); setCurrentTime(0); };
    el.addEventListener("timeupdate", onTime);
    el.addEventListener("ended", onEnded);
    return () => {
      el.removeEventListener("timeupdate", onTime);
      el.removeEventListener("ended", onEnded);
    };
  }, []);

  const handleSeek = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!duration || !progressRef.current || !audioRef.current) return;
    const rect = progressRef.current.getBoundingClientRect();
    const pct = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    audioRef.current.currentTime = pct * duration;
    setCurrentTime(audioRef.current.currentTime);
  };

  const pct = duration > 0 ? (currentTime / duration) * 100 : 0;

  return (
    <div className="mt-3 flex items-center gap-2">
      <audio ref={audioRef} src={src} preload="auto" />
      {/* Play / Pause */}
      <button
        onClick={togglePlay}
        className="w-8 h-8 rounded-full bg-slate-800 text-white flex items-center justify-center shrink-0 hover:bg-slate-700 transition-colors"
      >
        {playing ? (
          <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
            <rect x="6" y="4" width="4" height="16" rx="1" />
            <rect x="14" y="4" width="4" height="16" rx="1" />
          </svg>
        ) : (
          <svg className="w-3.5 h-3.5 ml-0.5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M8 5v14l11-7z" />
          </svg>
        )}
      </button>

      {/* Time */}
      <span className="text-xs text-slate-500 w-9 text-right tabular-nums shrink-0">
        {fmt(currentTime)}
      </span>

      {/* Progress bar */}
      <div
        ref={progressRef}
        className="flex-1 h-1.5 bg-slate-200 rounded-full cursor-pointer relative"
        onClick={handleSeek}
      >
        <div
          className="absolute inset-y-0 left-0 bg-slate-800 rounded-full transition-[width] duration-100"
          style={{ width: `${pct}%` }}
        />
      </div>

      {/* Duration */}
      <span className="text-xs text-slate-500 w-9 tabular-nums shrink-0">
        {duration > 0 ? fmt(duration) : "--:--"}
      </span>

      {/* Volume */}
      <button
        onClick={() => {
          const next = volume > 0 ? 0 : 1;
          setVolume(next);
          if (audioRef.current) audioRef.current.volume = next;
        }}
        className="text-slate-500 hover:text-slate-700 shrink-0"
      >
        {volume > 0 ? (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.536 8.464a5 5 0 010 7.072M17.95 6.05a8 8 0 010 11.9M11 5L6 9H2v6h4l5 4V5z" />
          </svg>
        ) : (
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707A1 1 0 0112 5v14a1 1 0 01-1.707.707L5.586 15z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2" />
          </svg>
        )}
      </button>
    </div>
  );
}

function ExpandableQuestionCard({
  index,
  question,
  response,
  isCompleted,
  audioUrl,
  durationSeconds,
  score,
  maxScore,
  feedback,
}: {
  index: number;
  question: string;
  response: string;
  isCompleted: boolean;
  audioUrl?: string | null;
  durationSeconds?: number | null;
  score?: number | null;
  maxScore?: number | null;
  feedback?: string | null;
}) {
  const [expanded, setExpanded] = useState(false);

  const scoreBadge =
    score != null && maxScore != null ? getQuestionScoreBadge(score, maxScore) : null;

  if (!isCompleted) {
    // Preview mode — just show the question
    return (
      <div className="bg-slate-50 border border-slate-200 rounded-lg p-3">
        <p className="text-sm text-slate-700">
          {index + 1}. {question}
        </p>
      </div>
    );
  }

  return (
    <div className="border border-slate-200 rounded-lg overflow-hidden">
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-start gap-3 p-3 text-left hover:bg-slate-50 transition-colors"
      >
        <span className="text-sm font-medium text-slate-500 shrink-0">{index + 1}.</span>
        <span className={`text-sm text-slate-700 flex-1 ${expanded ? "" : "line-clamp-2"}`}>
          {question}
        </span>
        {scoreBadge && (
          <span
            className={`flex-shrink-0 px-2.5 py-1 rounded-md border text-xs font-semibold ${scoreBadge.bg} ${scoreBadge.text}`}
          >
            {Number(score).toFixed(1)}/{Number(maxScore).toFixed(0)}
          </span>
        )}
        <svg
          className={`w-4 h-4 text-slate-400 shrink-0 mt-0.5 transition-transform ${expanded ? "rotate-180" : ""}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {expanded && (
        <div className="px-3 pb-3 border-t border-slate-100">
          {/* Transcript + audio in a gray box */}
          <div className="mt-3 bg-slate-50 rounded-lg p-3 border border-slate-200">
            {response && (
              <p className="text-sm text-slate-600 italic">{response}</p>
            )}
            {audioUrl ? (
              <AudioPlayer src={audioUrl} knownDuration={durationSeconds} />
            ) : (
              <p className="mt-3 text-xs text-slate-400 italic">No recording available</p>
            )}
            {durationSeconds != null && durationSeconds > 0 && (
              <p className="mt-2 text-xs text-slate-400">
                Duration: {Math.floor(durationSeconds / 60)}m {Math.floor(durationSeconds % 60)}s
              </p>
            )}
          </div>
          {/* Score Feedback */}
          {feedback && (
            <div className="mt-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
              <div className="text-xs font-medium text-slate-600 mb-1">Score Feedback</div>
              <p className="text-sm text-slate-700">{feedback}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ── Center content sections ────────────────────────────────────────────────────

function NotSentContent({ onLaunch }: { onLaunch: () => void }) {
  return (
    <div>
      <h3 className="text-base font-semibold text-slate-900 mb-4">AI Interview</h3>
      <button
        onClick={onLaunch}
        className="w-full border border-slate-200 rounded-lg p-4 hover:bg-slate-50 transition-colors text-left flex items-center gap-3"
      >
        <div className="w-10 h-10 rounded-lg bg-indigo-100 flex items-center justify-center shrink-0">
          <svg className="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
          </svg>
        </div>
        <div>
          <div className="text-sm font-semibold text-slate-900">Launch AI Interview</div>
          <div className="text-xs text-slate-500">Invite the candidate to complete an AI-led interview by web.</div>
        </div>
      </button>
    </div>
  );
}

function SendSchedulingLinkContent({
  applicant,
  onBack,
  onSent,
}: {
  applicant: ApplicantItem;
  onBack: () => void;
  onSent: (request: InterviewRequestItem) => void;
}) {
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [guides, setGuides] = useState<InterviewGuideItem[]>([]);
  const [selectedAgentId, setSelectedAgentId] = useState<string>("");
  const [selectedGuideId, setSelectedGuideId] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function loadData() {
      try {
        const [agentsData, guidesData] = await Promise.all([
          testAtsApi.get<AgentItem[]>("/api/v1/agents"),
          testAtsApi.get<InterviewGuideItem[]>("/api/v1/interview-guides"),
        ]);
        if (!cancelled) {
          setAgents(agentsData);
          setGuides(guidesData);
          if (agentsData.length > 0) setSelectedAgentId(agentsData[0].id);
          if (guidesData.length > 0) setSelectedGuideId(guidesData[0].id);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load agents and interview guides");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    loadData();
    return () => { cancelled = true; };
  }, []);

  const selectedAgent = agents.find((a) => a.id === selectedAgentId);
  const selectedGuide = guides.find((g) => g.id === selectedGuideId);

  const handleSend = async () => {
    if (!selectedAgentId || !selectedGuideId) return;
    setSending(true);
    setError(null);
    try {
      const data = await testAtsApi.post<InterviewRequestItem>(
        `/api/v1/applicants/${applicant.id}/interview`,
        { agentId: selectedAgentId, interviewGuideId: selectedGuideId }
      );
      onSent(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to send interview request");
    } finally {
      setSending(false);
    }
  };

  const canSend = selectedAgentId && selectedGuideId && agents.length > 0 && guides.length > 0;

  return (
    <div className="flex flex-col h-full">
      <h3 className="text-base font-semibold text-slate-900 mb-5">Send Scheduling Link</h3>

      <div className="flex-1">
          {loading ? (
            <div className="text-sm text-slate-500 mb-4">Loading agents and interview guides...</div>
          ) : agents.length === 0 || guides.length === 0 ? (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-4 text-amber-800 text-sm">
              {agents.length === 0 && "No agents found. "}
              {guides.length === 0 && "No interview guides found. "}
              Please create agents and interview guides in the Orchestrator first.
            </div>
          ) : (
            <>
              {/* Agent selector */}
              <div className="mb-5">
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Agent
                </label>
                <select
                  value={selectedAgentId}
                  onChange={(e) => setSelectedAgentId(e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                >
                  {agents.map((agent) => (
                    <option key={agent.id} value={agent.id}>
                      {agent.displayName}
                    </option>
                  ))}
                </select>
              </div>

              {/* Interview Guide selector */}
              <div className="mb-5">
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Interview Guide
                </label>
                <select
                  value={selectedGuideId}
                  onChange={(e) => setSelectedGuideId(e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-teal-500 focus:border-teal-500"
                >
                  {guides.map((guide) => (
                    <option key={guide.id} value={guide.id}>
                      {guide.name} {`\u2014 ${guide.questionCount} questions`}
                    </option>
                  ))}
                </select>

                {selectedGuide && (
                  <div className="mt-3 p-3 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-600 space-y-1">
                    {selectedGuide.description && <p>{selectedGuide.description}</p>}
                    <div className="flex items-center gap-3">
                      {selectedAgent && (
                        <span>
                          <span className="font-medium text-slate-700">Agent:</span>{" "}
                          {selectedAgent.displayName}
                        </span>
                      )}
                      <span>
                        <span className="font-medium text-slate-700">Questions:</span>{" "}
                        {selectedGuide.questionCount}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            </>
          )}

          {/* Placeholder: Template */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Template</label>
            <select
              disabled
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-400 bg-slate-50 cursor-not-allowed"
            >
              <option>Select message template</option>
            </select>
          </div>

          {/* Placeholder: Subject */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Subject</label>
            <input
              type="text"
              disabled
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-400 bg-slate-50 cursor-not-allowed"
            />
          </div>

          {/* Placeholder: Rich text editor */}
          <div className="mb-4">
            <div className="border border-slate-200 rounded-lg overflow-hidden">
              {/* Toolbar */}
              <div className="flex items-center gap-1 px-3 py-2 border-b border-slate-200 bg-slate-50">
                {["B", "I", "U", "B"].map((label, i) => (
                  <button
                    key={i}
                    disabled
                    className="w-7 h-7 flex items-center justify-center text-xs font-bold text-slate-400 rounded hover:bg-slate-100 cursor-not-allowed"
                  >
                    {label}
                  </button>
                ))}
                <div className="w-px h-5 bg-slate-200 mx-1" />
                {[
                  "M9 5l7 7-7 7",
                  "M4 6h16M4 12h16M4 18h16",
                  "M4 6h16M4 12h10M4 18h16",
                  "M4 6h16M4 12h16M4 18h7",
                ].map((d, i) => (
                  <button
                    key={i}
                    disabled
                    className="w-7 h-7 flex items-center justify-center text-slate-400 rounded hover:bg-slate-100 cursor-not-allowed"
                  >
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={d} />
                    </svg>
                  </button>
                ))}
                <div className="w-px h-5 bg-slate-200 mx-1" />
                <button
                  disabled
                  className="w-7 h-7 flex items-center justify-center text-xs font-bold text-slate-400 rounded hover:bg-slate-100 cursor-not-allowed"
                >
                  T
                </button>
              </div>
              {/* Body */}
              <div className="px-3 py-6 min-h-[120px]">
                <span className="text-sm text-slate-400">Placeholder</span>
              </div>
            </div>
          </div>

          {/* Placeholder: Attachments */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-slate-700 mb-1.5">Attachment(s)</label>
            <button
              disabled
              className="px-4 py-1.5 text-sm font-medium text-teal-600 border border-teal-300 rounded-lg cursor-not-allowed opacity-60"
            >
              Browse files...
            </button>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-3 mb-4 text-red-700 text-sm">
              {error}
            </div>
          )}
        </div>

      {/* Footer */}
      <div className="flex items-center justify-between pt-4 border-t border-slate-200 mt-auto">
          <button
            onClick={onBack}
            className="text-sm text-teal-600 hover:text-teal-800 font-medium flex items-center gap-1"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back
          </button>
          <button
            onClick={handleSend}
            disabled={sending || !canSend}
            className="px-5 py-2 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {sending ? "Sending..." : "Send Interview Request"}
          </button>
        </div>
    </div>
  );
}

function InterviewDetailsContent({
  ir,
  isCompleted,
  onResend,
}: {
  ir: InterviewRequestItem;
  isCompleted: boolean;
  onResend?: () => void;
}) {
  const [responses, setResponses] = useState<InterviewResponseData[]>([]);
  const [questionScores, setQuestionScores] = useState<Map<number, QuestionScoreData>>(new Map());
  const [loadingResponses, setLoadingResponses] = useState(false);
  const [urlCopied, setUrlCopied] = useState(false);

  // Fetch real interview responses and result (with question scores) in parallel
  useEffect(() => {
    if (!ir.orchestratorInterviewId) return;
    let cancelled = false;
    setLoadingResponses(true);

    const fetchResponses = fetch(`/api/interview-responses/${ir.orchestratorInterviewId}`)
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => {
        if (!cancelled && Array.isArray(data)) {
          const sorted = data
            .filter((r: InterviewResponseData) => !r.isFollowUp)
            .sort((a: InterviewResponseData, b: InterviewResponseData) => a.responseOrder - b.responseOrder);
          setResponses(sorted);
        }
      })
      .catch(() => {});

    const fetchResult = fetch(`/api/interview-results/${ir.orchestratorInterviewId}`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data: InterviewResultData | null) => {
        if (!cancelled && data?.questionScores) {
          const scoresMap = new Map<number, QuestionScoreData>();
          for (const qs of data.questionScores) {
            scoresMap.set(qs.questionIndex, qs);
          }
          setQuestionScores(scoresMap);
        }
      })
      .catch(() => {});

    Promise.all([fetchResponses, fetchResult]).finally(() => {
      if (!cancelled) setLoadingResponses(false);
    });

    return () => { cancelled = true; };
  }, [ir.orchestratorInterviewId]);

  const interviewDate = new Date(ir.createdAt).toLocaleDateString("en-US", {
    month: "2-digit",
    day: "2-digit",
    year: "numeric",
  });

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <h3 className="text-base font-semibold text-slate-900">AI Web Interview</h3>
        <InterviewStatusBadge status={ir.status} />
      </div>

      {/* Metadata grid */}
      <div className="border border-slate-200 rounded-lg p-4 mb-4">
        <div className="grid grid-cols-3 gap-4 text-sm">
          <div>
            <div className="text-slate-500 text-xs mb-0.5">Type</div>
            <div className="font-medium text-slate-900">Web</div>
          </div>
          <div>
            <div className="text-slate-500 text-xs mb-0.5">Date</div>
            <div className="font-medium text-slate-900">{interviewDate}</div>
          </div>
          <div>
            <div className="text-slate-500 text-xs mb-0.5">Interviewer</div>
            <div className="flex items-center gap-1.5">
              <div className="w-5 h-5 rounded-full bg-indigo-100 flex items-center justify-center">
                <svg className="w-3 h-3 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </div>
              <div>
                <div className="font-medium text-slate-900 text-xs">Jess</div>
                <div className="text-[10px] text-slate-500">AI Agent</div>
              </div>
            </div>
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4 text-sm mt-3">
          <div>
            <div className="text-slate-500 text-xs mb-0.5">Duration</div>
            <div className="font-medium text-slate-900">30 min</div>
          </div>
          <div>
            <div className="text-slate-500 text-xs mb-0.5">Time</div>
            <div className="font-medium text-slate-900">
              {new Date(ir.createdAt).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" })}
            </div>
          </div>
          <div className="flex items-center">
            <label className="flex items-center gap-1.5 text-xs text-slate-600">
              <input type="checkbox" defaultChecked className="rounded border-slate-300 text-teal-600" />
              Send reminder to candidate:
            </label>
          </div>
        </div>
        <div className="mt-1 ml-[calc(66.67%+0.5rem)]">
          <a href="#" className="text-xs text-teal-600 underline">30 min prior via email and text</a>
        </div>
      </div>

      {/* Invite URL — always visible when available */}
      {ir.inviteUrl && !isCompleted && (
        <div className="border border-slate-200 rounded-lg p-4 mb-4">
          <div className="flex items-center gap-2 mb-2">
            <svg className="w-4 h-4 text-teal-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
            </svg>
            <span className="text-sm font-medium text-slate-900">Candidate Interview Link</span>
          </div>
          <p className="text-xs text-slate-500 mb-2">
            Share this URL with the candidate to start the interview:
          </p>
          <div className="flex items-center gap-2">
            <input
              type="text"
              readOnly
              value={ir.inviteUrl}
              className="flex-1 text-xs bg-slate-50 border border-slate-200 rounded px-3 py-2 text-slate-700 font-mono"
            />
            <button
              onClick={() => {
                navigator.clipboard.writeText(ir.inviteUrl!);
                setUrlCopied(true);
                setTimeout(() => setUrlCopied(false), 2000);
              }}
              className="px-3 py-2 bg-teal-600 text-white text-xs font-medium rounded hover:bg-teal-700 transition-colors whitespace-nowrap"
            >
              {urlCopied ? "Copied!" : "Copy URL"}
            </button>
          </div>
        </div>
      )}

      {/* Completed: Summary + Score */}
      {isCompleted && ir.score != null && (
        <div className="border border-slate-200 rounded-lg p-4 mb-4">
          <div className="flex items-start gap-6">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <svg className="w-4 h-4 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <span className="text-sm font-semibold text-slate-900">Summary</span>
                {ir.resultRecommendation && (
                  <RecommendationBadge recommendation={ir.resultRecommendation} />
                )}
              </div>
              <p className="text-sm text-slate-600 leading-relaxed">
                {ir.resultSummary || "No summary available."}
                {ir.resultSummary && ir.resultSummary.length > 150 && (
                  <button className="text-teal-600 text-xs font-medium ml-1">Show more</button>
                )}
              </p>
            </div>
            <div className="shrink-0 w-36">
              <ScoreGauge score={ir.score} />
            </div>
          </div>
        </div>
      )}

      {/* Interview Questions */}
      <div className="mb-4">
        <h4 className="text-sm font-semibold text-slate-900 mb-3">
          {isCompleted ? "Interview Responses" : "Interview Questions Preview"}
        </h4>
        {loadingResponses ? (
          <div className="flex items-center justify-center py-6 text-sm text-slate-400">
            Loading responses…
          </div>
        ) : (
          <div className="space-y-2">
            {(responses.length > 0 ? responses : PLACEHOLDER_QUESTIONS).map(
              (item, i) => {
                const isReal = responses.length > 0;
                const realItem = item as InterviewResponseData;
                const placeholderItem = item as (typeof PLACEHOLDER_QUESTIONS)[0];
                const qScore = isReal ? questionScores.get(realItem.responseOrder) : undefined;

                return (
                  <ExpandableQuestionCard
                    key={isReal ? realItem.id : i}
                    index={i}
                    question={isReal ? realItem.questionText : placeholderItem.question}
                    response={
                      isReal
                        ? realItem.transcript || "No transcript available"
                        : placeholderItem.response
                    }
                    isCompleted={isCompleted}
                    audioUrl={isReal ? realItem.audioUrl : null}
                    durationSeconds={isReal ? realItem.durationSeconds : null}
                    score={qScore?.score}
                    maxScore={qScore?.maxScore}
                    feedback={qScore?.feedback}
                  />
                );
              }
            )}
          </div>
        )}
      </div>

      {/* Footer actions */}
      <div className="flex items-center justify-between pt-2 border-t border-slate-200">
        <button className="text-sm text-teal-600 hover:text-teal-800 font-medium flex items-center gap-1">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back
        </button>
        {ir.status === "link_expired" && onResend ? (
          <button
            onClick={onResend}
            className="px-4 py-1.5 text-sm font-medium text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors"
          >
            Resend Link
          </button>
        ) : (
          <button className="px-4 py-1.5 text-sm font-medium text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors">
            Reschedule
          </button>
        )}
      </div>
    </div>
  );
}

// ── Main panel ─────────────────────────────────────────────────────────────────

export function ApplicantSidePanel({
  applicant,
  interviewRequest,
  applicants,
  currentIndex,
  onNavigate,
  onClose,
  onInterviewSent,
  onRefreshInvite,
}: Props) {
  const [visible, setVisible] = useState(false);
  const [centerView, setCenterView] = useState<"default" | "send-form">("default");

  // Animate in on mount
  useEffect(() => {
    requestAnimationFrame(() => setVisible(true));
  }, []);

  // Reset view when switching applicants
  useEffect(() => {
    setCenterView("default");
  }, [applicant.id]);

  const handleClose = () => {
    setVisible(false);
    setTimeout(onClose, 300);
  };

  const goPrev = () => {
    if (currentIndex > 0) onNavigate(applicants[currentIndex - 1].id);
  };
  const goNext = () => {
    if (currentIndex < applicants.length - 1) onNavigate(applicants[currentIndex + 1].id);
  };

  const ir = interviewRequest;
  const isCompleted = ir?.status === "completed";
  const fullName = `${applicant.firstName} ${applicant.lastName}`;
  const appliedDate = new Date(applicant.createdAt).toLocaleDateString("en-US", {
    month: "numeric",
    day: "numeric",
    year: "2-digit",
  });

  // Determine applicant status label
  let statusLabel = "Awaiting Interview";
  if (ir?.status === "in_progress") statusLabel = "In Progress";
  else if (ir?.status === "completed") statusLabel = "Interview Complete";
  else if (ir?.status === "not_started") statusLabel = "Awaiting Interview";
  else if (ir?.status === "link_expired") statusLabel = "Link Expired";

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      {/* Backdrop */}
      <div
        className={`absolute inset-0 bg-black transition-opacity duration-300 ${visible ? "opacity-40" : "opacity-0"}`}
        onClick={handleClose}
      />

      {/* Panel */}
      <div
        className={`relative w-full max-w-[90vw] bg-white shadow-2xl flex flex-col transition-transform duration-300 ease-out ${
          visible ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {/* ── Top bar: navigation + close ── */}
        <div className="flex items-center justify-between px-6 py-3 border-b border-slate-200 bg-white shrink-0">
          <div className="flex items-center gap-3">
            <button
              onClick={goPrev}
              disabled={currentIndex === 0}
              className="w-7 h-7 rounded-full border border-slate-300 flex items-center justify-center disabled:opacity-30 hover:bg-slate-50 transition-colors"
            >
              <svg className="w-3.5 h-3.5 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <button
              onClick={goNext}
              disabled={currentIndex === applicants.length - 1}
              className="w-7 h-7 rounded-full border border-slate-300 flex items-center justify-center disabled:opacity-30 hover:bg-slate-50 transition-colors"
            >
              <svg className="w-3.5 h-3.5 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </button>
            <span className="text-sm text-slate-500">
              {currentIndex + 1} out of {applicants.length} applied
            </span>
          </div>
          <button
            onClick={handleClose}
            className="text-slate-400 hover:text-slate-600 text-xl leading-none"
          >
            &times;
          </button>
        </div>

        {/* ── Applicant header ── */}
        <div className="px-6 py-4 border-b border-slate-200 bg-white shrink-0">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Avatar firstName={applicant.firstName} lastName={applicant.lastName} />
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-lg font-bold text-slate-900">{fullName}</span>
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-600">
                    {statusLabel}
                  </span>
                </div>
                <div className="text-sm text-slate-500">
                  Applied on {appliedDate} via ZipRecruiter
                </div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button className="px-4 py-1.5 text-sm font-medium text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors">
                Decline
              </button>
              <button className="px-4 py-1.5 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors">
                Move to Pre-Hire
              </button>
              <button className="w-8 h-8 flex items-center justify-center border border-teal-700 bg-teal-700 rounded-lg text-white hover:bg-teal-800 transition-colors">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        {/* ── Pipeline breadcrumb ── */}
        <div className="px-6 py-3 border-b border-slate-200 bg-slate-50 shrink-0">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-slate-400">Applied</span>
            <svg className="w-3.5 h-3.5 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
            <span className="text-slate-400">Shortlisted</span>
            <svg className="w-3.5 h-3.5 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
            <span className="text-teal-700 font-semibold">In Progress</span>
            <svg className="w-3.5 h-3.5 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
            <span className="text-slate-400">Next Stage: Pre-Hire</span>
          </div>
        </div>

        {/* ── Three-column body ── */}
        <div className="flex-1 flex overflow-hidden">
          {/* Left: Hiring Steps */}
          <div className="w-48 border-r border-slate-200 bg-white py-4 px-4 shrink-0 overflow-y-auto">
            <div className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider mb-3">
              Hiring Steps
            </div>
            <div className="space-y-1">
              {/* AI Interview — active */}
              <button className="w-full flex items-center gap-2.5 px-2 py-2 rounded-lg bg-slate-100 text-left">
                <div className="w-5 h-5 rounded-full bg-teal-600 flex items-center justify-center shrink-0">
                  <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <span className="text-sm font-medium text-slate-900">AI Interview</span>
              </button>
              {/* Phone Screen */}
              <div className="flex items-center gap-2.5 px-2 py-2">
                <div className="w-5 h-5 rounded-full border-2 border-slate-300 shrink-0" />
                <span className="text-sm text-slate-500">Phone Screen</span>
              </div>
              {/* Interview */}
              <div className="flex items-center gap-2.5 px-2 py-2">
                <div className="w-5 h-5 rounded-full border-2 border-slate-300 shrink-0" />
                <span className="text-sm text-slate-500">Interview</span>
              </div>
              {/* Final Interview */}
              <div className="flex items-center gap-2.5 px-2 py-2">
                <div className="w-5 h-5 rounded-full border-2 border-slate-300 shrink-0" />
                <span className="text-sm text-slate-500">Final Interview</span>
              </div>
            </div>

            <div className="mt-6">
              <div className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider mb-3">
                Additional
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2.5 px-2 py-2 text-sm text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                  </svg>
                  Messages
                </div>
                <div className="flex items-center gap-2.5 px-2 py-2 text-sm text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  Documents
                </div>
                <div className="flex items-center gap-2.5 px-2 py-2 text-sm text-slate-500">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                  </svg>
                  Applications
                </div>
              </div>
            </div>
          </div>

          {/* Center: Interview content */}
          <div className="flex-1 overflow-y-auto p-6">
            {centerView === "send-form" ? (
              <SendSchedulingLinkContent
                applicant={applicant}
                onBack={() => setCenterView("default")}
                onSent={(request) => {
                  onInterviewSent(request);
                  setCenterView("default");
                }}
              />
            ) : !ir ? (
              <NotSentContent onLaunch={() => setCenterView("send-form")} />
            ) : (
              <InterviewDetailsContent
                ir={ir}
                isCompleted={!!isCompleted}
                onResend={ir.status === "link_expired" ? () => onRefreshInvite(ir) : undefined}
              />
            )}
          </div>

          {/* Right: Personal Info, Tags, Activity */}
          <div className="w-64 border-l border-slate-200 bg-white overflow-y-auto shrink-0">
            {/* Personal Info */}
            <div className="p-4 border-b border-slate-200">
              <div className="flex items-center justify-between mb-3">
                <h4 className="text-sm font-semibold text-slate-900">Personal Info</h4>
                <button className="text-xs text-teal-600 hover:text-teal-800 font-medium flex items-center gap-1">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                  </svg>
                  Edit Details
                </button>
              </div>
              <div className="space-y-2.5 text-sm">
                <div className="flex items-center gap-2 text-slate-600">
                  <svg className="w-4 h-4 text-slate-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                  </svg>
                  <span>{applicant.phone || "(123) 456-7890"}</span>
                </div>
                <div className="flex items-center gap-2 text-slate-600">
                  <svg className="w-4 h-4 text-slate-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                  <span className="truncate">{applicant.email}</span>
                </div>
                <div className="flex items-start gap-2 text-slate-600">
                  <svg className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  <span>123 Village Ln<br />Austin, TX 12345</span>
                </div>
              </div>
            </div>

            {/* Tags */}
            <div className="p-4 border-b border-slate-200">
              <h4 className="text-sm font-semibold text-slate-900 mb-3">Tags</h4>
              <div className="flex items-center gap-2 mb-2">
                <input
                  type="text"
                  placeholder="Add a tag"
                  className="flex-1 text-xs border border-slate-200 rounded px-2 py-1.5 text-slate-700 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-teal-500 focus:border-teal-500"
                />
                <button className="text-xs font-medium text-teal-600 border border-teal-300 rounded px-2 py-1.5 hover:bg-teal-50 transition-colors whitespace-nowrap">
                  + Add
                </button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {PLACEHOLDER_TAGS.map((tag) => (
                  <span
                    key={tag}
                    className="inline-flex items-center px-2 py-0.5 rounded border border-slate-200 bg-white text-xs text-slate-600"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>

            {/* Recent Activity */}
            <div className="p-4">
              <div className="flex items-center justify-between mb-3">
                <h4 className="text-sm font-semibold text-slate-900">Recent Activity</h4>
                <button className="text-xs text-teal-600 hover:text-teal-800 font-medium flex items-center gap-1">
                  <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
                  </svg>
                  Add a Comment
                </button>
              </div>
              <div className="space-y-4">
                {PLACEHOLDER_ACTIVITY.map((activity, i) => (
                  <div key={i} className="flex items-start gap-2.5">
                    <div className="w-6 h-6 rounded-full bg-slate-200 flex items-center justify-center shrink-0 mt-0.5">
                      {activity.icon === "pencil" ? (
                        <svg className="w-3 h-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                      ) : (
                        <svg className="w-3 h-3 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 21v-4m0 0V5a2 2 0 012-2h6.5l1 1H21l-3 6 3 6h-8.5l-1-1H5a2 2 0 00-2 2zm9-13.5V9" />
                        </svg>
                      )}
                    </div>
                    <div className="text-xs">
                      <p className="text-slate-700">
                        <span className="font-semibold">
                          {activity.useSelf ? fullName : activity.user}
                        </span>{" "}
                        {activity.action}
                      </p>
                      <p className="text-slate-400 mt-0.5">{activity.date}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
