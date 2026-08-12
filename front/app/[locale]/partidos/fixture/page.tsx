import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getMatches } from "@/lib/api/sports";
import { FixtureList } from "@/components/sports/FixtureList";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";
import { groupByCompetition } from "@/lib/matches";

export const revalidate = 300;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Matches" });
  return { title: t("fixtureMetaTitle"), description: t("fixtureMetaDescription") };
}

export default async function FixturePage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Matches");
  const tc = await getTranslations("Common");

  const all = await getMatches({ pageSize: 50 });
  const items = (all?.items ?? [])
    .slice()
    .sort((a, b) => +new Date(a.dateUtc) - +new Date(b.dateUtc));
  const groups = groupByCompetition(items, t("title"));

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-8 px-4 py-10">
      <Breadcrumbs
        items={[
          { label: tc("home"), href: "/" },
          { label: t("title"), href: "/partidos" },
          { label: t("fixtureTitle") },
        ]}
      />

      <header className="flex flex-col gap-2">
        <h1 className="font-display text-3xl font-bold tracking-tight">
          {t("fixtureTitle")}
        </h1>
        <p className="text-[var(--muted-foreground)]">{t("fixtureDescription")}</p>
      </header>

      {groups.length > 0 ? (
        <FixtureList groups={groups} locale={locale} />
      ) : (
        <EmptyState title={t("fixtureEmpty")} />
      )}
    </main>
  );
}
