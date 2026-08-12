import { apiGet, apiGetOrNull } from "./client";
import type {
  ArticleCategory,
  ArticleDto,
  ArticleListItemDto,
  PagedResult,
} from "./types";

const REVALIDATE = 300;

function qs(params: Record<string, string | number | undefined>): string {
  const entries = Object.entries(params).filter(([, v]) => v !== undefined && v !== "");
  if (entries.length === 0) return "";
  return "?" + entries.map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`).join("&");
}

export interface ArticleQuery {
  category?: ArticleCategory;
  locale: string;
  page?: number;
  pageSize?: number;
}

/** Paginated article list. Tagged `news-list` for on-demand revalidation. */
export const getArticles = (query: ArticleQuery) =>
  apiGet<PagedResult<ArticleListItemDto>>(`/api/articles${qs({ ...query })}`, {
    tags: ["news-list"],
    revalidate: REVALIDATE,
  });

/** Single article by slug + locale. 404 → null. Tagged `article:{slug}`. */
export const getArticle = (slug: string, locale: string) =>
  apiGetOrNull<ArticleDto>(`/api/articles/${encodeURIComponent(slug)}${qs({ locale })}`, {
    tags: [`article:${slug}`],
    revalidate: REVALIDATE,
  });

/** Featured articles (list shape). Tagged `news-list`. */
export const getFeatured = (locale: string) =>
  apiGet<ArticleListItemDto[]>(`/api/articles/featured${qs({ locale })}`, {
    tags: ["news-list"],
    revalidate: REVALIDATE,
  });
