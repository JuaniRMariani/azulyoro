import { NextResponse } from "next/server";
import { revalidateTag, revalidatePath } from "next/cache";

interface RevalidateBody {
  secret?: string;
  tags?: string[];
  paths?: string[];
}

export async function POST(request: Request) {
  let body: RevalidateBody = {};
  try {
    body = (await request.json()) as RevalidateBody;
  } catch {
    // Empty / non-JSON body → treat as no tags/paths; secret may come via header.
  }

  const provided = request.headers.get("x-revalidate-secret") ?? body.secret;
  const expected = process.env.REVALIDATE_SECRET;

  if (!expected || provided !== expected) {
    return NextResponse.json({ revalidated: false }, { status: 401 });
  }

  for (const tag of body.tags ?? []) {
    // Next 16: second arg required. Expire immediately so the webhook pushes
    // fresh content on the next request.
    revalidateTag(tag, { expire: 0 });
  }
  for (const path of body.paths ?? []) {
    revalidatePath(path);
  }

  return NextResponse.json({ revalidated: true, now: Date.now() });
}
