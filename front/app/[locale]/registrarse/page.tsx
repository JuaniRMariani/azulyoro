import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { AuthShell } from "@/components/auth/AuthShell";
import { RegisterForm } from "@/components/auth/RegisterForm";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Auth" });
  return { title: t("registerMetaTitle"), description: t("registerDescription") };
}

export default async function RegisterPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Auth");

  return (
    <AuthShell
      title={t("registerTitle")}
      description={t("registerDescription")}
      footer={
        <>
          {t("hasAccount")}{" "}
          <Link href="/ingresar" className="font-semibold text-[var(--primary)]">
            {t("goLogin")}
          </Link>
        </>
      }
    >
      <RegisterForm />
    </AuthShell>
  );
}
