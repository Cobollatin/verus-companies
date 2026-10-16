import type { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'companies' },
  {
    path: 'companies',
    loadChildren: () =>
      import('./features/companies/companies.routes').then((m) => m.COMPANY_ROUTES),
  },
  { path: '**', redirectTo: 'companies' },
];
