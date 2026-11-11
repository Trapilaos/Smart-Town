import { isPastEvent } from './event.helpers';

describe('isPastEvent', () => {
  const now = new Date('2026-11-11T12:00:00Z');

  it('treats an event that ended before now as past', () => {
    expect(isPastEvent({ date: '2026-11-01T10:00:00Z', endDate: '2026-11-01T18:00:00Z' }, now)).toBeTrue();
  });

  it('treats an event whose end is still ahead as upcoming, even if it has started', () => {
    expect(isPastEvent({ date: '2026-11-11T10:00:00Z', endDate: '2026-11-12T18:00:00Z' }, now)).toBeFalse();
  });

  it('uses the start when there is no end date', () => {
    expect(isPastEvent({ date: '2026-11-10T10:00:00Z', endDate: null }, now)).toBeTrue();
    expect(isPastEvent({ date: '2026-11-20T10:00:00Z', endDate: null }, now)).toBeFalse();
  });
});
