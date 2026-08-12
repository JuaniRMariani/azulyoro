import { cookies } from "next/headers";
import { NextResponse } from "next/server";

/**
 * Server-side proxy helpers for auth/newsletter mutations. The browser cannot
 * call the API directly (cross-origin + HttpOnly cookies + antiforgery), so
 * these run inside Next route handlers: they forward the user's session cookie,
 * fetch + attach the CSRF token, and relay Set-Cookie back to the browser.
 */

const API_URL =
  process.env.API_INTERNAL_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
const IS_DEV = process.env.NODE_ENV !== "production";

/** Serialize the incoming request cookies so the API sees the session. */
async function cookieHeader(): Promise<string> {
  const store = await cookies();
  return store.toString();
}

/**
 * Fetch a fresh antiforgery token + its cookie. Returns the token and the
 * raw Set-Cookie so the caller can send the csrf cookie on the same POST.
 */
async function getCsrf(incomingCookies: string): Promise<{
  token: string;
  setCookie: string | null;
}> {
  const res = await fetch(`${API_URL}/api/auth/csrf`, {
    headers: incomingCookies ? { cookie: incomingCookies } : {},
    cache: "no-store",
  });
  const setCookie = res.headers.get("set-cookie");
  const body = (await res.json().catch(() => ({}))) as { token?: string };
  return { token: body.token ?? "", setCookie };
}

/**
 * In dev the API sets Secure cookies, which the browser drops over plain
 * http://localhost. Strip the Secure attribute locally so the session cookie
 * actually persists in the browser. In production (https) leave it intact.
 */
function normalizeSetCookie(value: string): string {
  if (!IS_DEV) return value;
  return value
    .split(/,(?=[^;]+?=)/) // split multiple cookies on the top-level comma
    .map((c) => c.replace(/;\s*Secure/gi, ""))
    .join(", ");
}

function relaySetCookie(from: Response, to: NextResponse) {
  const raw = from.headers.get("set-cookie");
  if (raw) {
    to.headers.set("set-cookie", normalizeSetCookie(raw));
  }
}

/**
 * POST a JSON body to the API with CSRF + session cookie forwarding, and relay
 * any Set-Cookie (session login, csrf) back to the browser.
 */
export async function proxyPostWithCsrf(
  path: string,
  body: unknown,
): Promise<NextResponse> {
  const incoming = await cookieHeader();
  const { token, setCookie: csrfSetCookie } = await getCsrf(incoming);

  // Combine the incoming cookies with the freshly issued csrf cookie so the
  // antiforgery middleware can validate the header against the cookie.
  const csrfCookiePair = csrfSetCookie
    ? csrfSetCookie.split(";")[0]
    : "";
  const combinedCookie = [incoming, csrfCookiePair]
    .filter(Boolean)
    .join("; ");

  const res = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      "X-XSRF-TOKEN": token,
      ...(combinedCookie ? { cookie: combinedCookie } : {}),
    },
    body: JSON.stringify(body ?? {}),
    cache: "no-store",
  });

  const text = await res.text();
  const out = new NextResponse(text || null, {
    status: res.status,
    headers: { "content-type": res.headers.get("content-type") ?? "application/json" },
  });
  relaySetCookie(res, out);
  return out;
}

/** Simple POST forward (no CSRF) — used for newsletter subscribe. */
export async function proxyPost(path: string, body: unknown): Promise<NextResponse> {
  const incoming = await cookieHeader();
  const res = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      ...(incoming ? { cookie: incoming } : {}),
    },
    body: JSON.stringify(body ?? {}),
    cache: "no-store",
  });
  const text = await res.text();
  const out = new NextResponse(text || null, {
    status: res.status,
    headers: { "content-type": res.headers.get("content-type") ?? "application/json" },
  });
  relaySetCookie(res, out);
  return out;
}

/** GET forward with session cookie (verify/confirm/unsubscribe, logout). */
export async function proxyGet(path: string): Promise<NextResponse> {
  const incoming = await cookieHeader();
  const res = await fetch(`${API_URL}${path}`, {
    headers: incoming ? { cookie: incoming } : {},
    cache: "no-store",
  });
  const text = await res.text();
  const out = new NextResponse(text || null, {
    status: res.status,
    headers: { "content-type": res.headers.get("content-type") ?? "application/json" },
  });
  relaySetCookie(res, out);
  return out;
}
