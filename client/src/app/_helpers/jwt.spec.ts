import { decodeJwtPayload, isTokenExpired } from './jwt';
import { makeJwt } from './jwt.testing';

const FAR_FUTURE = 4102444800; // 2100-01-01, in seconds
const nowSeconds = () => Math.floor(Date.now() / 1000);

describe('decodeJwtPayload', () => {
  it('decodes a payload whose base64url form contains - and _', () => {
    // This username makes the payload's base64 contain '+' and '/', so base64url has '-' and '_'.
    const token = makeJwt({ unique_name: 'ü>?>ÿÿ', role: 'Member', exp: FAR_FUTURE });
    const encoded = token.split('.')[1];

    expect(encoded).toContain('-');
    expect(encoded).toContain('_');
    expect(decodeJwtPayload(token)['unique_name']).toBe('ü>?>ÿÿ');
  });

  it('decodes UTF-8 usernames correctly', () => {
    const token = makeJwt({ unique_name: 'José Müller', role: 'Member', exp: FAR_FUTURE });

    expect(decodeJwtPayload(token)['unique_name']).toBe('José Müller');
  });

  it('returns the role claim as sent', () => {
    const token = makeJwt({ unique_name: 'admin', role: ['Admin', 'Moderator'], exp: FAR_FUTURE });

    expect(decodeJwtPayload(token).role).toEqual(['Admin', 'Moderator']);
  });

  it('throws on a malformed token', () => {
    expect(() => decodeJwtPayload('not-a-token')).toThrow();
  });
});

describe('isTokenExpired', () => {
  it('is false for a token that expires well in the future', () => {
    expect(isTokenExpired(makeJwt({ exp: nowSeconds() + 3600 }))).toBeFalse();
  });

  it('is true for a token that has already expired', () => {
    expect(isTokenExpired(makeJwt({ exp: nowSeconds() - 10 }))).toBeTrue();
  });

  it('treats a token expiring inside the default 30 second skew as expired', () => {
    expect(isTokenExpired(makeJwt({ exp: nowSeconds() + 10 }))).toBeTrue();
  });

  it('respects a custom skew: a token 10 seconds from expiry is valid with no skew', () => {
    const token = makeJwt({ exp: nowSeconds() + 10 });

    expect(isTokenExpired(token, 0)).toBeFalse();
    expect(isTokenExpired(token, 30)).toBeTrue();
  });

  it('treats a token without an exp claim as expired', () => {
    expect(isTokenExpired(makeJwt({ unique_name: 'lisa' }))).toBeTrue();
  });

  it('treats an undecodable token as expired', () => {
    expect(isTokenExpired('abc.def.ghi')).toBeTrue();
  });
});
