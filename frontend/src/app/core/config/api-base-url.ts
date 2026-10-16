import { InjectionToken } from '@angular/core';

/**
 * Base URL of the versioned API. Defaults to a same-origin path: the dev server proxies `/api` to the
 * backend (see `proxy.conf.json`), and production is expected to sit behind the same reverse proxy.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api/v1',
});
