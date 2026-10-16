import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { apiErrorInterceptor } from '../../../core/http/api-error.interceptor';
import { CompaniesApi } from './companies-api';
import type { Company } from './company.models';

const company: Company = {
  id: '01a1',
  name: 'Acme',
  websiteUrl: 'https://acme.com',
  domain: 'acme.com',
  createdAt: '2026-10-16T12:00:00Z',
};

describe('CompaniesApi', () => {
  let api: CompaniesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(CompaniesApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('posts new companies to the versioned endpoint', async () => {
    const result = firstValueFrom(api.create({ name: 'Acme', websiteUrl: 'https://acme.com' }));

    const request = http.expectOne('/api/v1/companies');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Acme', websiteUrl: 'https://acme.com' });
    request.flush(company, { status: 201, statusText: 'Created' });

    await expect(result).resolves.toEqual(company);
  });

  it('sends only non-empty search criteria as query parameters', () => {
    api
      .search({ q: ' micro ', name: '', domain: '   ', sort: 'relevance', page: 2, pageSize: 10 })
      .subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/companies');
    expect(request.request.params.keys()).toEqual(['q', 'sort', 'page', 'pageSize']);
    expect(request.request.params.get('q')).toBe('micro');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });
  });

  it('encodes ids when fetching a single company', () => {
    api.getById('a/b').subscribe();

    http.expectOne('/api/v1/companies/a%2Fb').flush(company);
  });

  it('surfaces failures as ApiError', async () => {
    const result = firstValueFrom(api.getById('missing'));

    http
      .expectOne('/api/v1/companies/missing')
      .flush(
        { title: 'Resource not found', status: 404, detail: 'No company exists with id.' },
        { status: 404, statusText: 'Not Found' },
      );

    await expect(result).rejects.toMatchObject({
      status: 404,
      message: 'No company exists with id.',
    });
  });
});
