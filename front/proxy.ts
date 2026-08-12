import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";

export default createMiddleware(routing);

export const config = {
  // Match all pathnames except API routes, the (non-localized) admin CMS,
  // Next internals and static files.
  matcher: "/((?!api|admin|_next|_vercel|.*\\..*).*)",
};
