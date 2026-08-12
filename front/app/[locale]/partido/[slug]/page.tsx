import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";
import {
  getMatches,
  getMatch,
  getMatchEvents,
  getMatchLineups,
  getMatchPlayerStats,
  getSquad,
} from "@/lib/api/sports";
import { matchSlug } from "@/lib/slug";
import { classifyStatus } from "@/lib/matchStatus";
import { siteUrl } from "@/lib/site";
import { LiveRefresher } from "@/components/sports/LiveRefresher";
import { LiveScoreBadge } from "@/components/sports/LiveScoreBadge";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";
import type { MatchDto } from "@/lib/api/types";

export const revalidate = 30;

async function resolveMatch(slug: string): Promise<MatchDto | null> {
  const { items } = await getMatches({ pageSize: 50 });
  return items.find((m) => matchSlug(m) === slug) ?? null;
}

export async function generateStaticParams() {
  const { items } = await getMatches({ pageSize: 50 });
  return routing.locales.flatMap((locale) =>
    items.map((m) => ({ locale, slug: matchSlug(m) })),
  );
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}): Promise<Metadata> {
  const { locale, slug } = await params;
  const match = await resolveMatch(slug);
  if (!match) return {};
  const title = `${match.homeTeamName} vs ${match.awayTeamName}`;
  const t = await getTranslations({ locale, namespace: "Matches" });
  return {
    title,
    description: `${title} · ${match.competitionName ?? ""} — ${t("title")}`,
  };
}

export default async function MatchDetailPage({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Matches");
  const tc = await getTranslations("Common");

  const match = await resolveMatch(slug);
  if (!match) notFound();

  const state = classifyStatus(match.status);
  const [detail, events, lineups, stats, squad] = await Promise.all([
    getMatch(match.id),
    getMatchEvents(match.id),
    getMatchLineups(match.id),
    getMatchPlayerStats(match.id),
    getSquad(),
  ]);

  const playerName = new Map(squad.map((p) => [p.id, p.name]));
  const teamName = (id: string | null) =>
    id === match.homeTeamId
      ? match.homeTeamName
      : id === match.awayTeamId
        ? match.awayTeamName
        : null;

  const kickoff = new Date(match.dateUtc).toLocaleString(locale, {
    dateStyle: "full",
    timeStyle: "short",
  });

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "SportsEvent",
    name: `${match.homeTeamName} vs ${match.awayTeamName}`,
    startDate: match.dateUtc,
    eventStatus:
      state === "finished"
        ? "https://schema.org/EventCompleted"
        : "https://schema.org/EventScheduled",
    location: detail?.venue
      ? { "@type": "Place", name: detail.venue }
      : undefined,
    competitor: [
      { "@type": "SportsTeam", name: match.homeTeamName },
      { "@type": "SportsTeam", name: match.awayTeamName },
    ],
    url: `${siteUrl}/${locale}/${locale === "es" ? "partido" : "match"}/${slug}`,
  };

  return (
    <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-8 px-4 py-8">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      {state === "live" && <LiveRefresher />}

      <Breadcrumbs
        items={[
          { label: tc("home"), href: "/" },
          { label: t("title"), href: "/partidos" },
          { label: `${match.homeTeamName} vs ${match.awayTeamName}` },
        ]}
      />

      {/* Scoreboard */}
      <section className="rounded-xl border border-[var(--border)] bg-[var(--card)] p-6">
        <div className="mb-4 flex items-center justify-between gap-2 text-xs uppercase tracking-wide text-[var(--muted-foreground)]">
          <span>{match.competitionName}</span>
          {state === "live" ? (
            <LiveScoreBadge label={t("live")} minute={detail?.elapsed ?? undefined} />
          ) : (
            <span>{state === "finished" ? t("statusFinished") : t("statusScheduled")}</span>
          )}
        </div>
        <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-4">
          <div className="text-right font-display text-lg font-semibold">
            {match.homeTeamName}
          </div>
          <div className="tabular-nums text-3xl font-bold">
            {state === "scheduled"
              ? "—"
              : `${match.homeGoals ?? 0} : ${match.awayGoals ?? 0}`}
          </div>
          <div className="text-left font-display text-lg font-semibold">
            {match.awayTeamName}
          </div>
        </div>
        <p className="mt-4 text-center text-sm text-[var(--muted-foreground)]">
          <time dateTime={match.dateUtc}>{kickoff}</time>
          {detail?.venue ? ` · ${detail.venue}` : ""}
          {detail?.round ? ` · ${detail.round}` : ""}
        </p>
      </section>

      {/* Events */}
      <section>
        <h2 className="mb-3 font-display text-lg font-semibold">{t("events")}</h2>
        {events.length > 0 ? (
          <ol className="flex flex-col gap-2">
            {events.map((e, i) => (
              <li
                key={i}
                className="flex items-baseline gap-3 rounded-md border border-[var(--border)] px-3 py-2 text-sm"
              >
                <span className="tabular-nums font-semibold text-[var(--accent)]">
                  {e.minute}
                  {e.extraMinute ? `+${e.extraMinute}` : ""}&apos;
                </span>
                <span className="font-medium">{e.type}</span>
                <span className="text-[var(--muted-foreground)]">
                  {e.playerId ? playerName.get(e.playerId) ?? "" : ""}
                  {e.detail ? ` · ${e.detail}` : ""}
                </span>
                <span className="ml-auto text-xs text-[var(--muted-foreground)]">
                  {teamName(e.teamId)}
                </span>
              </li>
            ))}
          </ol>
        ) : (
          <EmptyState title={t("eventsEmpty")} />
        )}
      </section>

      {/* Lineups */}
      {lineups.length > 0 && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("lineups")}</h2>
          <div className="grid gap-4 sm:grid-cols-2">
            {lineups.map((lu) => (
              <div key={lu.teamId} className="rounded-lg border border-[var(--border)] p-4">
                <div className="mb-2 flex items-center justify-between">
                  <span className="font-semibold">{teamName(lu.teamId)}</span>
                  <span className="text-xs text-[var(--muted-foreground)]">
                    {lu.formation}
                  </span>
                </div>
                <ul className="flex flex-col gap-1 text-sm">
                  {lu.players.map((p) => (
                    <li key={p.playerId} className="flex gap-2">
                      <span className="tabular-nums text-[var(--muted-foreground)] w-6">
                        {p.number ?? "–"}
                      </span>
                      <span>{playerName.get(p.playerId) ?? "—"}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Player stats (count only if present) */}
      {stats.length > 0 && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("playerStats")}</h2>
          <p className="text-sm text-[var(--muted-foreground)]">
            {stats.length} {t("playerStats").toLowerCase()}
          </p>
        </section>
      )}
    </main>
  );
}
