import type { Metadata } from "next";
import { setRequestLocale } from "next-intl/server";
import { getLegalPage } from "@/lib/api/legal";
import { LegalArticle } from "@/components/legal/LegalArticle";

export const revalidate = 3600;

const SLUG = "aviso-legal";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const page = await getLegalPage(SLUG, locale);
  return { title: page?.title, description: page?.title };
}

export default async function LegalNoticePage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  return <LegalArticle slug={SLUG} locale={locale} />;
}
