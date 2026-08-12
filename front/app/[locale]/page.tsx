import { useTranslations } from "next-intl";
import { setRequestLocale } from "next-intl/server";
import { use } from "react";
import { LocaleSwitcher } from "@/components/ui/LocaleSwitcher";

export default function Home({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = use(params);
  setRequestLocale(locale);
  const t = useTranslations("Home");

  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-6 p-8 text-center">
      <h1 className="text-4xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="max-w-md text-lg text-zinc-600 dark:text-zinc-400">
        {t("tagline")}
      </p>
      <LocaleSwitcher />
    </main>
  );
}
