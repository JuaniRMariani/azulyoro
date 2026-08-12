import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { getMatches } from "@/lib/api/sports";
import { MatchCard } from "@/components/sports/MatchCard";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";

export const revalidate = 300;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Matches" });
  return { title: t("metaTitle"), description: t("metaDescription") };
}

export default async function MatchesPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Matches");
  const tc = await getTranslations("Common");

  const upcoming = await getMatches({ status: "upcoming", pageSize: 20 });
  const matches = upcoming?.items ?? [];

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
        <div className="mt-1 flex flex-wrap gap-3 text-sm font-medium">
          <Link
            href="/partidos/resultados"
            className="rounded-full border border-[var(--border)] px-3.5 py-1.5 transition-colors hover:border-[var(--accent)]"
          >
            {t("seeResults")}
          </Link>
          <Link
            href="/partidos/fixture"
            className="rounded-full border border-[var(--border)] px-3.5 py-1.5 transition-colors hover:border-[var(--accent)]"
          >
            {t("seeFixture")}
          </Link>
        </div>
      </header>

      <section>
        <h2 className="mb-3 font-display text-xl font-semibold">{t("upcoming")}</h2>
        {matches.length > 0 ? (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {matches.map((m) => (
              <MatchCard key={m.id} match={m} locale={locale} />
            ))}
          </div>
        ) : (
          <EmptyState title={t("upcomingEmpty")} />
        )}
      </section>
    </main>
  );
}
