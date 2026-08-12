import { getTranslations, setRequestLocale } from "next-intl/server";
import { getSquad, getNextMatch } from "@/lib/api/sports";
import { QueryState } from "@/components/ui/QueryState";

export const revalidate = 60;

export default async function Home({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Home");

  const [squad, next] = await Promise.all([getSquad(), getNextMatch()]);

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-10 px-4 py-10">
      <section className="text-center">
        <h1 className="font-display text-4xl font-bold tracking-tight">{t("title")}</h1>
        <p className="mt-2 text-lg text-[var(--muted-foreground)]">{t("tagline")}</p>
      </section>

      {next && (
        <section className="rounded-xl border border-[var(--border)] bg-[var(--card)] p-5">
          <h2 className="mb-2 font-display text-sm uppercase tracking-wide text-[var(--accent)]">
            {t("nextMatch")}
          </h2>
          <p className="text-lg font-semibold tabular-nums">
            {next.homeTeamName} vs {next.awayTeamName}
          </p>
          <p className="text-sm text-[var(--muted-foreground)]">
            {next.competitionName} ·{" "}
            {new Date(next.dateUtc).toLocaleString(locale, { dateStyle: "medium", timeStyle: "short" })}
          </p>
        </section>
      )}

      <section>
        <h2 className="mb-3 font-display text-xl font-semibold">{t("squad")}</h2>
        <QueryState data={squad} emptyTitle={t("squadEmpty")}>
          {(players) => (
            <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
              {players.map((p) => (
                <li
                  key={p.id}
                  className="rounded-lg border border-[var(--border)] px-3 py-2 text-sm"
                >
                  <span className="font-semibold tabular-nums text-[var(--accent)]">
                    {p.number ?? "–"}
                  </span>{" "}
                  {p.name}
                </li>
              ))}
            </ul>
          )}
        </QueryState>
      </section>
    </main>
  );
}
