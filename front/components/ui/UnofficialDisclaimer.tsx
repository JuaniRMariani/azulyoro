import { useTranslations } from "next-intl";

/**
 * Non-official disclaimer band. Text is the exact wording from
 * docs/02-legal.md §1 (see messages Footer.disclaimer). Must appear on every
 * page via the global Footer.
 */
export function UnofficialDisclaimer({ className }: { className?: string }) {
  const t = useTranslations("Footer");
  return (
    <p
      className={`text-xs leading-relaxed text-[var(--muted-foreground)] ${className ?? ""}`}
    >
      {t("disclaimer")}
    </p>
  );
}
