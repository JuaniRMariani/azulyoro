import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getSquad } from "@/lib/api/sports";
import type { PlayerDto } from "@/lib/api/types";
import { PlayerCard } from "@/components/sports/PlayerCard";
import { Breadcrumbs } from "@/components/sports/Breadcrumbs";
import { EmptyState } from "@/components/ui/EmptyState";

export const revalidate = 3600;

const POSITION_ORDER = ["Goalkeeper", "Defender", "Midfielder", "Attacker"] as const;
const POSITION_KEY: Record<string, string> = {
  Goalkeeper: "goalkeepers",
  Defender: "defenders",
  Midfielder: "midfielders",
  Attacker: "attackers",
};

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Squad" });
  return { title: t("metaTitle"), description: t("metaDescription") };
}

export default async function SquadPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Squad");
  const tc = await getTranslations("Common");

  const squad = await getSquad();
  const players = squad ?? [];

  const byPosition = new Map<string, PlayerDto[]>();
  for (const p of players) {
    const bucket = byPosition.get(p.position) ?? [];
    bucket.push(p);
    byPosition.set(p.position, bucket);
  }
  const known = new Set<string>(POSITION_ORDER);
  const orderedPositions = [
    ...POSITION_ORDER.filter((pos) => byPosition.has(pos)),
    ...[...byPosition.keys()].filter((pos) => !known.has(pos)),
  ];

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

      {players.length > 0 ? (
        <div className="flex flex-col gap-8">
          {orderedPositions.map((pos) => (
            <section key={pos}>
              <h2 className="mb-3 font-display text-sm font-semibold uppercase tracking-wide text-[var(--accent)]">
                {POSITION_KEY[pos] ? t(POSITION_KEY[pos]) : pos}
              </h2>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {(byPosition.get(pos) ?? [])
                  .slice()
                  .sort((a, b) => (a.number ?? 99) - (b.number ?? 99))
                  .map((p) => (
                    <PlayerCard key={p.id} player={p} />
                  ))}
              </div>
            </section>
          ))}
        </div>
      ) : (
        <EmptyState title={t("empty")} />
      )}
    </main>
  );
}
