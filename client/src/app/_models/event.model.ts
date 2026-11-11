export const EVENT_CATEGORIES = ['Culture', 'Sports', 'Council', 'Environment', 'Other'] as const;

export type EventCategory = typeof EVENT_CATEGORIES[number];

/** Public event as returned by GET /api/events. Never contains user ids. */
export interface EventDTO {
  id: number;
  title: string;
  description: string;
  photoUrl: string | null;
  location: string | null;
  lat: number | null;
  lng: number | null;
  /** UTC ISO string ending in Z. */
  date: string;
  /** UTC ISO string ending in Z, or null. */
  endDate: string | null;
  category: EventCategory;
  interestedCount: number;
  isInterested: boolean;
}

/** Event as returned by the admin endpoints, including unpublished events. */
export interface AdminEventDTO {
  id: number;
  title: string;
  description: string;
  photoUrl: string | null;
  location: string | null;
  lat: number | null;
  lng: number | null;
  date: string;
  endDate: string | null;
  category: EventCategory;
  interestedCount: number;
  isPublished: boolean;
  createdAt: string;
}

/** Body for POST /api/admin/events and PUT /api/admin/events/{id}. */
export interface AdminEventRequest {
  title: string;
  description?: string | null;
  photoUrl?: string | null;
  location?: string | null;
  lat?: number | null;
  lng?: number | null;
  date: string;
  endDate?: string | null;
  category: EventCategory;
  isPublished: boolean;
}
