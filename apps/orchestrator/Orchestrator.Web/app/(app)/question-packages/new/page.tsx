"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function QuestionPackagesNewRedirectPage() {
  const router = useRouter();
  useEffect(() => {
    router.replace("/competency-frameworks/new");
  }, [router]);
  return null;
}
