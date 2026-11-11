import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { LightingService } from '../_services/lighting.service';
import { LightingNow, LightingScheduleDay, LightingStatus } from '../_models/lighting.model';
import { getErrorMessages } from '../_helpers/api-error';
import {
  barHeightPercent,
  formatHours,
  formatLocalDateTime,
  formatLocalTime,
  formatScheduleDate,
  statusBadgeClass,
  statusLabel
} from './lighting.helpers';

/** Public street lighting page: the current status, a 7-day schedule, and how it is calculated. */
@Component({
    selector: 'app-lighting',
    templateUrl: './lighting.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LightingComponent implements OnInit {
  readonly days = 7;

  now: LightingNow | null = null;
  schedule: LightingScheduleDay[] = [];

  nowLoading = true;
  scheduleLoading = true;
  nowError: string | null = null;
  scheduleError: string | null = null;

  maxLitHours = 0;

  readonly formatLocalTime = formatLocalTime;
  readonly formatLocalDateTime = formatLocalDateTime;
  readonly formatScheduleDate = formatScheduleDate;
  readonly formatHours = formatHours;
  readonly statusLabel = statusLabel;
  readonly statusBadgeClass = statusBadgeClass;

  constructor(private lightingService: LightingService) {}

  ngOnInit(): void {
    this.loadNow();
    this.loadSchedule();
  }

  loadNow(): void {
    this.nowLoading = true;
    this.nowError = null;
    this.lightingService.getNow().subscribe({
      next: now => {
        this.now = now;
        this.nowLoading = false;
      },
      error: error => {
        this.nowError = getErrorMessages(error)[0];
        this.nowLoading = false;
      }
    });
  }

  loadSchedule(): void {
    this.scheduleLoading = true;
    this.scheduleError = null;
    this.lightingService.getSchedule(this.days).subscribe({
      next: rows => {
        this.schedule = rows;
        this.maxLitHours = rows.reduce((max, row) => Math.max(max, row.litHours), 0);
        this.scheduleLoading = false;
      },
      error: error => {
        this.scheduleError = getErrorMessages(error)[0];
        this.scheduleLoading = false;
      }
    });
  }

  nextChangeText(status: LightingStatus): string {
    switch (status) {
      case 'Off': return 'lights off';
      case 'Dimmed': return 'dims to 50%';
      default: return 'lights on';
    }
  }

  barHeight(day: LightingScheduleDay): number {
    return barHeightPercent(day.litHours, this.maxLitHours);
  }
}
