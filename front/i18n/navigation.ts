import { createNavigation } from "next-intl/navigation";
import { routing } from "./routing";

// Locale-aware wrappers. `usePathname` returns the path WITHOUT the locale
// prefix, so switching locales preserves the current route.
export const { Link, redirect, usePathname, useRouter, getPathname } =
  createNavigation(routing);
