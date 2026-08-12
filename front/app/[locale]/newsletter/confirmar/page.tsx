import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthShell } from "@/components/auth/AuthShell";
import { TokenActionClient } from "@/components/newsletter/TokenActionClient";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Newsletter" });
  return { title: t("confirmMetaTitle"), robots: { index: false } };
}

export default async function NewsletterConfirmPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ token?: string; email?: string }>;
}) {
  const { locale } = await params;
  const { token, email } = await searchParams;
  setRequestLocale(locale);
  const t = await getTranslations("Newsletter");

  return (
    <AuthShell title={t("confirmTitle")}>
      <TokenActionClient
        endpoint="/api/newsletter/confirm"
        token={token ?? ""}
        email={email ?? ""}
        labels={{
          loading: t("confirming"),
          success: t("confirmSuccess"),
          error: t("confirmError"),
          missing: t("missingParams"),
        }}
      />
    </AuthShell>
  );
}
