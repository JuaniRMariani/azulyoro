export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      className={`motion-safe:animate-pulse rounded bg-[var(--muted)] ${className ?? ""}`}
      aria-hidden
    />
  );
}
