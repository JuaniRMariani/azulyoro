type WordmarkProps = {
  className?: string;
  /** Show the compact "AyO" monogram instead of the full wordmark. */
  compact?: boolean;
};

/**
 * Brand-owned wordmark for "Azul y Oro". Never renders the club crest.
 * Colours use design tokens so it adapts to light/dark automatically.
 */
export function Wordmark({ className, compact = false }: WordmarkProps) {
  return (
    <span
      className={`inline-flex items-center font-display font-bold leading-none tracking-tight ${className ?? ""}`}
      aria-label="Azul y Oro"
    >
      {compact ? (
        <span className="inline-flex items-baseline">
          <span className="text-[var(--primary)]">Ay</span>
          <span className="text-[var(--accent)]">O</span>
        </span>
      ) : (
        <span className="inline-flex items-baseline gap-[0.15em]">
          <span className="text-[var(--primary)]">Azul</span>
          <span className="text-[var(--muted-foreground)] text-[0.7em]">y</span>
          <span className="text-[var(--accent)]">Oro</span>
        </span>
      )}
    </span>
  );
}
