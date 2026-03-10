import { redirect } from "next/navigation";

export default async function InterviewQuestionsRoleRedirect({
  params,
}: {
  params: Promise<{ roleKey: string }>;
}) {
  const { roleKey } = await params;
  redirect(`/interview-content?role=${encodeURIComponent(roleKey)}`);
}
