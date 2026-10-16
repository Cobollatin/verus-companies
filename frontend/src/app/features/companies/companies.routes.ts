import type { Routes } from '@angular/router';

import { CompaniesPage } from './feature/companies-page';
import { CompanyDetailPage } from './feature/company-detail-page';

export const COMPANY_ROUTES: Routes = [
  { path: '', component: CompaniesPage, title: 'Companies · Verus' },
  { path: ':id', component: CompanyDetailPage, title: 'Company · Verus' },
];
