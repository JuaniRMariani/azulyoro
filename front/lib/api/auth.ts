import "server-only";
import { cookies } from "next/headers";
import type { ArticleListItemDto } from "./types";

const API_URL =
  process.env.API_INTERNAL_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
const API_INTERNAL_HOST = process.env.API_INTERNAL_HOST;

export interface MeDto {
  email: string;
  displayName: string | null;
  locale: string;
  roles: string[];
}

/**
 * Authenticated GET that forwards the session cookie and treats 401/404 as an
 * absent resource (null). Auth calls opt into dynamic rendering, so never
 * cached. Non-401 errors surface as thrown for the nearest error boundary.
 */
async function authGetOrNull<T>(path: string): Promise<T | null> {
  const store = await cookies();
  const cookie = store.toString();
  const res = await fetch(`${API_URL}${path}`, {
    headers: {
      ...(API_INTERNAL_HOST ? { host: API_INTERNAL_HOST } : {}),
      ...(cookie ? { cookie } : {}),
    },
    cache: "no-store",
  });
  if (res.status === 401 || res.status === 403 || res.status === 404 || res.status === 204) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`Auth API ${res.status} on ${path}`);
  }
  return (await res.json()) as T;
}

/** Current authenticated user via the session cookie. 401 → null. */
export const getMe = () => authGetOrNull<MeDto>("/api/auth/me");

/** Members-only published articles (F4-5). 401 → null (redirect to login). */
export const getMembersContent = (locale: string) =>
  authGetOrNull<ArticleListItemDto[]>(
    `/api/members/content?locale=${encodeURIComponent(locale)}`,
  );
