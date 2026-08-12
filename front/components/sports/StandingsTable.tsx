import { getTranslations } from "next-intl/server";
import type { StandingDto } from "@/lib/api/types";

function isBocaRow(row: StandingDto, bocaTeamId?: string): boolean {
  if (bocaTeamId && row.teamId === bocaTeamId) return true;
  return (row.teamName ?? "").toLowerCase().includes("boca");
}

function FormPips({ form }: { form: string | null }) {
  if (!form) return <span className="text-[var(--muted-foreground)]">—</span>;
  const color: Record<string, string> = {
    W: "bg-[var(--success)]",
    D: "bg-[var(--warning)]",
    L: "bg-[var(--danger)]",
  };
  return (
    <span className="inline-flex gap-1">
      {form
        .slice(-5)
        .split("")
        .map((r, i) => (
          <span
            key={i}
            title={r}
            className={`h-4 w-4 rounded-sm text-[10px] font-bold leading-4 text-white ${
              color[r.toUpperCase()] ?? "bg-[var(--muted)]"
            } text-center`}
          >
            {r.toUpperCase()}
          </span>
        ))}
    </span>
  );
}

/**
 * Standings table (tabular-nums). Highlights the tracked team's row — matched by
 * `bocaTeamId` when provided, otherwise by teamName containing "Boca".
 */
export async function StandingsTable({
  rows,
  bocaTeamId,
}: {
  rows: StandingDto[];
  bocaTeamId?: string;
}) {
  const t = await getTranslations("Standings");

  return (
    <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
      <table className="w-full min-w-[640px] text-sm tabular-nums">
        <thead>
          <tr className="border-b border-[var(--border)] bg-[var(--muted)] text-left text-xs uppercase tracking-wide text-[var(--muted-foreground)]">
            <th scope="col" className="px-3 py-2 text-right">{t("rank")}</th>
            <th scope="col" className="px-3 py-2 text-left">{t("team")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("played")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("win")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("draw")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("lose")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("goalsFor")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("goalsAgainst")}</th>
            <th scope="col" className="px-2 py-2 text-right">{t("goalsDiff")}</th>
            <th scope="col" className="px-3 py-2 text-right font-bold">{t("points")}</th>
            <th scope="col" className="px-3 py-2 text-left">{t("form")}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const boca = isBocaRow(row, bocaTeamId);
            return (
              <tr
                key={row.teamId}
                className={`border-b border-[var(--border)] last:border-0 ${
                  boca
                    ? "bg-[color-mix(in_oklab,var(--accent)_14%,transparent)] font-semibold"
                    : ""
                }`}
              >
                <td className="px-3 py-2 text-right text-[var(--muted-foreground)]">
                  {row.rank}
                </td>
                <td className="px-3 py-2 text-left">
                  <span
                    className={
                      boca
                        ? "border-l-2 border-[var(--accent)] pl-2"
                        : undefined
                    }
                  >
                    {row.teamName ?? "—"}
                  </span>
                </td>
                <td className="px-2 py-2 text-right">{row.played}</td>
                <td className="px-2 py-2 text-right">{row.win}</td>
                <td className="px-2 py-2 text-right">{row.draw}</td>
                <td className="px-2 py-2 text-right">{row.lose}</td>
                <td className="px-2 py-2 text-right">{row.goalsFor}</td>
                <td className="px-2 py-2 text-right">{row.goalsAgainst}</td>
                <td className="px-2 py-2 text-right">
                  {row.goalsDiff > 0 ? `+${row.goalsDiff}` : row.goalsDiff}
                </td>
                <td className="px-3 py-2 text-right font-bold">{row.points}</td>
                <td className="px-3 py-2 text-left">
                  <FormPips form={row.form} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
