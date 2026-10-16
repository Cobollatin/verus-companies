import { HttpErrorResponse } from '@angular/common/http';

import { toApiError } from './api-error';

describe('toApiError', () => {
  it('maps problem details to a concise message and per-field errors', () => {
    const response = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Validation failed',
        status: 400,
        detail: 'Company name is required.',
        errors: [
          { name: 'name', reason: 'Company name is required.', code: 'Company.Name.Required' },
          { name: 'websiteUrl', reason: 'Website URL is required.' },
          { name: 'generalErrors', reason: 'Ignored for fields.' },
        ],
      },
    });

    expect(toApiError(response)).toEqual({
      status: 400,
      title: 'Validation failed',
      message: 'Company name is required.',
      fieldErrors: {
        name: ['Company name is required.'],
        websiteUrl: ['Website URL is required.'],
      },
    });
  });

  it('explains network failures', () => {
    const error = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(error.status).toBe(0);
    expect(error.message).toContain('Cannot reach the server');
  });

  it('falls back to a generic message for non-problem bodies', () => {
    const error = toApiError(
      new HttpErrorResponse({ status: 502, error: '<html>Bad gateway</html>' }),
    );

    expect(error.message).toBe('Request failed (502).');
    expect(error.fieldErrors).toEqual({});
  });

  it('passes through errors that are already normalized', () => {
    const apiError = { status: 404, title: 'Not found', message: 'Nope', fieldErrors: {} };

    expect(toApiError(apiError)).toBe(apiError);
  });
});
