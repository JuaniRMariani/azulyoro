import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { AuthShell } from "@/components/auth/AuthShell";
import { LoginForm } from "@/components/auth/LoginForm";

export const dynamic = "force-dynamic";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Auth" });
  return { title: t("loginMetaTitle"), description: t("loginDescription") };
}

export default async function LoginPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Auth");

  return (
    <AuthShell
      title={t("loginTitle")}
      description={t("loginDescription")}
      footer={
        <>
          {t("noAccount")}{" "}
          <Link href="/registrarse" className="font-semibold text-[var(--primary)]">
            {t("goRegister")}
          </Link>
        </>
      }
    >
      <LoginForm />
    </AuthShell>
  );
}
