import type { OperatorFunction } from 'rxjs';
import { catchError, map, of, startWith } from 'rxjs';

import type { ApiError } from '../../core/http/api-error';
import { toApiError } from '../../core/http/api-error';

/** Lifecycle of an async request as a discriminated union: illegal combinations are unrepresentable. */
export type RequestState<T> =
  | { readonly kind: 'idle' }
  | { readonly kind: 'pending' }
  | { readonly kind: 'success'; readonly value: T }
  | { readonly kind: 'error'; readonly error: ApiError };

export const idle = <T>(): RequestState<T> => ({ kind: 'idle' });
export const pending = <T>(): RequestState<T> => ({ kind: 'pending' });
export const success = <T>(value: T): RequestState<T> => ({ kind: 'success', value });
export const failure = <T>(error: ApiError): RequestState<T> => ({ kind: 'error', error });

/** Maps a request observable to `pending → success | error`, never erroring the outer stream. */
export function toRequestState<T>(): OperatorFunction<T, RequestState<T>> {
  return (source) =>
    source.pipe(
      map((value) => success(value)),
      catchError((error: unknown) => of(failure<T>(toApiError(error)))),
      startWith(pending<T>()),
    );
}
