"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2, RotateCcw, Save, AlertTriangle } from "lucide-react";
import {
  getRecommendationThresholds,
  updateRecommendationThresholds,
  RecommendationThresholds,
} from "./actions";

const DEFAULTS = {
  stronglyRecommendMin: 80,
  recommendMin: 65,
  considerMin: 50,
  doNotRecommendMin: 0,
};

const TIER_CONFIG = [
  { key: "stronglyRecommendMin" as const, label: "Strongly Recommend", color: "text-emerald-700", bg: "bg-emerald-50 border-emerald-200" },
  { key: "recommendMin" as const, label: "Recommend", color: "text-green-700", bg: "bg-green-50 border-green-200" },
  { key: "considerMin" as const, label: "Consider", color: "text-amber-700", bg: "bg-amber-50 border-amber-200" },
  { key: "doNotRecommendMin" as const, label: "Do Not Recommend", color: "text-red-700", bg: "bg-red-50 border-red-200" },
];

export default function RecommendationThresholdsPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [thresholds, setThresholds] = useState<RecommendationThresholds | null>(null);
  const [form, setForm] = useState(DEFAULTS);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user) loadThresholds();
  }, [user]);

  const loadThresholds = async () => {
    try {
      setIsLoading(true);
      const data = await getRecommendationThresholds();
      setThresholds(data);
      setForm({
        stronglyRecommendMin: data.stronglyRecommendMin,
        recommendMin: data.recommendMin,
        considerMin: data.considerMin,
        doNotRecommendMin: data.doNotRecommendMin,
      });
    } catch {
      setError("Failed to load thresholds.");
    } finally {
      setIsLoading(false);
    }
  };

  const validate = (values: typeof form): string | null => {
    if (values.stronglyRecommendMin <= values.recommendMin) {
      return "\"Strongly Recommend\" must be greater than \"Recommend\".";
    }
    if (values.recommendMin <= values.considerMin) {
      return "\"Recommend\" must be greater than \"Consider\".";
    }
    if (values.considerMin < 0 || values.stronglyRecommendMin > 100) {
      return "Thresholds must be between 0 and 100.";
    }
    return null;
  };

  const handleChange = (key: keyof typeof form, value: string) => {
    const num = parseInt(value, 10);
    if (isNaN(num)) return;
    const next = { ...form, [key]: num };
    setForm(next);
    setValidationError(validate(next));
    setSuccessMessage(null);
  };

  const handleSave = async () => {
    const err = validate(form);
    if (err) {
      setValidationError(err);
      return;
    }
    try {
      setIsSaving(true);
      setError(null);
      setSuccessMessage(null);
      const updated = await updateRecommendationThresholds(form);
      setThresholds(updated);
      setSuccessMessage("Thresholds saved. Changes apply to all future interviews.");
    } catch {
      setError("Failed to save thresholds.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleReset = () => {
    setForm(DEFAULTS);
    setValidationError(null);
    setSuccessMessage(null);
  };

  if (isUserLoading || isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Recommendation Thresholds</h1>
        <p className="text-muted-foreground mt-1">
          Set the minimum score required for each recommendation tier. Scores are on a 0–100 scale.
        </p>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
          {error}
        </div>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Tier Thresholds</CardTitle>
          <CardDescription>
            A candidate&apos;s overall score (0–100) is compared against these thresholds to determine their recommendation tier.
            Thresholds must be strictly descending.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {TIER_CONFIG.map((tier) => (
            <div key={tier.key} className="flex items-center gap-4">
              <div className={`w-48 px-3 py-2 rounded-md border text-sm font-medium ${tier.bg} ${tier.color}`}>
                {tier.label}
              </div>
              <div className="flex items-center gap-2 flex-1">
                <Label htmlFor={tier.key} className="text-sm text-muted-foreground whitespace-nowrap">
                  Min score:
                </Label>
                <Input
                  id={tier.key}
                  type="number"
                  min={0}
                  max={100}
                  value={form[tier.key]}
                  onChange={(e) => handleChange(tier.key, e.target.value)}
                  className="w-24"
                  disabled={tier.key === "doNotRecommendMin"}
                />
              </div>
            </div>
          ))}

          {validationError && (
            <div className="flex items-start gap-2 p-3 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-700">
              <AlertTriangle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{validationError}</span>
            </div>
          )}

          {successMessage && (
            <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-700">
              {successMessage}
            </div>
          )}

          <div className="flex items-center justify-between pt-2">
            <Button variant="outline" size="sm" onClick={handleReset}>
              <RotateCcw className="h-4 w-4 mr-2" />
              Reset to Defaults
            </Button>

            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <AlertTriangle className="h-3.5 w-3.5" />
                Changes apply to all future interviews
              </div>
              <Button onClick={handleSave} disabled={isSaving || !!validationError} size="sm">
                {isSaving ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Save className="h-4 w-4 mr-2" />}
                Save
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
