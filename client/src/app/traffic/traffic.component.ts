import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MembersService } from '../_services/members.service';
import { ParkingSpace } from '../_models/parking-space.model';
import { Reservation } from '../_models/reservation.model';
import { TrafficData } from '../_models/traffic-data.model';
import { Member } from '../_models/member';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom, Subject, takeUntil } from 'rxjs';
import { User } from '../_models/user';

@Component({
    selector: 'app-traffic',
    templateUrl: './traffic.component.html',
    styleUrls: ['./traffic.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class TrafficComponent implements OnInit, OnDestroy {
  readonly minDuration = 1;
  readonly maxDuration = 1440;
  readonly durationPresets = [
    { label: '30 min', minutes: 30 },
    { label: '1 hour', minutes: 60 },
    { label: '2 hours', minutes: 120 },
    { label: '4 hours', minutes: 240 },
    { label: '1 day', minutes: 1440 }
  ];

  parkingSpaces: ParkingSpace[] = [];
  trafficData: TrafficData[] = [];
  activeReservation: Reservation | null = null;

  parkingLoading = true;
  trafficLoading = true;
  parkingError = '';
  trafficError = '';
  reservationError = '';

  selectedParkingSpaceId: number | null = null;
  reservationDuration: number | null = null;
  reservationTime: Date = new Date();
  reserving = false;
  currentUser: Member | null = null;

  private readonly visibilityHandler = () => this.handleVisibilityChange();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private trafficAndParkingService: TrafficAndParkingService,
    private membersService: MembersService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.loadTrafficData();
    this.loadParkingSpaces();
    this.loadActiveReservation();

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
      this.loadParkingSpaces();
      this.loadActiveReservation();
    }
  }

  async loadTrafficData(): Promise<void> {
    try {
      this.trafficData = await lastValueFrom(this.trafficAndParkingService.getTrafficData().pipe(takeUntil(this.destroy$)));
      this.trafficError = '';
    } catch {
      this.trafficError = 'Traffic data is unavailable right now.';
    } finally {
      this.trafficLoading = false;
    }
  }

  async loadParkingSpaces(): Promise<void> {
    try {
      this.parkingSpaces = await lastValueFrom(this.trafficAndParkingService.getParkingSpaces().pipe(takeUntil(this.destroy$)));
      this.parkingError = '';
    } catch {
      this.parkingError = 'Parking spaces are unavailable right now.';
    } finally {
      this.parkingLoading = false;
    }
  }

  async loadActiveReservation(): Promise<void> {
    try {
      this.activeReservation = await lastValueFrom(this.trafficAndParkingService.getActiveReservation().pipe(takeUntil(this.destroy$)));
    } catch {
      this.activeReservation = null;
    }
  }

  private async loadCurrentUser(): Promise<Member> {
    const userStr = localStorage.getItem('user');
    const username = userStr ? (JSON.parse(userStr) as User).username : '';
    if (!username) {
      throw new Error('No logged-in user found');
    }
    const member = await lastValueFrom(this.membersService.getMemberByUsername(username).pipe(takeUntil(this.destroy$)));
    this.currentUser = member;
    return member;
  }

  isSelected(space: ParkingSpace): boolean {
    return this.selectedParkingSpaceId === space.id;
  }

  selectParkingSpace(space: ParkingSpace): void {
    this.selectedParkingSpaceId = space.id;
    this.reservationDuration = null;
    this.reservationError = '';
  }

  cancelReservation(): void {
    this.selectedParkingSpaceId = null;
    this.reservationDuration = null;
    this.reservationError = '';
  }

  isDurationValid(): boolean {
    const duration = this.reservationDuration;
    return duration !== null && Number.isInteger(duration) && duration >= this.minDuration && duration <= this.maxDuration;
  }

  get durationInvalid(): boolean {
    return this.reservationDuration !== null && !this.isDurationValid();
  }

  async confirmReservation(): Promise<void> {
    const spaceId = this.selectedParkingSpaceId;
    const duration = this.reservationDuration;
    if (this.reserving || spaceId === null || duration === null || !this.isDurationValid()) {
      return;
    }

    let user = this.currentUser;
    if (!user) {
      try {
        user = await this.loadCurrentUser();
      } catch {
        this.reservationError = 'We could not load your profile. Please log in again.';
        return;
      }
    }

    const reservation: Reservation = {
      userId: user.id.toString(),
      parkingSpaceId: spaceId,
      reservationTime: new Date(this.reservationTime.getTime() - (this.reservationTime.getTimezoneOffset() * 60000)),
      duration
    };

    this.reserving = true;
    this.reservationError = '';
    try {
      await lastValueFrom(this.trafficAndParkingService.reserveParkingSpace(reservation).pipe(takeUntil(this.destroy$)));
      this.toastr.success('Parking space reserved.');
      this.resetReservationForm();
      await Promise.all([this.loadParkingSpaces(), this.loadActiveReservation()]);
    } catch {
      // The global error interceptor already shows the API's message, so the form stays open without a second toast.
    } finally {
      this.reserving = false;
    }
  }

  resetReservationForm(): void {
    this.selectedParkingSpaceId = null;
    this.reservationDuration = null;
    this.reservationTime = new Date();
  }

  occupancyPercent(space: ParkingSpace): number {
    if (!space.maxVehicles) return 0;
    return Math.min(100, Math.round((space.currentVehicles / space.maxVehicles) * 100));
  }

  occupancyClass(space: ParkingSpace): string {
    return this.statusClass(this.occupancyPercent(space));
  }

  isFull(space: ParkingSpace): boolean {
    return space.currentVehicles >= space.maxVehicles;
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

  spaceName(id: number): string {
    return this.parkingSpaces.find(space => space.id === id)?.name ?? `Space ${id}`;
  }

  getEndTime(reservation: Reservation): Date {
    return new Date(new Date(reservation.reservationTime).getTime() + reservation.duration * 60000);
  }

  hasActiveReservation(): boolean {
    return !!this.activeReservation;
  }

  private statusClass(percent: number): string {
    if (percent >= 80) return 'bg-danger';
    if (percent >= 50) return 'bg-warning';
    return 'bg-success';
  }
}
