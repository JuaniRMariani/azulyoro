import type { ReactNode } from "react";
import { EmptyState } from "./EmptyState";

/**
 * Renders `children` with the data when present, otherwise an empty state.
 * Loading is handled upstream via Suspense/`loading.tsx`; errors via
 * `error.tsx`. Keeps list pages declarative.
 */
export function QueryState<T>({
  data,
  emptyTitle,
  emptyDescription,
  children,
}: {
  data: T[] | null | undefined;
  emptyTitle: string;
  emptyDescription?: string;
  children: (items: T[]) => ReactNode;
}) {
  if (!data || data.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} />;
  }
  return <>{children(data)}</>;
}
