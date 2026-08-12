import { proxyPost } from "@/lib/api/serverProxy";

export async function POST() {
  return proxyPost("/api/auth/logout", {});
}
