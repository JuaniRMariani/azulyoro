import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { getNextMatch, getMatches } from "@/lib/api/sports";
import { MatchCard } from "@/components/sports/MatchCard";
import { EmptyState } from "@/components/ui/EmptyState";
import { siteUrl } from "@/lib/site";

export const revalidate = 60;

const teamJsonLd = {
  "@context": "https://schema.org",
  "@type": "SportsTeam",
  name: "Boca Juniors",
  sport: "Soccer",
  url: siteUrl,
};

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Home" });
  return { title: t("metaTitle"), description: t("metaDescription") };
}

export default async function Home({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Home");

  const [next, results] = await Promise.all([
    getNextMatch(),
    getMatches({ status: "finished", pageSize: 4 }),
  ]);
  const latest = results?.items ?? [];

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-12 px-4 py-10">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(teamJsonLd) }}
      />
      <section className="text-center">
        <h1 className="font-display text-4xl font-bold tracking-tight sm:text-5xl">
          {t("title")}
        </h1>
        <p className="mt-3 text-lg text-[var(--muted-foreground)]">{t("tagline")}</p>
      </section>

      <section>
        <div className="mb-3 flex items-center justify-between gap-2">
          <h2 className="font-display text-xl font-semibold">{t("nextMatch")}</h2>
          <Link
            href="/partidos"
            className="text-sm font-medium text-[var(--accent)] hover:underline"
          >
            {t("viewAllMatches")}
          </Link>
        </div>
        {next ? (
          <div className="max-w-md">
            <MatchCard match={next} locale={locale} linked />
          </div>
        ) : (
          <EmptyState title={t("latestResultsEmpty")} />
        )}
      </section>

      <section>
        <h2 className="mb-3 font-display text-xl font-semibold">{t("latestResults")}</h2>
        {latest.length > 0 ? (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {latest.map((m) => (
              <MatchCard key={m.id} match={m} locale={locale} linked />
            ))}
          </div>
        ) : (
          <EmptyState title={t("latestResultsEmpty")} />
        )}
      </section>

      <section>
        <h2 className="mb-3 font-display text-xl font-semibold">{t("news")}</h2>
        <div className="rounded-lg border border-dashed border-[var(--border)] px-6 py-10 text-center text-sm text-[var(--muted-foreground)]">
          {t("newsPlaceholder")}
        </div>
      </section>
    </main>
  );
}
