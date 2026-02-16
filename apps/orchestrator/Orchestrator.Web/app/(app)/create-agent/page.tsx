"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ImageUpload } from "@/components/image-upload";
import { Loader2, Building2 } from "lucide-react";
import Link from "next/link";
import { createAgent } from "./actions";
import { uploadImage } from "@/lib/upload-image";
import { useServerAction } from "@/lib/use-server-action";

function getCookie(name: string): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

export default function CreateAgent() {
  const { user, isLoading } = useUser();
  const router = useRouter();
  const [profileImageUrl, setProfileImageUrl] = useState<string>("");
  const [visibilityScope, setVisibilityScope] = useState<string>("organization_only");
  
  const selectedOrgId = getCookie("orchestrator_selected_org");
  const hasOrgSelected = !!selectedOrgId;

  const { execute, isLoading: isSubmitting, error } = useServerAction(createAgent, {
    successMessage: "Agent created successfully!",
  });

  useEffect(() => {
    if (!isLoading && !user) {
      router.push("/login");
    }
  }, [user, isLoading, router]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const formData = new FormData(e.currentTarget);
    if (profileImageUrl) {
      formData.set("profileImageUrl", profileImageUrl);
    }
    formData.set("visibilityScope", visibilityScope);

    await execute(formData);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) {
    return null;
  }

  if (!hasOrgSelected) {
    return (
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-2xl font-semibold mb-6">Create Agent</h1>
        <div className="text-center py-20">
          <Building2 className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
          <h3 className="text-xl font-semibold mb-2">Select an organization</h3>
          <p className="text-muted-foreground max-w-md mx-auto">
            Please select an organization from the dropdown in the top-right corner
            before creating an agent. Agents must belong to a specific organization.
          </p>
        </div>
      </div>
    );
  }

  return (
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-2xl font-semibold mb-6">Create Agent</h1>
        <div className="max-w-2xl mx-auto space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Profile Information</CardTitle>
              <CardDescription>
                Set up the basic information for this agent
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                  <div className="p-4 bg-destructive/10 text-destructive rounded-lg text-sm">
                    {error}
                  </div>
                )}

                {/* Display Name (Required) */}
                <div className="space-y-2">
                  <Label htmlFor="displayName">
                    Display Name <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="displayName"
                    name="displayName"
                    placeholder="e.g., Alex, Jordan, or Sam"
                    required
                    disabled={isSubmitting}
                  />
                  <p className="text-sm text-muted-foreground">
                    This is how the agent will be identified. Must be unique.
                  </p>
                </div>

                {/* Profile Image Upload */}
                <div className="space-y-2">
                  <Label>Profile Image</Label>
                  <ImageUpload
                    value={profileImageUrl}
                    onChange={setProfileImageUrl}
                    disabled={isSubmitting}
                    uploadAction={uploadImage}
                  />
                  <p className="text-sm text-muted-foreground">
                    Optional. Upload an image or leave blank.
                  </p>
                  <input 
                    type="hidden" 
                    name="profileImageUrl" 
                    value={profileImageUrl} 
                  />
                </div>

                {/* Visibility Scope */}
                <div className="space-y-3">
                  <Label>Visibility</Label>
                  <p className="text-sm text-muted-foreground">
                    Control which organizations can see and use this agent.
                  </p>
                  <div className="space-y-2">
                    <label className="flex items-start gap-3 p-3 rounded-lg border cursor-pointer hover:bg-muted/50 transition-colors">
                      <input
                        type="radio"
                        name="visibilityScopeRadio"
                        value="organization_only"
                        checked={visibilityScope === "organization_only"}
                        onChange={(e) => setVisibilityScope(e.target.value)}
                        className="mt-0.5"
                        disabled={isSubmitting}
                      />
                      <div>
                        <div className="font-medium text-sm">This organization only</div>
                        <div className="text-xs text-muted-foreground">
                          Only visible at the current organization.
                        </div>
                      </div>
                    </label>
                    <label className="flex items-start gap-3 p-3 rounded-lg border cursor-pointer hover:bg-muted/50 transition-colors">
                      <input
                        type="radio"
                        name="visibilityScopeRadio"
                        value="organization_and_descendants"
                        checked={visibilityScope === "organization_and_descendants"}
                        onChange={(e) => setVisibilityScope(e.target.value)}
                        className="mt-0.5"
                        disabled={isSubmitting}
                      />
                      <div>
                        <div className="font-medium text-sm">This organization and sub-organizations</div>
                        <div className="text-xs text-muted-foreground">
                          Visible here and at all sub-organizations below.
                        </div>
                      </div>
                    </label>
                    <label className="flex items-start gap-3 p-3 rounded-lg border cursor-pointer hover:bg-muted/50 transition-colors">
                      <input
                        type="radio"
                        name="visibilityScopeRadio"
                        value="descendants_only"
                        checked={visibilityScope === "descendants_only"}
                        onChange={(e) => setVisibilityScope(e.target.value)}
                        className="mt-0.5"
                        disabled={isSubmitting}
                      />
                      <div>
                        <div className="font-medium text-sm">Sub-organizations only</div>
                        <div className="text-xs text-muted-foreground">
                          Only visible at sub-organizations, not at this organization.
                        </div>
                      </div>
                    </label>
                  </div>
                </div>

                {/* Save Button */}
                <div className="flex justify-end gap-4">
                  <Link href="/my-agents">
                    <Button type="button" variant="outline" disabled={isSubmitting}>
                      Cancel
                    </Button>
                  </Link>
                  <Button type="submit" className="w-full sm:w-auto" disabled={isSubmitting}>
                    {isSubmitting ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Creating...
                      </>
                    ) : (
                      "Create Agent"
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
  );
}
