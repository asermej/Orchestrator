"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function QuestionPackagesRedirectPage() {
  const router = useRouter();
  useEffect(() => {
    router.replace("/competency-frameworks");
  }, [router]);
  return null;
}
