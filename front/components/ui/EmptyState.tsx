import type { ReactNode } from "react";

export function EmptyState({
  title,
  description,
  icon,
}: {
  title: string;
  description?: string;
  icon?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-[var(--border)] px-6 py-12 text-center">
      {icon}
      <p className="font-display text-lg font-semibold">{title}</p>
      {description && (
        <p className="max-w-sm text-sm text-[var(--muted-foreground)]">{description}</p>
      )}
    </div>
  );
}
