/** Bin types, as the API stores them. */
export const WASTE_TYPES = ['General', 'Recycling', 'Glass', 'Organic', 'Paper'] as const;
export type WasteType = typeof WASTE_TYPES[number];

/** Mirrors GET /api/waste/bins and the admin endpoints (camelCase JSON). */
export interface WasteBin {
  id: number;
  location: string;
  type: WasteType;
  latitude: number;
  longitude: number;
  capacity: number;
  currentFillLevel: number;
  fillPercent: number;
  lastEmptied: string | null;
  isActive: boolean;
}

/** Body for POST and PUT /api/admin/waste/bins. */
export interface WasteBinRequest {
  location: string;
  type: WasteType;
  latitude: number;
  longitude: number;
  capacity: number;
  currentFillLevel: number;
}

/** One stop from GET /api/admin/waste/route. */
export interface CollectionStop {
  binId: number;
  location: string;
  type: WasteType;
  lat: number;
  lng: number;
  fillPercent: number;
  order: number;
}

export interface CollectionRoute {
  stops: CollectionStop[];
  totalDistanceKm: number;
  depot: { lat: number; lng: number };
}
