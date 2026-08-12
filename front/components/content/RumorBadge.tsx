import { useTranslations } from "next-intl";

/** Amber "Unconfirmed" pill flagging rumor-category articles. */
export function RumorBadge() {
  const t = useTranslations("News");
  return (
    <span className="inline-flex items-center rounded-full bg-amber-500/15 px-2 py-0.5 text-xs font-semibold uppercase tracking-wide text-amber-600 dark:text-amber-400">
      {t("unconfirmed")}
    </span>
  );
}
