import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { pending, toRequestState } from '../../../shared/data/request-state';
import { Alert } from '../../../shared/ui/alert';
import { CompaniesApi } from '../data-access/companies-api';
import type { Company } from '../data-access/company.models';

@Component({
  selector: 'app-company-detail-page',
  imports: [Alert, DatePipe, RouterLink],
  template: `
    <section class="card">
      <a routerLink="/companies">← All companies</a>

      @let state = company();
      @switch (state.kind) {
        @case ('pending') {
          <p class="hint" aria-live="polite">Loading…</p>
        }
        @case ('error') {
          <h2>{{ state.error.status === 404 ? 'Company not found' : 'Something went wrong' }}</h2>
          <app-alert kind="error">{{ state.error.message }}</app-alert>
        }
        @case ('success') {
          <h2>{{ state.value.name }}</h2>
          <dl>
            <dt>Website</dt>
            <dd>
              <a [href]="state.value.websiteUrl" target="_blank" rel="noopener noreferrer">{{
                state.value.websiteUrl
              }}</a>
            </dd>
            <dt>Domain</dt>
            <dd>{{ state.value.domain }}</dd>
            <dt>Added</dt>
            <dd>{{ state.value.createdAt | date: 'full' }}</dd>
            <dt>Id</dt>
            <dd>
              <code>{{ state.value.id }}</code>
            </dd>
          </dl>
        }
      }
    </section>
  `,
  styles: `
    dl {
      display: grid;
      grid-template-columns: max-content 1fr;
      gap: 0.5rem 1.5rem;
    }
    dt {
      color: var(--muted);
    }
    dd {
      margin: 0;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanyDetailPage {
  private readonly api = inject(CompaniesApi);

  /** Bound from the `:id` route parameter (`withComponentInputBinding`). */
  readonly id = input.required<string>();

  protected readonly company = toSignal(
    toObservable(this.id).pipe(switchMap((id) => this.api.getById(id).pipe(toRequestState()))),
    { initialValue: pending<Company>() },
  );
}
