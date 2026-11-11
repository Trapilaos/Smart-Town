import {
  athensInputToUtcIso,
  emptyEventForm,
  EventFormModel,
  toEventRequest,
  utcIsoToAthensInput,
  validateEventForm
} from './event-form.helpers';

describe('event form helpers', () => {
  const validModel = (overrides: Partial<EventFormModel> = {}): EventFormModel => ({
    ...emptyEventForm(),
    title: 'Summer concert',
    category: 'Culture',
    date: '2026-07-01T20:00',
    ...overrides
  });

  describe('Athens time conversion', () => {
    it('converts a summer Athens time (UTC+3) to UTC', () => {
      expect(athensInputToUtcIso('2026-07-01T20:00')).toBe('2026-07-01T17:00:00Z');
    });

    it('converts a winter Athens time (UTC+2) to UTC', () => {
      expect(athensInputToUtcIso('2026-12-01T20:00')).toBe('2026-12-01T18:00:00Z');
    });

    it('returns null for malformed input', () => {
      expect(athensInputToUtcIso('')).toBeNull();
      expect(athensInputToUtcIso('2026-07-01 20:00')).toBeNull();
    });

    it('shows a UTC instant as Athens wall-clock time for the input', () => {
      expect(utcIsoToAthensInput('2026-07-01T17:00:00Z')).toBe('2026-07-01T20:00');
      expect(utcIsoToAthensInput('2026-12-01T18:00:00Z')).toBe('2026-12-01T20:00');
    });

    it('round-trips through UTC', () => {
      const utc = athensInputToUtcIso('2026-03-29T12:30');
      expect(utc).not.toBeNull();
      expect(utcIsoToAthensInput(utc)).toBe('2026-03-29T12:30');
    });

    it('returns an empty input value for a missing or invalid instant', () => {
      expect(utcIsoToAthensInput(null)).toBe('');
      expect(utcIsoToAthensInput('nope')).toBe('');
    });
  });

  describe('validateEventForm', () => {
    it('accepts a minimal valid event', () => {
      expect(validateEventForm(validModel())).toEqual({});
    });

    it('requires a title and trims it', () => {
      expect(validateEventForm(validModel({ title: '   ' })).title).toBeDefined();
    });

    it('rejects a title over 200 characters', () => {
      expect(validateEventForm(validModel({ title: 'x'.repeat(201) })).title).toBeDefined();
      expect(validateEventForm(validModel({ title: 'x'.repeat(200) })).title).toBeUndefined();
    });

    it('requires a category', () => {
      expect(validateEventForm(validModel({ category: '' })).category).toBeDefined();
    });

    it('accepts only https photo links', () => {
      expect(validateEventForm(validModel({ photoUrl: 'http://example.com/a.jpg' })).photoUrl).toBeDefined();
      expect(validateEventForm(validModel({ photoUrl: 'not a url' })).photoUrl).toBeDefined();
      expect(validateEventForm(validModel({ photoUrl: 'https://example.com/a.jpg' })).photoUrl).toBeUndefined();
      expect(validateEventForm(validModel({ photoUrl: '' })).photoUrl).toBeUndefined();
    });

    it('requires latitude and longitude together', () => {
      expect(validateEventForm(validModel({ lat: 37.9 })).coordinates).toBeDefined();
      expect(validateEventForm(validModel({ lng: 23.7 })).coordinates).toBeDefined();
      expect(validateEventForm(validModel({ lat: 37.9, lng: 23.7 })).coordinates).toBeUndefined();
      expect(validateEventForm(validModel()).coordinates).toBeUndefined();
    });

    it('requires a start date', () => {
      expect(validateEventForm(validModel({ date: '' })).date).toBeDefined();
    });

    it('requires the end to be on or after the start', () => {
      expect(validateEventForm(validModel({ endDate: '2026-06-30T20:00' })).endDate).toBeDefined();
      expect(validateEventForm(validModel({ endDate: '2026-07-01T20:00' })).endDate).toBeUndefined();
      expect(validateEventForm(validModel({ endDate: '2026-07-02T10:00' })).endDate).toBeUndefined();
    });
  });

  describe('toEventRequest', () => {
    it('sends UTC times and nulls for empty optional fields', () => {
      const request = toEventRequest(validModel({ description: '  ', location: '', photoUrl: '' }));
      expect(request.title).toBe('Summer concert');
      expect(request.date).toBe('2026-07-01T17:00:00Z');
      expect(request.endDate).toBeNull();
      expect(request.description).toBeNull();
      expect(request.location).toBeNull();
      expect(request.photoUrl).toBeNull();
      expect(request.lat).toBeNull();
      expect(request.lng).toBeNull();
      expect(request.isPublished).toBeFalse();
    });

    it('keeps coordinates and the published flag', () => {
      const request = toEventRequest(validModel({ lat: 37.9, lng: 23.7, isPublished: true, endDate: '2026-07-01T22:00' }));
      expect(request.lat).toBe(37.9);
      expect(request.lng).toBe(23.7);
      expect(request.isPublished).toBeTrue();
      expect(request.endDate).toBe('2026-07-01T19:00:00Z');
    });
  });
});
