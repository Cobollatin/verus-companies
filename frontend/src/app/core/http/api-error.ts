import { HttpErrorResponse } from '@angular/common/http';

/** A single field-level error as returned by the API (RFC 9457 problem details, FastEndpoints shape). */
interface ProblemDetailsError {
  readonly name: string;
  readonly reason: string;
  readonly code?: string;
}

interface ProblemDetailsBody {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly errors?: readonly ProblemDetailsError[];
}

/** Normalized error every feature consumes; never leaks `HttpErrorResponse` past the HTTP layer. */
export interface ApiError {
  readonly status: number;
  readonly title: string;
  /** Concise, user-facing message. */
  readonly message: string;
  /** Messages per input field (camelCase, matching request DTO properties). */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

const GENERAL_ERRORS_FIELD = 'generalErrors';

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'message' in value &&
    'fieldErrors' in value
  );
}

export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, title: 'Error', message: 'Something went wrong.', fieldErrors: {} };
  }

  if (error.status === 0) {
    return {
      status: 0,
      title: 'Network error',
      message: 'Cannot reach the server. Is the API running?',
      fieldErrors: {},
    };
  }

  const body = isProblemDetails(error.error) ? error.error : {};
  const fieldErrors: Record<string, string[]> = {};
  for (const { name, reason } of body.errors ?? []) {
    if (name !== GENERAL_ERRORS_FIELD) {
      (fieldErrors[name] ??= []).push(reason);
    }
  }

  return {
    status: error.status,
    title: body.title ?? 'Request failed',
    message: body.detail ?? body.errors?.[0]?.reason ?? `Request failed (${String(error.status)}).`,
    fieldErrors,
  };
}

function isProblemDetails(value: unknown): value is ProblemDetailsBody {
  return typeof value === 'object' && value !== null && ('detail' in value || 'errors' in value);
}
