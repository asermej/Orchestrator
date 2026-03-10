import { redirect } from "next/navigation";

export default async function CompetencyFrameworksRoleEditRedirect({
  params,
}: {
  params: Promise<{ roleKey: string }>;
}) {
  const { roleKey } = await params;
  redirect(`/interview-content?role=${encodeURIComponent(roleKey)}&view=role`);
}
