import { proxyPostWithCsrf } from "@/lib/api/serverProxy";

export async function POST(request: Request) {
  const body = await request.json().catch(() => ({}));
  return proxyPostWithCsrf("/api/auth/login", body);
}
