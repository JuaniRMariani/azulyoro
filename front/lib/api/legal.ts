import { apiGetOrNull } from "./client";

export interface LegalPageDto {
  slug: string;
  locale: string;
  title: string;
  bodyHtml: string;
  version: number;
  effectiveDate: string;
}

/** Public legal page by slug + locale. 404 → null. ISR-cacheable (1h). */
export const getLegalPage = (slug: string, locale: string) =>
  apiGetOrNull<LegalPageDto>(
    `/api/legal/${encodeURIComponent(slug)}?locale=${encodeURIComponent(locale)}`,
    { tags: [`legal:${slug}`], revalidate: 3600 },
  );
