import {
  barHeightPercent,
  formatHours,
  formatLocalDateTime,
  formatLocalTime,
  formatScheduleDate,
  statusLabel
} from './lighting.helpers';

describe('lighting helpers', () => {
  describe('formatLocalTime', () => {
    it('shows Athens wall-clock time in summer (UTC+3)', () => {
      // 2026-06-21 03:02 UTC is 06:02 in Athens
      expect(formatLocalTime('2026-06-21T03:02:00Z')).toBe('06:02');
    });

    it('shows Athens wall-clock time in winter (UTC+2)', () => {
      // 2026-12-21 05:38 UTC is 07:38 in Athens
      expect(formatLocalTime('2026-12-21T05:38:00Z')).toBe('07:38');
    });

    it('converts an ISO value that carries the Athens offset', () => {
      expect(formatLocalTime('2026-06-21T20:51:00+03:00')).toBe('20:51');
    });

    it('wraps midnight into the next day without showing 24:00', () => {
      expect(formatLocalTime('2026-06-21T21:30:00Z')).toBe('00:30');
    });

    it('returns an empty string for missing or invalid values', () => {
      expect(formatLocalTime(null)).toBe('');
      expect(formatLocalTime('not a date')).toBe('');
    });
  });

  describe('formatLocalDateTime', () => {
    it('includes the weekday, day, month and time', () => {
      const text = formatLocalDateTime('2026-11-11T15:02:00Z');
      expect(text).toContain('Wed');
      expect(text).toContain('11');
      expect(text).toContain('Nov');
      expect(text).toContain('17:02');
    });
  });

  describe('formatScheduleDate', () => {
    it('formats a calendar date without shifting it by the browser time zone', () => {
      expect(formatScheduleDate('2026-11-11')).toContain('11');
      expect(formatScheduleDate('2026-11-11')).toContain('Nov');
    });

    it('returns the input unchanged when it is not yyyy-MM-dd', () => {
      expect(formatScheduleDate('11/11/2026')).toBe('11/11/2026');
    });
  });

  describe('barHeightPercent', () => {
    it('scales against the largest value', () => {
      expect(barHeightPercent(12, 24)).toBe(50);
      expect(barHeightPercent(24, 24)).toBe(100);
    });

    it('returns 0 for zero, negative, or unusable input', () => {
      expect(barHeightPercent(0, 24)).toBe(0);
      expect(barHeightPercent(5, 0)).toBe(0);
      expect(barHeightPercent(NaN, 24)).toBe(0);
    });

    it('never exceeds 100', () => {
      expect(barHeightPercent(30, 24)).toBe(100);
    });
  });

  describe('formatHours', () => {
    it('shows one decimal place', () => {
      expect(formatHours(9.5)).toBe('9.5');
      expect(formatHours(10)).toBe('10.0');
    });
  });

  it('labels each status', () => {
    expect(statusLabel('On')).toBe('On at full brightness');
    expect(statusLabel('Dimmed')).toBe('Dimmed');
    expect(statusLabel('Off')).toBe('Off');
  });
});
