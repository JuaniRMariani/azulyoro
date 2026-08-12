import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthShell } from "@/components/auth/AuthShell";
import { NewsletterForm } from "@/components/newsletter/NewsletterForm";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Newsletter" });
  return { title: t("metaTitle"), description: t("description") };
}

export default async function NewsletterPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Newsletter");

  return (
    <AuthShell title={t("title")} description={t("description")}>
      <NewsletterForm />
    </AuthShell>
  );
}
