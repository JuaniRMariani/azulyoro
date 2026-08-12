import "server-only";
import type { ModerationItemDto, UpdateArticleInput } from "./types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

class AdminApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly path: string,
    public readonly detail?: string,
  ) {
    super(`Admin API ${status} on ${path}${detail ? `: ${detail}` : ""}`);
    this.name = "AdminApiError";
  }
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    method,
    headers: body !== undefined ? { "content-type": "application/json" } : {},
    body: body !== undefined ? JSON.stringify(body) : undefined,
    cache: "no-store",
  });
  if (!res.ok) {
    const detail = await res.text().catch(() => undefined);
    throw new AdminApiError(res.status, path, detail || undefined);
  }
  if (res.status === 204) return null as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : null) as T;
}

/** Pending (or filtered) staging items awaiting moderation. */
export const getModerationQueue = (status = "Pending") =>
  request<ModerationItemDto[]>(
    "GET",
    `/api/admin/moderation?status=${encodeURIComponent(status)}`,
  );

/** Approve a staging item → returns the created article id. */
export const approveModeration = (id: string) =>
  request<{ articleId: string }>("POST", `/api/admin/moderation/${id}/approve`);

/** Reject a staging item. */
export const rejectModeration = (id: string) =>
  request<void>("POST", `/api/admin/moderation/${id}/reject`);

/** Update an article's translations + metadata. */
export const updateArticle = (id: string, input: UpdateArticleInput) =>
  request<void>("PUT", `/api/admin/articles/${id}`, input);

/** Publish an article. */
export const publishArticle = (id: string) =>
  request<void>("POST", `/api/admin/articles/${id}/publish`);
