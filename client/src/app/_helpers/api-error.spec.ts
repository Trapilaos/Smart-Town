import { HttpErrorResponse } from '@angular/common/http';
import { FALLBACK_ERROR_MESSAGE, getErrorMessages } from './api-error';

function httpError(body: unknown, status = 400): HttpErrorResponse {
  return new HttpErrorResponse({ error: body, status, statusText: 'Bad Request', url: '/api/x' });
}

describe('getErrorMessages', () => {
  it('returns a plain string body as a single message', () => {
    expect(getErrorMessages(httpError('Username is taken!'))).toEqual(['Username is taken!']);
  });

  it('reads the description of each Identity error', () => {
    const body = [
      { code: 'PasswordRequiresLower', description: "Passwords must have at least one lowercase ('a'-'z')." },
      { code: 'PasswordRequiresDigit', description: "Passwords must have at least one digit ('0'-'9')." }
    ];

    expect(getErrorMessages(httpError(body))).toEqual([
      "Passwords must have at least one lowercase ('a'-'z').",
      "Passwords must have at least one digit ('0'-'9')."
    ]);
  });

  it('accepts an array of plain strings and of objects with a message', () => {
    expect(getErrorMessages(['first', { message: 'second' }])).toEqual(['first', 'second']);
  });

  it('flattens ASP.NET validation errors from every field', () => {
    const body = {
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: {
        Username: ['Required'],
        Password: ['Too short', 'Too weak']
      }
    };

    expect(getErrorMessages(httpError(body))).toEqual(['Required', 'Too short', 'Too weak']);
  });

  it('reads the message of an object body such as { message }', () => {
    expect(getErrorMessages(httpError({ message: 'Failed to reserve parking space' }))).toEqual([
      'Failed to reserve parking space'
    ]);
  });

  it('reads the message of an ApiException body', () => {
    expect(getErrorMessages(httpError({ statusCode: 400, message: 'Bad input', details: null }))).toEqual([
      'Bad input'
    ]);
  });

  it('accepts a raw body as well as an HttpErrorResponse', () => {
    expect(getErrorMessages('Invalid username or password')).toEqual(['Invalid username or password']);
  });

  it('falls back to a generic message when nothing usable is present', () => {
    expect(getErrorMessages(httpError(null, 500))).toEqual([FALLBACK_ERROR_MESSAGE]);
    expect(getErrorMessages(httpError({ unexpected: true }))).toEqual([FALLBACK_ERROR_MESSAGE]);
    expect(getErrorMessages(httpError('   '))).toEqual([FALLBACK_ERROR_MESSAGE]);
    expect(getErrorMessages(undefined)).toEqual([FALLBACK_ERROR_MESSAGE]);
  });

  it('trims messages and removes duplicates', () => {
    const body = {
      errors: {
        Password: ['  Too short  ', 'Too short'],
        ConfirmPassword: ['Too short', '   ']
      }
    };

    expect(getErrorMessages(httpError(body))).toEqual(['Too short']);
  });

  it('never returns an empty array', () => {
    expect(getErrorMessages(httpError([]))).toEqual([FALLBACK_ERROR_MESSAGE]);
    expect(getErrorMessages(httpError({ errors: {} }))).toEqual([FALLBACK_ERROR_MESSAGE]);
  });
});
