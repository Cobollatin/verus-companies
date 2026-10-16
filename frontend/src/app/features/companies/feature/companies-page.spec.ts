import { provideHttpClient, withInterceptors } from '@angular/common/http';
import type { TestRequest } from '@angular/common/http/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { apiErrorInterceptor } from '../../../core/http/api-error.interceptor';
import type { Company, Page } from '../data-access/company.models';
import { CompaniesPage, PAGE_SIZE } from './companies-page';

const acme: Company = {
  id: '1',
  name: 'Acme',
  websiteUrl: 'https://acme.com',
  domain: 'acme.com',
  createdAt: '2026-10-16T12:00:00Z',
};

const pageOf = (items: Company[]): Page<Company> => ({
  items,
  page: 1,
  pageSize: PAGE_SIZE,
  totalCount: items.length,
  totalPages: items.length ? 1 : 0,
  hasNextPage: false,
  hasPreviousPage: false,
});

describe('CompaniesPage', () => {
  let fixture: ComponentFixture<CompaniesPage>;
  let http: HttpTestingController;
  let element: HTMLElement;

  const expectSearch = (): TestRequest =>
    http.expectOne((r) => r.method === 'GET' && r.url === '/api/v1/companies');

  const render = async (): Promise<void> => {
    await fixture.whenStable();
  };

  const text = (): string => element.textContent.replace(/\s+/g, ' ');

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [CompaniesPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CompaniesPage);
    element = fixture.nativeElement as HTMLElement;
    await render();
  });

  afterEach(() => {
    http.verify();
  });

  it('loads the first page immediately', async () => {
    const request = expectSearch();
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe(String(PAGE_SIZE));

    request.flush(pageOf([acme]));
    await render();

    expect(element.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(text()).toContain('1 company');
    expect(text()).toContain('acme.com');
  });

  it('shows an empty state when there are no companies', async () => {
    expectSearch().flush(pageOf([]));
    await render();

    expect(text()).toContain('No companies match your search.');
  });

  it('creates a company, confirms it and refreshes the list', async () => {
    expectSearch().flush(pageOf([]));
    await render();

    const name = element.querySelector<HTMLInputElement>('#company-name')!;
    const website = element.querySelector<HTMLInputElement>('#company-website')!;
    name.value = 'Acme';
    name.dispatchEvent(new Event('input'));
    website.value = 'https://acme.com';
    website.dispatchEvent(new Event('input'));
    element.querySelector('app-company-form form')!.dispatchEvent(new Event('submit'));
    await render();

    const create = http.expectOne((r) => r.method === 'POST');
    expect(create.request.body).toEqual({ name: 'Acme', websiteUrl: 'https://acme.com' });
    create.flush(acme, { status: 201, statusText: 'Created' });
    await render();

    expect(text()).toContain('Acme was registered (acme.com).');
    expectSearch().flush(pageOf([acme]));
    await render();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('shows the API error message when creation fails', async () => {
    expectSearch().flush(pageOf([]));
    await render();

    const name = element.querySelector<HTMLInputElement>('#company-name')!;
    const website = element.querySelector<HTMLInputElement>('#company-website')!;
    name.value = 'Apple';
    name.dispatchEvent(new Event('input'));
    website.value = 'https://microsoft.com';
    website.dispatchEvent(new Event('input'));
    element.querySelector('app-company-form form')!.dispatchEvent(new Event('submit'));
    await render();

    http
      .expectOne((r) => r.method === 'POST')
      .flush(
        {
          status: 400,
          detail: 'The company name does not match the website.',
          errors: [{ name: 'websiteUrl', reason: 'The company name does not match the website.' }],
        },
        { status: 400, statusText: 'Bad Request' },
      );
    await render();

    expect(element.querySelector('app-alert[role="alert"]')?.textContent).toContain(
      'does not match the website',
    );
  });
});
