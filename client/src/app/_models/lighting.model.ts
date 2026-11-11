export type LightingStatus = 'On' | 'Dimmed' | 'Off';

/** One row of GET /api/lighting/schedule. Row D is the night that starts on D's evening. */
export interface LightingScheduleDay {
  /** Local date, yyyy-MM-dd. */
  date: string;
  /** Local ISO times with the Athens offset, e.g. 2026-06-21T06:02:00+03:00. */
  sunrise: string;
  sunset: string;
  lightsOn: string;
  dimStart: string;
  dimEnd: string;
  /** Falls on the next morning. */
  lightsOff: string;
  litHours: number;
  estimatedKwh: number;
}

export interface LightingNextChange {
  at: string;
  toStatus: LightingStatus;
}

export interface LightingNow {
  status: LightingStatus;
  brightnessPercent: number;
  nextChange: LightingNextChange | null;
}
