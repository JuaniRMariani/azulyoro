import { defineRouting } from "next-intl/routing";

export const routing = defineRouting({
  locales: ["es", "en"],
  defaultLocale: "es",
  // Always prefix the locale in the URL (/es, /en). x-default resolves to es
  // via the middleware below.
  localePrefix: "always",
});

export type Locale = (typeof routing.locales)[number];
