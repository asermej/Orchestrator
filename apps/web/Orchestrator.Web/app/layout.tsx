import type { Metadata } from "next";
import { Auth0Provider } from '@auth0/nextjs-auth0/client';
import { UserProvisioner } from '@/components/user-provisioner';
import { Toaster } from '@/components/ui/sonner';
import "./globals.css";

export const metadata: Metadata = {
  title: "Orchestrator - AI Interviewer",
  description: "AI-powered interview and recruiting platform",
  generator: "Orchestrator",
};

// Force dynamic rendering since we need session/cookie access
export const dynamic = 'force-dynamic';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <Auth0Provider>
          <UserProvisioner />
          {children}
          <Toaster />
        </Auth0Provider>
      </body>
    </html>
  );
}
