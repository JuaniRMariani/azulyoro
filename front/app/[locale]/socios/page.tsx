import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { redirect } from "@/i18n/navigation";
import { getMe, getMembersContent } from "@/lib/api/auth";
import { ArticleCard } from "@/components/content/ArticleCard";
import { EmptyState } from "@/components/ui/EmptyState";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Members" });
  return { title: t("metaTitle"), description: t("description"), robots: { index: false } };
}

export default async function MembersPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Members");

  // Auth gate: no session → send to login.
  const me = await getMe();
  if (!me) {
    redirect({ href: "/ingresar", locale });
  }

  const articles = (await getMembersContent(locale)) ?? [];

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-8 px-4 py-12">
      <header className="flex flex-col gap-2">
        <span className="w-fit rounded-full bg-[var(--accent)] px-2.5 py-0.5 text-xs font-semibold text-[var(--accent-foreground)]">
          {t("badge")}
        </span>
        <h1 className="font-display text-3xl font-bold tracking-tight">
          {t("title")}
        </h1>
        <p className="text-[var(--muted-foreground)]">{t("description")}</p>
      </header>

      {articles.length > 0 ? (
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {articles.map((article) => (
            <ArticleCard key={article.slug} article={article} />
          ))}
        </div>
      ) : (
        <EmptyState title={t("empty")} />
      )}
    </main>
  );
}
