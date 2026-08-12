"use client";

import { useLocale } from "next-intl";
import { useParams } from "next/navigation";
import { usePathname, useRouter } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";

const LABELS: Record<string, string> = { es: "ES", en: "EN" };

export function LocaleSwitcher() {
  const active = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams();

  return (
    <div
      role="group"
      aria-label="Language"
      className="inline-flex items-center gap-1 rounded-full border border-[var(--border)] p-1"
    >
      {routing.locales.map((locale) => {
        const isActive = locale === active;
        return (
          <button
            key={locale}
            type="button"
            aria-current={isActive ? "true" : undefined}
            disabled={isActive}
            onClick={() =>
              router.replace(
                // Preserve dynamic route params when switching locale.
                // @ts-expect-error -- params are validated at runtime by next-intl
                { pathname, params },
                { locale },
              )
            }
            className={`rounded-full px-3 py-1 text-sm font-medium transition-colors ${
              isActive
                ? "bg-[var(--primary)] text-[var(--primary-foreground)]"
                : "text-[var(--foreground)] hover:bg-[var(--muted)]"
            }`}
          >
            {LABELS[locale] ?? locale.toUpperCase()}
          </button>
        );
      })}
    </div>
  );
}
