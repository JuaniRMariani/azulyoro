"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { Button } from "@/components/ui/Button";

export function LogoutButton() {
  const t = useTranslations("Profile");
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function onClick() {
    setPending(true);
    try {
      await fetch("/api/auth/logout", { method: "POST" });
      router.push("/ingresar");
      router.refresh();
    } finally {
      setPending(false);
    }
  }

  return (
    <Button
      type="button"
      onClick={onClick}
      pending={pending}
      className="bg-[var(--muted)] text-[var(--foreground)]"
    >
      {pending ? t("loggingOut") : t("logout")}
    </Button>
  );
}
