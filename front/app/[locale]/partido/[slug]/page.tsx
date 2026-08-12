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

function TeamCrest({ name, logoUrl }: { name: string | null; logoUrl: string | null }) {
  return (
    <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-[color-mix(in_oklab,var(--foreground)_6%,transparent)] p-2">
      {logoUrl ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={logoUrl}
          alt={name ?? ""}
          width={56}
          height={56}
          loading="eager"
          className="h-full w-full object-contain"
        />
      ) : (
        <span className="font-display text-xl font-bold text-[var(--muted-foreground)]">
          {(name ?? "?").slice(0, 3).toUpperCase()}
        </span>
      )}
    </span>
  );
}

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
  const played = state !== "scheduled";

  const [detail, events, lineups, stats, squad] = await Promise.all([
    getMatch(match.id),
    played ? getMatchEvents(match.id) : Promise.resolve([]),
    played ? getMatchLineups(match.id) : Promise.resolve([]),
    played ? getMatchPlayerStats(match.id) : Promise.resolve([]),
    getSquad().catch(() => []),
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
    location: detail?.venue ? { "@type": "Place", name: detail.venue } : undefined,
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
      <section className="overflow-hidden rounded-2xl border border-[var(--border)] bg-gradient-to-b from-[var(--azul-900)] to-[var(--card)] p-6 text-[var(--foreground)] shadow-lg">
        <div className="mb-6 flex items-center justify-between gap-2 text-xs font-semibold uppercase tracking-wide">
          <span className="text-[var(--oro-500)]">{match.competitionName}</span>
          {state === "live" ? (
            <LiveScoreBadge label={t("live")} minute={detail?.elapsed ?? undefined} />
          ) : (
            <span className="text-[var(--muted-foreground)]">
              {state === "finished" ? t("statusFinished") : t("statusScheduled")}
            </span>
          )}
        </div>

        <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-4">
          <div className="flex flex-col items-center gap-2 text-center">
            <TeamCrest name={match.homeTeamName} logoUrl={match.homeTeamLogoUrl} />
            <span className="font-display text-sm font-semibold sm:text-base">
              {match.homeTeamName}
            </span>
          </div>

          <div className="px-2 text-center">
            <div className="tabular-nums text-4xl font-bold sm:text-5xl">
              {played ? `${match.homeGoals ?? 0} : ${match.awayGoals ?? 0}` : "vs"}
            </div>
            {played && detail && (detail.htHome != null || detail.htAway != null) && (
              <div className="mt-1 text-xs text-[var(--muted-foreground)]">
                HT {detail.htHome ?? 0}-{detail.htAway ?? 0}
              </div>
            )}
          </div>

          <div className="flex flex-col items-center gap-2 text-center">
            <TeamCrest name={match.awayTeamName} logoUrl={match.awayTeamLogoUrl} />
            <span className="font-display text-sm font-semibold sm:text-base">
              {match.awayTeamName}
            </span>
          </div>
        </div>

        <p className="mt-6 text-center text-sm text-[var(--muted-foreground)]">
          <time dateTime={match.dateUtc}>{kickoff}</time>
          {detail?.venue ? ` · ${detail.venue}` : ""}
          {detail?.round ? ` · ${detail.round}` : ""}
        </p>
      </section>

      {/* Scheduled match: no events/stats yet */}
      {!played && (
        <EmptyState
          title={t("notStartedTitle")}
          description={t("notStartedDescription")}
        />
      )}

      {/* Events */}
      {played && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("events")}</h2>
          {events.length > 0 ? (
            <ol className="flex flex-col gap-2">
              {events.map((e, i) => (
                <li
                  key={i}
                  className="flex items-baseline gap-3 rounded-md border border-[var(--border)] px-3 py-2 text-sm"
                >
                  <span className="w-10 tabular-nums font-semibold text-[var(--accent)]">
                    {e.minute}
                    {e.extraMinute ? `+${e.extraMinute}` : ""}&apos;
                  </span>
                  <span className="font-medium">{e.type}</span>
                  <span className="text-[var(--muted-foreground)]">
                    {e.playerId ? (playerName.get(e.playerId) ?? "") : ""}
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
      )}

      {/* Lineups */}
      {lineups.length > 0 && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("lineups")}</h2>
          <div className="grid gap-4 sm:grid-cols-2">
            {lineups.map((lu) => (
              <div key={lu.teamId} className="rounded-lg border border-[var(--border)] p-4">
                <div className="mb-2 flex items-center justify-between">
                  <span className="font-semibold">{teamName(lu.teamId)}</span>
                  <span className="text-xs text-[var(--muted-foreground)]">{lu.formation}</span>
                </div>
                <ul className="flex flex-col gap-1 text-sm">
                  {lu.players.map((p) => (
                    <li key={p.playerId} className="flex gap-2">
                      <span className="w-6 tabular-nums text-[var(--muted-foreground)]">
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

      {/* Player stats */}
      {stats.length > 0 && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("playerStats")}</h2>
          <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
            <table className="w-full text-sm">
              <thead className="bg-[var(--muted)] text-left text-xs uppercase tracking-wide text-[var(--muted-foreground)]">
                <tr>
                  <th className="px-3 py-2">{t("player")}</th>
                  <th className="px-3 py-2 text-right">{t("minutesShort")}</th>
                  <th className="px-3 py-2 text-right">{t("goalsShort")}</th>
                  <th className="px-3 py-2 text-right">{t("assistsShort")}</th>
                  <th className="px-3 py-2 text-right">{t("ratingShort")}</th>
                </tr>
              </thead>
              <tbody>
                {stats.map((s, i) => (
                  <tr key={i} className="border-t border-[var(--border)]">
                    <td className="px-3 py-2">{playerName.get(s.playerId) ?? "—"}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{s.minutes ?? 0}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{s.goals}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{s.assists}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{s.rating ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </main>
  );
}
