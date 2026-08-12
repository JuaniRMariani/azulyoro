/**
 * SEO slug helper. Kebab-cases a name and strips accents/diacritics so player
 * URLs are stable and readable (no UUID). Resolution back to a record is done
 * by matching `slugify(name) === slug` against the fetched list.
 */
export function slugify(input: string): string {
  return input
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "") // strip combining diacritics
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-") // non-alphanumerics -> hyphen
    .replace(/^-+|-+$/g, ""); // trim leading/trailing hyphens
}

/** SEO slug for a match: home-vs-away-YYYY-MM-DD (no UUID in the URL). */
export function matchSlug(m: {
  homeTeamName: string | null;
  awayTeamName: string | null;
  dateUtc: string;
}): string {
  const date = m.dateUtc.slice(0, 10);
  return `${slugify(m.homeTeamName ?? "local")}-vs-${slugify(m.awayTeamName ?? "visita")}-${date}`;
}
