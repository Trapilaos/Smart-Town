import { EventDTO } from '../_models/event.model';

/** An event is past once its end (or its start, when there is no end) is before `now`. */
export function isPastEvent(event: Pick<EventDTO, 'date' | 'endDate'>, now: Date = new Date()): boolean {
  const end = new Date(event.endDate ?? event.date);
  return !Number.isNaN(end.getTime()) && end.getTime() < now.getTime();
}

/** Interest button label for a logged-in user. */
export function interestLabel(isInterested: boolean): string {
  return isInterested ? 'Withdraw' : 'I\'m interested';
}
