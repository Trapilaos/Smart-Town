# Smart-Town client

Angular 22 single-page app (NgModule style) with Bootstrap and Leaflet maps. See the [root README](../README.md) for the overview, architecture and setup.

## Scripts

Run from this folder (Node 24, `npm ci` first):

| Command | What it does |
|---|---|
| `npm start` | `ng serve` on http://localhost:4200, proxying `/api` to the API on `http://localhost:5000` (see `proxy.conf.json`). Needs the API running. |
| `npm run build` | `ng build` (production configuration by default) into `dist/client`. |
| `npm run watch` | Development build in watch mode. |
| `npm test` | Karma and Jasmine tests with Chrome. |
| `npx ng test --watch=false --browsers=ChromeHeadlessCI` | The headless run that CI uses. |
