"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

/**
 * While a match is live, periodically re-render the server component via
 * router.refresh() so the score/events update in-page. Uses the Next server
 * (no direct browser→API call), so there is no CORS concern.
 */
export function LiveRefresher({ intervalMs = 30000 }: { intervalMs?: number }) {
  const router = useRouter();
  useEffect(() => {
    const id = setInterval(() => router.refresh(), intervalMs);
    return () => clearInterval(id);
  }, [router, intervalMs]);
  return null;
}
