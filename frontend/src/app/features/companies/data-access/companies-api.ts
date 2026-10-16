import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import { API_BASE_URL } from '../../../core/config/api-base-url';
import type { Company, CompanySearchCriteria, CreateCompanyRequest, Page } from './company.models';

/** Thin, typed gateway to `/companies`. Errors surface as `ApiError` (see `apiErrorInterceptor`). */
@Injectable({ providedIn: 'root' })
export class CompaniesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/companies`;

  create(request: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.baseUrl, request);
  }

  getById(id: string): Observable<Company> {
    return this.http.get<Company>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  search(criteria: CompanySearchCriteria = {}): Observable<Page<Company>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(criteria) as [string, unknown][]) {
      if (typeof value === 'number' || (typeof value === 'string' && value.trim() !== '')) {
        params = params.set(key, String(value).trim());
      }
    }
    return this.http.get<Page<Company>>(this.baseUrl, { params });
  }
}
