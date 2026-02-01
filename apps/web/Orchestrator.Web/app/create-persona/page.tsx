"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Header } from "@/components/header";
import { ImageUpload } from "@/components/image-upload";
import { ArrowLeft, Loader2 } from "lucide-react";
import Link from "next/link";
import { createPersona } from "./actions";
import { uploadImage } from "@/lib/upload-image";
import { useServerAction } from "@/lib/use-server-action";

export default function CreatePersona() {
  const { user, isLoading } = useUser();
  const router = useRouter();
  const [profileImageUrl, setProfileImageUrl] = useState<string>("");
  
  // Use the new useServerAction hook for better error handling
  // Note: No onSuccess callback - the server action handles the redirect to edit page with onboarding flag
  const { execute, isLoading: isSubmitting, error } = useServerAction(createPersona, {
    successMessage: "Persona created successfully!",
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
    return null; // Will redirect to login
  }

  return (
    <div className="min-h-screen bg-background">
      <Header />

      {/* Breadcrumb Header */}
      <div className="border-b bg-muted/20">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-4">
            <Link href="/">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <h1 className="text-2xl font-semibold">Create Persona</h1>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto space-y-6">
          {/* Onboarding Progress */}
          <Card className="border-2 border-primary">
            <CardContent className="p-6">
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-semibold">
                    1
                  </div>
                  <span className="font-medium">Profile</span>
                </div>
                <div className="flex-1 h-px bg-border" />
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-full bg-muted text-muted-foreground flex items-center justify-center font-semibold">
                    2
                  </div>
                  <span className="text-muted-foreground">General Training</span>
                </div>
                <div className="flex-1 h-px bg-border" />
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-full bg-muted text-muted-foreground flex items-center justify-center font-semibold">
                    3
                  </div>
                  <span className="text-muted-foreground">Topics</span>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Profile Information</CardTitle>
              <CardDescription>
                Update the basic information for this persona
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
                    placeholder="e.g., Yoda, John Smith, or Madonna"
                    required
                    disabled={isSubmitting}
                  />
                  <p className="text-sm text-muted-foreground">
                    This is how the persona will be identified. Must be unique.
                  </p>
                </div>

                {/* First Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input
                    id="firstName"
                    name="firstName"
                    placeholder="Optional - e.g., John"
                    disabled={isSubmitting}
                  />
                  <p className="text-sm text-muted-foreground">
                    Optional. Leave blank for single-name personas.
                  </p>
                </div>

                {/* Last Name (Optional) */}
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input
                    id="lastName"
                    name="lastName"
                    placeholder="Optional - e.g., Smith"
                    disabled={isSubmitting}
                  />
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

                {/* Save Button */}
                <div className="flex justify-end gap-4">
                  <Link href="/">
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
                      "Create Persona"
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
