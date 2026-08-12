import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getStandings } from "@/lib/api/sports";
import type { StandingDto } from "@/lib/api/types";
import { StandingsTable } from "@/components/sports/StandingsTable";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";

export const revalidate = 3600;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Standings" });
  return { title: t("metaTitle"), description: t("metaDescription") };
}

export default async function StandingsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Standings");
  const tc = await getTranslations("Common");

  const standings = await getStandings();
  const rows = standings ?? [];

  // Split into groups (a group per competition/table) preserving API order.
  const byGroup = new Map<string, StandingDto[]>();
  for (const r of rows) {
    const bucket = byGroup.get(r.groupName) ?? [];
    bucket.push(r);
    byGroup.set(r.groupName, bucket);
  }
  const groups = [...byGroup.entries()];

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-8 px-4 py-10">
      <Breadcrumbs
        items={[{ label: tc("home"), href: "/" }, { label: t("title") }]}
      />

      <header className="flex flex-col gap-2">
        <h1 className="font-display text-3xl font-bold tracking-tight">
          {t("title")}
        </h1>
        <p className="text-[var(--muted-foreground)]">{t("description")}</p>
      </header>

      {groups.length > 0 ? (
        <div className="flex flex-col gap-8">
          {groups.map(([groupName, groupRows]) => (
            <section key={groupName}>
              {groups.length > 1 && (
                <h2 className="mb-3 font-display text-sm font-semibold uppercase tracking-wide text-[var(--accent)]">
                  {groupName}
                </h2>
              )}
              <StandingsTable rows={groupRows} />
            </section>
          ))}
        </div>
      ) : (
        <EmptyState title={t("empty")} />
      )}
    </main>
  );
}
