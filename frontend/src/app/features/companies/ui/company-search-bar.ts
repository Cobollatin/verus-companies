import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { outputFromObservable } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { map } from 'rxjs';

import type { CompanySearchCriteria, CompanySort } from '../data-access/company.models';

export type CompanyFilters = Omit<CompanySearchCriteria, 'page' | 'pageSize'>;

/** Filter inputs. Emits on every change; debouncing is the container's decision. */
@Component({
  selector: 'app-company-search-bar',
  imports: [ReactiveFormsModule],
  templateUrl: './company-search-bar.html',
  styleUrl: './company-search-bar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanySearchBar {
  protected readonly form = inject(NonNullableFormBuilder).group({
    q: [''],
    name: [''],
    domain: [''],
    sort: new FormControl<CompanySort | ''>('', { nonNullable: true }),
  });

  readonly filtersChange = outputFromObservable(
    this.form.valueChanges.pipe(
      map((): CompanyFilters => {
        const { q, name, domain, sort } = this.form.getRawValue();
        return {
          ...(q.trim() ? { q: q.trim() } : {}),
          ...(name.trim() ? { name: name.trim() } : {}),
          ...(domain.trim() ? { domain: domain.trim() } : {}),
          ...(sort && (sort !== 'relevance' || q.trim()) ? { sort } : {}),
        };
      }),
    ),
  );
}
