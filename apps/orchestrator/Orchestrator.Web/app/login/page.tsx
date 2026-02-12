"use client";

import { useUser } from "@auth0/nextjs-auth0/client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { LogIn, UserPlus, Loader2, AlertCircle } from "lucide-react";
import Link from "next/link";

// Check if Auth0 is configured (consistent across server/client)
const isAuth0Configured =
  !!process.env.NEXT_PUBLIC_AUTH0_CLIENT_ID &&
  process.env.NEXT_PUBLIC_AUTH0_CLIENT_ID !== "your-client-id";

export default function LoginPage() {
  const router = useRouter();
  const [isClient, setIsClient] = useState(false);

  // Always call useUser hook (Rules of Hooks requirement)
  const auth0Data = useUser();

  // Ensure client-side rendering for Auth0 hooks
  useEffect(() => {
    setIsClient(true);
  }, []);

  // Only use Auth0 data if properly configured and client-side
  const user =
    isClient && isAuth0Configured && auth0Data ? auth0Data.user : null;
  const isLoading =
    isClient && isAuth0Configured && auth0Data ? auth0Data.isLoading : false;

  useEffect(() => {
    if (user) {
      router.push("/");
    }
  }, [user, router]);

  // Show loading only after client hydration to prevent mismatch
  if (isClient && isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (user) {
    return null; // Will redirect
  }

  // Show configuration message if Auth0 is not set up
  if (!isAuth0Configured) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center p-4">
        <div className="w-full max-w-md">
          <Card>
            <CardHeader className="text-center">
              <AlertCircle className="mx-auto h-12 w-12 text-yellow-500 mb-4" />
              <CardTitle className="text-2xl">
                Authentication Setup Required
              </CardTitle>
              <CardDescription>
                Auth0 configuration is required to enable authentication
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 text-center">
              <p className="text-sm text-muted-foreground">
                Please configure Auth0 environment variables to enable login
                functionality.
              </p>
              <Link href="/" className="w-full">
                <Button variant="outline" className="w-full">
                  Return to Homepage
                </Button>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold mb-2">Orchestrator</h1>
          <p className="text-muted-foreground">
            Create and discover AI agents
          </p>
        </div>

        {/* Login Card */}
        <Card>
          <CardHeader className="text-center">
            <CardTitle className="text-2xl">Welcome</CardTitle>
            <CardDescription>
              Sign in to your account or create a new one
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Login Button */}
            <a href="/api/auth/login" className="w-full">
              <Button className="w-full" size="lg">
                <LogIn className="mr-2 h-5 w-5" />
                Sign In
              </Button>
            </a>

            {/* Signup Button */}
            <a href="/api/auth/signup" className="w-full">
              <Button variant="outline" className="w-full" size="lg">
                <UserPlus className="mr-2 h-5 w-5" />
                Create Account
              </Button>
            </a>

            <div className="text-center text-sm text-muted-foreground">
              By continuing, you agree to our Terms of Service and Privacy
              Policy
            </div>
          </CardContent>
        </Card>

        {/* Footer */}
        <div className="text-center mt-8 text-sm text-muted-foreground">
          <p>Secure authentication powered by Auth0</p>
        </div>
      </div>
    </div>
  );
}
