/** Parking space types, as the API stores them. */
export const PARKING_TYPES = ['Street', 'Lot', 'Disabled', 'EV'] as const;
export type ParkingType = typeof PARKING_TYPES[number];

/** Mirrors GET /api/parking and GET /api/admin/parking (camelCase JSON). */
export interface ParkingSpace {
  id: number;
  name: string;
  address: string | null;
  type: ParkingType;
  latitude: number;
  longitude: number;
  hourlyRate: number;
  maxVehicles: number;
  currentVehicles: number;
  /** Spaces free right now (max minus reservations active at this moment). */
  freeNow: number;
  isActive: boolean;
}

/** Body for POST and PUT /api/admin/parking. */
export interface ParkingSpaceRequest {
  name: string;
  address: string | null;
  type: ParkingType;
  latitude: number;
  longitude: number;
  maxVehicles: number;
  hourlyRate: number;
  isActive: boolean;
}

/** Body for POST /api/parking/reserve. `reservationTime` is a UTC ISO string. */
export interface ReservationRequest {
  parkingSpaceId: number;
  reservationTime: string;
  duration: number;
}

/** The member's active reservation from GET /api/parking/active-reservation. */
export interface ActiveReservation {
  parkingSpaceId: number;
  reservationTime: string;
  duration: number;
}

/** One row from GET /api/admin/parking/{id}/reservations. Usernames only, never contact details. */
export interface ParkingReservation {
  id: number;
  username: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
}
