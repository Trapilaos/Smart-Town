import { LightingStatus } from '../_models/lighting.model';

export const MUNICIPALITY_TIME_ZONE = 'Europe/Athens';

const timeFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: MUNICIPALITY_TIME_ZONE,
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23'
});

const dateFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'UTC',
  weekday: 'short',
  day: 'numeric',
  month: 'short'
});

const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: MUNICIPALITY_TIME_ZONE,
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23'
});

/**
 * Local time (HH:mm) in the municipality's time zone for an ISO instant.
 * Values with an offset are converted, so the result is correct wherever the browser is.
 * Returns an empty string for an invalid value.
 */
export function formatLocalTime(iso: string | null | undefined): string {
  const date = parseInstant(iso);
  return date ? timeFormat.format(date) : '';
}

/** Local date and time in the municipality's time zone, e.g. "Tue 11 Nov, 17:02". */
export function formatLocalDateTime(iso: string | null | undefined): string {
  const date = parseInstant(iso);
  return date ? dateTimeFormat.format(date) : '';
}

/** Schedule row date (yyyy-MM-dd) as "Tue 11 Nov". The date is a calendar day, so it is formatted in UTC. */
export function formatScheduleDate(date: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
  if (!match) return date;
  const utc = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  return dateFormat.format(utc);
}

/** Lit hours to a readable value with one decimal place. */
export function formatHours(hours: number): string {
  return hours.toFixed(1);
}

/** Bar height as a percentage of the largest value, clamped to 0..100. */
export function barHeightPercent(value: number, max: number): number {
  if (!Number.isFinite(value) || !Number.isFinite(max) || max <= 0 || value <= 0) return 0;
  return Math.min(100, Math.round((value / max) * 1000) / 10);
}

/** Plain-language label for a lighting status. */
export function statusLabel(status: LightingStatus): string {
  switch (status) {
    case 'On': return 'On at full brightness';
    case 'Dimmed': return 'Dimmed';
    default: return 'Off';
  }
}

/** Bootstrap colour class for a lighting status. */
export function statusBadgeClass(status: LightingStatus): string {
  switch (status) {
    case 'On': return 'bg-warning text-on-warning';
    case 'Dimmed': return 'bg-info text-on-warning';
    default: return 'bg-secondary';
  }
}

function parseInstant(iso: string | null | undefined): Date | null {
  if (!iso) return null;
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? null : date;
}
