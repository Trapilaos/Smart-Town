/**
 * Test-only helper: builds an unsigned JWT the way the API does, with a base64url
 * payload (no padding, `-` and `_` in place of `+` and `/`) and UTF-8 bytes.
 * Not imported by app code, so it stays out of the production bundle.
 */
export function makeJwt(payload: object): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  const binary = Array.from(bytes, byte => String.fromCharCode(byte)).join('');
  const encoded = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `eyJhbGciOiJub25lIn0.${encoded}.sig`;
}
