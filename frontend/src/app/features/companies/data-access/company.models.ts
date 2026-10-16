/** Mirrors the API's `CompanyResponse`. */
export interface Company {
  readonly id: string;
  readonly name: string;
  readonly websiteUrl: string;
  readonly domain: string;
  readonly createdAt: string;
  /** Only present on relevance-ranked searches. */
  readonly relevanceScore?: number;
}

export interface Page<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasNextPage: boolean;
  readonly hasPreviousPage: boolean;
}

export interface CreateCompanyRequest {
  readonly name: string;
  readonly websiteUrl: string;
}

export const COMPANY_SORTS = ['relevance', 'name', '-name', 'createdAt', '-createdAt'] as const;
export type CompanySort = (typeof COMPANY_SORTS)[number];

export interface CompanySearchCriteria {
  /** Free-text relevance search. */
  readonly q?: string;
  readonly name?: string;
  readonly domain?: string;
  readonly sort?: CompanySort;
  readonly page?: number;
  readonly pageSize?: number;
}
