import { Link } from "@/i18n/navigation";
import type { PlayerDto } from "@/lib/api/types";
import { slugify } from "@/lib/slug";

/**
 * Player card: photo (fixed box to avoid CLS), number, name, position,
 * nationality. Links to the player's detail page by SEO slug.
 */
export function PlayerCard({ player }: { player: PlayerDto }) {
  const slug = slugify(player.name);

  return (
    <Link
      href={{ pathname: "/jugadores/[slug]", params: { slug } }}
      className="group flex items-center gap-3 rounded-lg border border-[var(--border)] bg-[var(--card)] p-3 transition-colors hover:border-[var(--accent)]"
    >
      <span className="relative flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--muted)]">
        {player.photoUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={player.photoUrl}
            alt={player.name}
            width={56}
            height={56}
            loading="lazy"
            className="h-14 w-14 object-cover"
          />
        ) : (
          <span className="font-display text-lg font-bold text-[var(--muted-foreground)]">
            {player.name.charAt(0)}
          </span>
        )}
        {player.number != null && (
          <span className="absolute -bottom-0.5 -right-0.5 flex h-6 w-6 items-center justify-center rounded-full bg-[var(--primary)] text-xs font-bold tabular-nums text-[var(--primary-foreground)]">
            {player.number}
          </span>
        )}
      </span>

      <span className="min-w-0 flex-1">
        <span className="block truncate font-semibold group-hover:text-[var(--accent)]">
          {player.name}
        </span>
        <span className="block truncate text-xs text-[var(--muted-foreground)]">
          {player.position}
          {player.nationality ? ` · ${player.nationality}` : ""}
        </span>
      </span>
    </Link>
  );
}
