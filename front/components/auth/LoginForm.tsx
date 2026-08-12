"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { Field } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { FormMessage } from "@/components/ui/FormMessage";

export function LoginForm() {
  const t = useTranslations("Auth");
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setPending(true);
    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (res.ok) {
        router.push("/perfil");
        router.refresh();
        return;
      }
      setError(res.status === 401 ? t("loginError") : t("genericError"));
    } catch {
      setError(t("genericError"));
    } finally {
      setPending(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
      {error && <FormMessage variant="error">{error}</FormMessage>}
      <Field
        id="email"
        type="email"
        label={t("email")}
        required
        autoComplete="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <Field
        id="password"
        type="password"
        label={t("password")}
        required
        autoComplete="current-password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      <Button type="submit" pending={pending}>
        {pending ? t("loggingIn") : t("submitLogin")}
      </Button>
    </form>
  );
}
