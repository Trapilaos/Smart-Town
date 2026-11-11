/** Claims this app reads from a JWT payload. Other claims are passed through untouched. */
export interface JwtPayload {
  exp?: number;
  role?: string | string[];
  [claim: string]: unknown;
}

/**
 * Decodes the payload segment of a JWT. JWTs use base64url (`-` and `_` instead of
 * `+` and `/`, no padding), which plain `atob` rejects, so convert it first. The
 * bytes are UTF-8, so decode them as UTF-8 to keep non-ASCII usernames intact.
 * Throws when the token is malformed.
 */
export function decodeJwtPayload(token: string): JwtPayload {
  const parts = token.split('.');
  if (parts.length !== 3 || !parts[1]) {
    throw new Error('Malformed token');
  }

  let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
  base64 += '='.repeat((4 - (base64.length % 4)) % 4);

  const binary = atob(base64);
  const bytes = Uint8Array.from(binary, char => char.charCodeAt(0));
  return JSON.parse(new TextDecoder().decode(bytes)) as JwtPayload;
}

/**
 * True when the token expires within `skewSeconds` from now (or has already expired).
 * The skew stops a token from being sent just before the server rejects it.
 * A token that cannot be decoded, or has no `exp`, counts as expired.
 */
export function isTokenExpired(token: string, skewSeconds = 30): boolean {
  try {
    const exp = decodeJwtPayload(token).exp;
    if (typeof exp !== 'number') {
      return true;
    }
    return Date.now() / 1000 + skewSeconds >= exp;
  } catch {
    return true;
  }
}
