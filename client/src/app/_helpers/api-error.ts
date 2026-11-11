import { HttpContextToken, HttpErrorResponse } from '@angular/common/http';

/** Set on a request to stop the ErrorInterceptor from toasting its error (the caller shows it instead). */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export const FALLBACK_ERROR_MESSAGE = 'Something went wrong. Please try again.';

/**
 * Turns any error from the API into a list of user-facing messages.
 * Accepts an HttpErrorResponse or a raw response body. Handles:
 *  - a plain string
 *  - an array of strings or of objects with `description` / `message` (Identity errors)
 *  - an object with `errors` whose values are strings or string arrays (ASP.NET validation)
 *  - an object with `message` (ApiException, custom errors)
 */
export function getErrorMessages(error: unknown): string[] {
  const body = error instanceof HttpErrorResponse ? error.error : error;
  const messages = collectMessages(body)
    .map(message => message.trim())
    .filter(message => message.length > 0);
  const unique = [...new Set(messages)];
  return unique.length > 0 ? unique : [FALLBACK_ERROR_MESSAGE];
}

function collectMessages(body: unknown): string[] {
  if (typeof body === 'string') {
    return [body];
  }

  if (Array.isArray(body)) {
    return body.flatMap(item => {
      if (typeof item === 'string') return [item];
      if (item && typeof item === 'object') {
        const text = (item as { description?: unknown; message?: unknown });
        const value = typeof text.description === 'string' ? text.description : text.message;
        return typeof value === 'string' ? [value] : [];
      }
      return [];
    });
  }

  if (body && typeof body === 'object') {
    const record = body as { errors?: unknown; message?: unknown };

    if (record.errors && typeof record.errors === 'object') {
      const fromErrors = Object.values(record.errors as Record<string, unknown>).flatMap(value => {
        if (Array.isArray(value)) return value.filter((v): v is string => typeof v === 'string');
        return typeof value === 'string' ? [value] : [];
      });
      if (fromErrors.length > 0) return fromErrors;
    }

    if (typeof record.message === 'string') {
      return [record.message];
    }
  }

  return [];
}
