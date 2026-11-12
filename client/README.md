# Smart-Town client

Angular 22 single-page app (NgModule style) with Bootstrap and Leaflet maps. See the [root README](../README.md) for the overview, architecture and setup.

## Scripts

Run from this folder (Node 24, `npm ci` first):

| Command | What it does |
|---|---|
| `npm start` | `ng serve` on port 4200, proxying `/api` to `https://localhost:5001` (see `proxy.conf.json`). Needs the API running. |
| `npm run build` | `ng build` (production configuration by default) into `dist/client`. |
| `npm run watch` | Development build in watch mode. |
| `npm test` | Karma and Jasmine tests with Chrome. |
| `npx ng test --watch=false --browsers=ChromeHeadlessCI` | The headless run that CI uses. |

`npm start` is configured for TLS and expects `ssl/localhost-key.pem`, which is not in the repository. Generate a pair, or run `npm start -- --ssl false`.
