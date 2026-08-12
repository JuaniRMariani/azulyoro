/**
 * Red "LIVE" pill using --live, with a pulsing dot (respects
 * prefers-reduced-motion) and an optional minute label. Static — no client JS.
 */
export function LiveScoreBadge({
  label,
  minute,
  className,
}: {
  label: string;
  minute?: number | null;
  className?: string;
}) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full bg-[color-mix(in_oklab,var(--live)_16%,transparent)] px-2 py-0.5 text-xs font-semibold uppercase tracking-wide text-[var(--live)] ${className ?? ""}`}
    >
      <span
        className="h-2 w-2 rounded-full bg-[var(--live)] motion-safe:animate-pulse"
        aria-hidden
      />
      {label}
      {minute != null && <span className="tabular-nums">{minute}&apos;</span>}
    </span>
  );
}
