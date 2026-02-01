// Force dynamic rendering for this route segment to fix Next.js 15 cookies() issue with Auth0
export const dynamic = 'force-dynamic';

export default function CreatePersonaLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
}

