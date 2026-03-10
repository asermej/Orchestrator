"use client";

import { useEffect } from "react";
import { useRouter, useParams } from "next/navigation";

export default function QuestionPackagesRoleRedirectPage() {
  const router = useRouter();
  const params = useParams();
  const roleKey = params.roleKey as string;

  useEffect(() => {
    if (roleKey) router.replace(`/interview-content?role=${encodeURIComponent(roleKey)}`);
  }, [router, roleKey]);
  return null;
}
