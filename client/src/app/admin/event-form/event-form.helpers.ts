import { AdminEventDTO, AdminEventRequest, EVENT_CATEGORIES, EventCategory } from '../../_models/event.model';

export const EVENT_TIME_ZONE = 'Europe/Athens';

/** Form state for the event form. Dates are "yyyy-MM-ddTHH:mm" wall-clock times in Athens. */
export interface EventFormModel {
  title: string;
  description: string;
  category: EventCategory | '';
  location: string;
  lat: number | null;
  lng: number | null;
  date: string;
  endDate: string;
  photoUrl: string;
  isPublished: boolean;
}

export type EventFormErrors = Partial<Record<'title' | 'category' | 'photoUrl' | 'location' | 'coordinates' | 'date' | 'endDate', string>>;

const wallFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: EVENT_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23'
});

const inputPattern = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

/** Athens wall-clock time of an instant, as if it were UTC (milliseconds). */
function athensWallMs(utcMs: number): number {
  const parts: Record<string, string> = {};
  for (const part of wallFormat.formatToParts(new Date(utcMs))) {
    parts[part.type] = part.value;
  }
  return Date.UTC(
    Number(parts['year']), Number(parts['month']) - 1, Number(parts['day']),
    Number(parts['hour']), Number(parts['minute'])
  );
}

/** Converts a UTC ISO instant from the API to an Athens "yyyy-MM-ddTHH:mm" input value. */
export function utcIsoToAthensInput(iso: string | null | undefined): string {
  if (!iso) return '';
  const ms = Date.parse(iso);
  if (Number.isNaN(ms)) return '';
  return new Date(athensWallMs(ms)).toISOString().slice(0, 16);
}

/** Converts an Athens "yyyy-MM-ddTHH:mm" input value to a UTC ISO string ending in Z. Null when the value is not valid. */
export function athensInputToUtcIso(value: string): string | null {
  const match = inputPattern.exec(value);
  if (!match) return null;

  const wall = Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3]), Number(match[4]), Number(match[5]));
  if (Number.isNaN(wall)) return null;

  // The Athens offset differs between winter and summer, so correct the first guess once.
  const firstGuess = wall - (athensWallMs(wall) - wall);
  const utc = wall - (athensWallMs(firstGuess) - firstGuess);
  return new Date(utc).toISOString().replace('.000Z', 'Z');
}

/** Checks the form the way the API does. Returns one message per field; an empty object means valid. */
export function validateEventForm(model: EventFormModel): EventFormErrors {
  const errors: EventFormErrors = {};

  const title = model.title.trim();
  if (title.length === 0) errors.title = 'Enter a title.';
  else if (title.length > 200) errors.title = 'The title can be at most 200 characters.';

  if (!model.category || !(EVENT_CATEGORIES as readonly string[]).includes(model.category)) {
    errors.category = 'Choose a category.';
  }

  const photo = model.photoUrl.trim();
  if (photo.length > 0 && !isHttpsUrl(photo)) {
    errors.photoUrl = 'The photo address must be a full https:// link.';
  }

  if (model.location.trim().length > 200) {
    errors.location = 'The location can be at most 200 characters.';
  }

  const hasLat = model.lat !== null;
  const hasLng = model.lng !== null;
  if (hasLat !== hasLng) {
    errors.coordinates = 'Pick a point on the map, or clear it. Latitude and longitude are set together.';
  } else if (hasLat && (model.lat! < -90 || model.lat! > 90 || model.lng! < -180 || model.lng! > 180)) {
    errors.coordinates = 'The picked point is outside the valid range.';
  }

  if (!model.date) {
    errors.date = 'Enter a start date and time.';
  } else if (athensInputToUtcIso(model.date) === null) {
    errors.date = 'Enter a valid start date and time.';
  }

  if (model.endDate) {
    if (athensInputToUtcIso(model.endDate) === null) {
      errors.endDate = 'Enter a valid end date and time.';
    } else if (model.date && model.endDate < model.date) {
      errors.endDate = 'The end must be on or after the start.';
    }
  }

  return errors;
}

/** Builds the API body from a valid form. Empty optional fields become null. */
export function toEventRequest(model: EventFormModel): AdminEventRequest {
  const date = athensInputToUtcIso(model.date);
  if (!date || !model.category) {
    throw new Error('The event form must be validated before it is converted.');
  }

  return {
    title: model.title.trim(),
    description: model.description.trim() || null,
    photoUrl: model.photoUrl.trim() || null,
    location: model.location.trim() || null,
    lat: model.lat,
    lng: model.lng,
    date,
    endDate: model.endDate ? athensInputToUtcIso(model.endDate) : null,
    category: model.category,
    isPublished: model.isPublished
  };
}

/** Form values for an existing event. */
export function formFromEvent(event: AdminEventDTO): EventFormModel {
  return {
    title: event.title,
    description: event.description ?? '',
    category: event.category,
    location: event.location ?? '',
    lat: event.lat,
    lng: event.lng,
    date: utcIsoToAthensInput(event.date),
    endDate: utcIsoToAthensInput(event.endDate),
    photoUrl: event.photoUrl ?? '',
    isPublished: event.isPublished
  };
}

/** A blank form for a new event. A new event starts unpublished, and the start date is left for the admin to pick. */
export function emptyEventForm(): EventFormModel {
  return {
    title: '',
    description: '',
    category: '',
    location: '',
    lat: null,
    lng: null,
    date: '',
    endDate: '',
    photoUrl: '',
    isPublished: false
  };
}

function isHttpsUrl(value: string): boolean {
  try {
    return new URL(value).protocol === 'https:';
  } catch {
    return false;
  }
}
