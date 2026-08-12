import type { MatchDto } from "@/lib/api/types";
import { MatchCard } from "./MatchCard";

export interface MatchGroup {
  /** Group heading, e.g. a date or a competition name. */
  title: string;
  matches: MatchDto[];
}

/**
 * Renders MatchCards grouped by date or competition. Accepts already-grouped
 * data so callers decide the grouping strategy.
 */
export async function FixtureList({
  groups,
  locale,
}: {
  groups: MatchGroup[];
  locale: string;
}) {
  return (
    <div className="flex flex-col gap-8">
      {groups.map((group) => (
        <section key={group.title}>
          <h3 className="mb-3 font-display text-sm font-semibold uppercase tracking-wide text-[var(--accent)]">
            {group.title}
          </h3>
          <div className="grid gap-3 sm:grid-cols-2">
            {group.matches.map((match) => (
              <MatchCard key={match.id} match={match} locale={locale} linked />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
