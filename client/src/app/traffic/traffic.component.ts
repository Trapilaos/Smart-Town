import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { TrafficData } from '../_models/traffic-data.model';
import { getErrorMessages } from '../_helpers/api-error';
import { lastValueFrom, Subject, takeUntil } from 'rxjs';

/** Live road traffic. Parking and reservations live on /parking. */
@Component({
    selector: 'app-traffic',
    templateUrl: './traffic.component.html',
    styleUrls: ['./traffic.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class TrafficComponent implements OnInit, OnDestroy {
  trafficData: TrafficData[] = [];
  trafficLoading = true;
  trafficError = '';

  private readonly visibilityHandler = () => this.handleVisibilityChange();
  private readonly destroy$ = new Subject<void>();

  constructor(private trafficAndParkingService: TrafficAndParkingService) { }

  ngOnInit(): void {
    this.loadTrafficData();
    document.addEventListener('visibilitychange', this.visibilityHandler);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    document.removeEventListener('visibilitychange', this.visibilityHandler);
  }

  handleVisibilityChange(): void {
    // Refresh live data when the tab becomes visible again.
    if (!document.hidden) {
      this.loadTrafficData();
    }
  }

  async loadTrafficData(): Promise<void> {
    try {
      this.trafficData = await lastValueFrom(this.trafficAndParkingService.getTrafficData().pipe(takeUntil(this.destroy$)));
      this.trafficError = '';
    } catch (error) {
      this.trafficError = getErrorMessages(error)[0];
    } finally {
      this.trafficLoading = false;
    }
  }

  flowPercent(data: TrafficData): number {
    return Math.min(100, Math.max(0, data.trafficFlow));
  }

  flowClass(data: TrafficData): string {
    // Traffic flow is congestion, so a high value uses the warning colours.
    const flow = this.flowPercent(data);
    if (flow > 75) return 'bg-danger';
    if (flow > 40) return 'bg-warning';
    return 'bg-success';
  }
}
