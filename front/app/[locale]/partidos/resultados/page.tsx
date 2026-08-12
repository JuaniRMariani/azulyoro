import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getMatches } from "@/lib/api/sports";
import { FixtureList } from "@/components/sports/FixtureList";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";
import { groupByDay } from "@/lib/matches";

export const revalidate = 300;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Matches" });
  return { title: t("resultsMetaTitle"), description: t("resultsMetaDescription") };
}

export default async function ResultsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Matches");
  const tc = await getTranslations("Common");

  const finished = await getMatches({ status: "finished", pageSize: 50 });
  const items = finished?.items ?? [];
  const groups = groupByDay(items, locale, "desc");

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-8 px-4 py-10">
      <Breadcrumbs
        items={[
          { label: tc("home"), href: "/" },
          { label: t("title"), href: "/partidos" },
          { label: t("resultsTitle") },
        ]}
      />

      <header className="flex flex-col gap-2">
        <h1 className="font-display text-3xl font-bold tracking-tight">
          {t("resultsTitle")}
        </h1>
        <p className="text-[var(--muted-foreground)]">{t("resultsDescription")}</p>
      </header>

      {groups.length > 0 ? (
        <FixtureList groups={groups} locale={locale} />
      ) : (
        <EmptyState title={t("resultsEmpty")} />
      )}
    </main>
  );
}
