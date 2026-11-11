/** Mirrors GET /api/municipality (camelCase JSON). */
export interface Municipality {
  name: string;
  centerLatitude: number;
  centerLongitude: number;
  defaultZoom: number;
  timeZoneId: string;
  boundary: GeoJSON.Geometry | null;
  boundaryIsApproximate: boolean;
  boundarySource: string | null;
  boundaryFetchedAt: string | null;
  streetlightCount: number;
  streetlightWatts: number;
  updatedAt: string;
}

/** Body for PUT /api/admin/municipality. The boundary is optional: omit it to keep the stored one. */
export interface MunicipalityUpdateRequest {
  name: string;
  centerLatitude: number;
  centerLongitude: number;
  defaultZoom: number;
  streetlightCount: number;
  streetlightWatts: number;
  boundary?: GeoJSON.Geometry;
}

/** A candidate returned by POST /api/admin/municipality/boundary/search. */
export interface BoundaryCandidate {
  osmType: string;
  osmId: number;
  displayName: string;
  centerLat: number;
  centerLng: number;
  geometry: GeoJSON.Geometry;
}
