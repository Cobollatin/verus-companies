import { DatePipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import type { Company } from '../data-access/company.models';

@Component({
  selector: 'app-company-table',
  imports: [RouterLink, DatePipe, PercentPipe],
  templateUrl: './company-table.html',
  styleUrl: './company-table.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanyTable {
  readonly companies = input.required<readonly Company[]>();
  /** Show the relevance column (relevance-ranked searches only). */
  readonly showRelevance = input(false);
}
