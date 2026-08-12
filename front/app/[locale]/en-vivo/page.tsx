import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { redirect } from "@/i18n/navigation";
import { getLiveMatches, getNextMatch } from "@/lib/api/sports";
import { matchSlug } from "@/lib/slug";
import { MatchCard } from "@/components/sports/MatchCard";
import { EmptyState } from "@/components/ui/EmptyState";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Live" });
  return { title: t("title"), description: t("description") };
}

export default async function LivePage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);

  const live = await getLiveMatches();
  if (live && live.length > 0) {
    redirect({
      href: { pathname: "/partido/[slug]", params: { slug: matchSlug(live[0]) } },
      locale,
    });
  }

  const t = await getTranslations("Live");
  const next = await getNextMatch();

  return (
    <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-6 px-4 py-10">
      <h1 className="font-display text-3xl font-bold tracking-tight">{t("title")}</h1>
      <EmptyState title={t("noLive")} description={t("noLiveDescription")} />
      {next && (
        <section>
          <h2 className="mb-3 font-display text-lg font-semibold">{t("nextUp")}</h2>
          <MatchCard match={next} locale={locale} linked />
        </section>
      )}
    </main>
  );
}
