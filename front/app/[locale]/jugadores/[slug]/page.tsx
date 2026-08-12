import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getSquad, getPlayerStats, getCompetitions } from "@/lib/api/sports";
import type { PlayerDto } from "@/lib/api/types";
import { slugify } from "@/lib/slug";
import { PlayerStatsTable } from "@/components/sports/PlayerStatsTable";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";
import { routing } from "@/i18n/routing";

export const revalidate = 3600;

async function resolvePlayer(slug: string): Promise<PlayerDto | null> {
  const squad = await getSquad();
  return (squad ?? []).find((p) => slugify(p.name) === slug) ?? null;
}

export async function generateStaticParams() {
  const squad = await getSquad();
  const slugs = (squad ?? []).map((p) => slugify(p.name));
  return routing.locales.flatMap((locale) =>
    slugs.map((slug) => ({ locale, slug })),
  );
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}): Promise<Metadata> {
  const { locale, slug } = await params;
  const player = await resolvePlayer(slug);
  const t = await getTranslations({ locale, namespace: "Player" });
  if (!player) {
    return { title: t("notFoundTitle") };
  }
  return {
    title: t("metaTitle", { name: player.name }),
    description: t("metaDescription", { name: player.name }),
  };
}

function Fact({ label, value }: { label: string; value: string | number | null }) {
  if (value == null || value === "") return null;
  return (
    <div className="rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 py-2">
      <dt className="text-xs uppercase tracking-wide text-[var(--muted-foreground)]">
        {label}
      </dt>
      <dd className="mt-0.5 font-semibold tabular-nums">{value}</dd>
    </div>
  );
}

export default async function PlayerPage({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale, slug } = await params;
  setRequestLocale(locale);

  const player = await resolvePlayer(slug);
  if (!player) notFound();

  const t = await getTranslations("Player");
  const ts = await getTranslations("Squad");
  const tc = await getTranslations("Common");

  const [stats, competitions] = await Promise.all([
    getPlayerStats(player.id),
    getCompetitions(),
  ]);
  const birth = player.birthDate
    ? new Date(player.birthDate).toLocaleDateString(locale, { dateStyle: "long" })
    : null;

  return (
    <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-8 px-4 py-10">
      <Breadcrumbs
        items={[
          { label: tc("home"), href: "/" },
          { label: ts("title"), href: "/plantel" },
          { label: player.name },
        ]}
      />

      <header className="flex flex-col items-start gap-4 sm:flex-row sm:items-center">
        <span className="relative flex h-24 w-24 shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--muted)]">
          {player.photoUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={player.photoUrl}
              alt={player.name}
              width={96}
              height={96}
              className="h-24 w-24 object-cover"
            />
          ) : (
            <span className="font-display text-3xl font-bold text-[var(--muted-foreground)]">
              {player.name.charAt(0)}
            </span>
          )}
        </span>
        <div>
          <h1 className="font-display text-3xl font-bold tracking-tight">
            {player.name}
          </h1>
          <p className="mt-1 text-[var(--muted-foreground)]">
            {player.number != null && (
              <span className="font-semibold tabular-nums text-[var(--accent)]">
                #{player.number}{" "}
              </span>
            )}
            {player.position}
            {player.nationality ? ` · ${player.nationality}` : ""}
          </p>
        </div>
      </header>

      <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <Fact label={t("position")} value={player.position} />
        <Fact label={t("nationality")} value={player.nationality} />
        <Fact label={t("number")} value={player.number} />
        <Fact label={t("birthDate")} value={birth} />
        <Fact
          label={t("height")}
          value={player.height != null ? `${player.height} cm` : null}
        />
        <Fact
          label={t("weight")}
          value={player.weight != null ? `${player.weight} kg` : null}
        />
      </dl>

      <section>
        <h2 className="mb-3 font-display text-xl font-semibold">
          {t("seasonStats")}
        </h2>
        {stats.length > 0 ? (
          <PlayerStatsTable stats={stats} competitions={competitions} />
        ) : (
          <EmptyState title={t("statsEmpty")} />
        )}
      </section>
    </main>
  );
}
