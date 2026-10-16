import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  BehaviorSubject,
  Subject,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  exhaustMap,
  filter,
  map,
  merge,
  share,
  skip,
  startWith,
  switchMap,
  take,
} from 'rxjs';

import type { RequestState } from '../../../shared/data/request-state';
import { idle, pending, toRequestState } from '../../../shared/data/request-state';
import { Alert } from '../../../shared/ui/alert';
import { Pager } from '../../../shared/ui/pager';
import { CompaniesApi } from '../data-access/companies-api';
import type {
  Company,
  CompanySearchCriteria,
  CreateCompanyRequest,
  Page,
} from '../data-access/company.models';
import { CompanyForm } from '../ui/company-form';
import type { CompanyFilters } from '../ui/company-search-bar';
import { CompanySearchBar } from '../ui/company-search-bar';
import { CompanyTable } from '../ui/company-table';

export const PAGE_SIZE = 10;
export const SEARCH_DEBOUNCE_MS = 300;

const sameFilters = (a: CompanyFilters, b: CompanyFilters): boolean =>
  a.q === b.q && a.name === b.name && a.domain === b.domain && a.sort === b.sort;

/**
 * Container: wires the API to the presentational components with declarative RxJS streams,
 * exposed to the template as signals.
 */
@Component({
  selector: 'app-companies-page',
  imports: [Alert, CompanyForm, CompanySearchBar, CompanyTable, Pager],
  templateUrl: './companies-page.html',
  styleUrl: './companies-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompaniesPage {
  private readonly api = inject(CompaniesApi);

  private readonly filters$ = new BehaviorSubject<CompanyFilters>({});
  private readonly pageChanges$ = new Subject<number>();
  private readonly submissions$ = new Subject<CreateCompanyRequest>();

  // Create: ignore re-submits while a request is in flight.
  private readonly create$ = this.submissions$.pipe(
    exhaustMap((request) => this.api.create(request).pipe(toRequestState())),
    share(),
  );
  private readonly created$ = this.create$.pipe(filter((state) => state.kind === 'success'));

  // Search: first load is immediate, typing is debounced, a filter change resets to page 1,
  // a successful create re-runs the current search, and stale requests are cancelled.
  private readonly debouncedFilters$ = merge(
    this.filters$.pipe(take(1)),
    this.filters$.pipe(skip(1), debounceTime(SEARCH_DEBOUNCE_MS)),
  ).pipe(distinctUntilChanged(sameFilters));

  private readonly criteria$ = this.debouncedFilters$.pipe(
    switchMap((filters) =>
      this.pageChanges$.pipe(
        startWith(1),
        map((page): CompanySearchCriteria => ({ ...filters, page, pageSize: PAGE_SIZE })),
      ),
    ),
  );

  protected readonly createState = toSignal(this.create$, { initialValue: idle<Company>() });

  protected readonly results = toSignal(
    combineLatest([this.criteria$, this.created$.pipe(startWith(null))]).pipe(
      switchMap(([criteria]) => this.api.search(criteria).pipe(toRequestState())),
    ),
    { initialValue: pending<Page<Company>>() },
  );

  private readonly query = signal('');
  protected readonly showRelevance = computed(() => this.query() !== '');

  /** Last successfully loaded page, kept on screen while the next one loads (no flicker). */
  protected readonly lastPage = linkedSignal<RequestState<Page<Company>>, Page<Company> | null>({
    source: this.results,
    computation: (state, previous) =>
      state.kind === 'success' ? state.value : (previous?.value ?? null),
  });

  protected create(request: CreateCompanyRequest): void {
    this.submissions$.next(request);
  }

  protected filter(filters: CompanyFilters): void {
    this.query.set(filters.q ?? '');
    this.filters$.next(filters);
  }

  protected goToPage(page: number): void {
    this.pageChanges$.next(page);
  }
}
