"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ImageUpload } from "@/components/image-upload";
import { Loader2 } from "lucide-react";
import Link from "next/link";
import { fetchAgentById, updateAgent } from "./actions";
import { AgentItem } from "@/app/(app)/my-agents/actions";
import { uploadImage } from "@/lib/upload-image";
import { useServerAction } from "@/lib/use-server-action";

export default function EditAgent() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const agentId = params.id as string;

  const [agent, setAgent] = useState<AgentItem | null>(null);
  const [isLoadingAgent, setIsLoadingAgent] = useState(true);
  const [profileImageUrl, setProfileImageUrl] = useState<string>("");

  const { execute: executeProfileUpdate, isLoading: isSubmitting } = useServerAction(
    async (formData: FormData) => {
      if (profileImageUrl) {
        formData.set("profileImageUrl", profileImageUrl);
      }
      await updateAgent(agentId, formData);
    },
    {
      successMessage: "Agent profile updated successfully!",
      onSuccess: () => router.push("/my-agents"),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && agentId) {
      loadAgent();
    }
  }, [user, agentId]);

  const loadAgent = async () => {
    try {
      setIsLoadingAgent(true);
      const data = await fetchAgentById(agentId);
      setAgent(data);
      setProfileImageUrl(data.profileImageUrl || "");
    } catch (err) {
      console.error("Error loading agent:", err);
    } finally {
      setIsLoadingAgent(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    await executeProfileUpdate(formData);
  };

  if (isUserLoading || isLoadingAgent) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user || !agent) {
    return null;
  }

  return (
    <main className="container mx-auto px-4 py-8">
      <div className="max-w-2xl mx-auto">
        <Card>
          <CardHeader>
            <CardTitle>Edit {agent.displayName}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="displayName">
                  Display Name <span className="text-red-500">*</span>
                </Label>
                <Input
                  id="displayName"
                  name="displayName"
                  placeholder="e.g., Alex, Jordan, or Sam"
                  defaultValue={agent.displayName}
                  required
                  disabled={isSubmitting}
                />
                <p className="text-sm text-muted-foreground">
                  This is how the agent will be identified. Must be unique.
                </p>
              </div>

              <div className="space-y-2">
                <Label>Profile Image</Label>
                <ImageUpload
                  value={profileImageUrl}
                  onChange={setProfileImageUrl}
                  onRemove={() => setProfileImageUrl("")}
                  disabled={isSubmitting}
                  uploadAction={uploadImage}
                />
                <p className="text-sm text-muted-foreground">
                  Optional. Upload a new image to replace the current one, or remove it.
                </p>
                <input
                  type="hidden"
                  name="profileImageUrl"
                  value={profileImageUrl}
                />
              </div>

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
                      Saving...
                    </>
                  ) : (
                    "Save Changes"
                  )}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </main>
  );
}
