import { proxyGet } from "@/lib/api/serverProxy";

export async function POST(request: Request) {
  const { token, email } = (await request.json().catch(() => ({}))) as {
    token?: string;
    email?: string;
  };
  const qs = new URLSearchParams({ token: token ?? "", email: email ?? "" });
  return proxyGet(`/api/auth/verify-email?${qs.toString()}`);
}
