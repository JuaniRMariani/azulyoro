import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthShell } from "@/components/auth/AuthShell";
import { VerifyEmailClient } from "@/components/auth/VerifyEmailClient";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Auth" });
  return { title: t("verifyMetaTitle"), robots: { index: false } };
}

export default async function VerifyEmailPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ token?: string; email?: string }>;
}) {
  const { locale } = await params;
  const { token, email } = await searchParams;
  setRequestLocale(locale);
  const t = await getTranslations("Auth");

  return (
    <AuthShell title={t("verifyTitle")}>
      <VerifyEmailClient token={token ?? ""} email={email ?? ""} />
    </AuthShell>
  );
}
