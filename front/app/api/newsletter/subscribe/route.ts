import { proxyPost } from "@/lib/api/serverProxy";

export async function POST(request: Request) {
  const body = await request.json().catch(() => ({}));
  return proxyPost("/api/newsletter/subscribe", body);
}
