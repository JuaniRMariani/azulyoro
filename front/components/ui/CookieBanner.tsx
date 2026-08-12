"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

const STORAGE_KEY = "azulyoro.cookie-notice";

/**
 * Minimal, dismissible cookie notice. Plausible is cookieless and we set no
 * non-essential cookies, so this is informational only — dismissal is stored
 * in localStorage (not a cookie). Rendered inside the [locale] layout body.
 */
export function CookieBanner() {
  const t = useTranslations("Cookies");
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    try {
      if (localStorage.getItem(STORAGE_KEY) !== "dismissed") {
        setVisible(true);
      }
    } catch {
      setVisible(true);
    }
  }, []);

  function dismiss() {
    try {
      localStorage.setItem(STORAGE_KEY, "dismissed");
    } catch {
      /* ignore storage errors */
    }
    setVisible(false);
  }

  if (!visible) return null;

  return (
    <div
      role="region"
      aria-label="Cookies"
      className="fixed inset-x-0 bottom-0 z-50 border-t border-[var(--border)] bg-[color-mix(in_oklab,var(--card)_96%,transparent)] px-4 py-3 backdrop-blur"
    >
      <div className="mx-auto flex max-w-6xl flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-[var(--muted-foreground)]">
          {t("bannerText")}{" "}
          <Link href="/cookies" className="font-semibold text-[var(--primary)] underline">
            {t("bannerLink")}
          </Link>
        </p>
        <button
          type="button"
          onClick={dismiss}
          className="shrink-0 rounded-full bg-[var(--primary)] px-4 py-1.5 text-sm font-semibold text-[var(--primary-foreground)] transition-opacity hover:opacity-90"
        >
          {t("bannerAccept")}
        </button>
      </div>
    </div>
  );
}
