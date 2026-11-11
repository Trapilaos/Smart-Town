import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom, Subject, takeUntil } from 'rxjs';
import { AccountService } from '../_services/account.service';
import { TrafficAndParkingService } from '../_services/traffic-and-parking.service';
import { MunicipalityService } from '../_services/municipality.service';
import { getErrorMessages } from '../_helpers/api-error';
import { MapMarker } from '../_models/map-marker.model';
import { ActiveReservation, ParkingSpace, ReservationRequest } from '../_models/parking.model';
import { parkingTypeIcon } from './parking.helpers';

const FREE_COLOUR = '#198754';
const FULL_COLOUR = '#dc3545';

/** Public parking: a map and list coloured by free or full. Members reserve from the list. */
@Component({
    selector: 'app-parking-map',
    templateUrl: './parking-map.component.html',
    styleUrls: ['./parking-map.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ParkingMapComponent implements OnInit, OnDestroy {
  readonly minDuration = 1;
  readonly maxDuration = 1440;
  readonly durationPresets = [
    { label: '30 min', minutes: 30 },
    { label: '1 hour', minutes: 60 },
    { label: '2 hours', minutes: 120 },
    { label: '4 hours', minutes: 240 },
    { label: '1 day', minutes: 1440 }
  ];

  readonly parkingTypeIcon = parkingTypeIcon;

  boundary: GeoJSON.Geometry | null = null;
  spaces: ParkingSpace[] = [];
  markers: MapMarker[] = [];
  /** Space highlighted on the map (set by a marker click). */
  selectedMapId: number | null = null;

  loading = true;
  error = '';

  loggedIn = false;
  activeReservation: ActiveReservation | null = null;

  /** Space whose reservation form is open. */
  selectedParkingSpaceId: number | null = null;
  reservationDuration: number | null = null;
  reservationTime: Date = new Date();
  reservationError = '';
  reserving = false;

  private readonly destroy$ = new Subject<void>();
  private readonly visibilityHandler = () => this.handleVisibilityChange();

  constructor(
    private accountService: AccountService,
    private parkingService: TrafficAndParkingService,
    private municipality: MunicipalityService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.municipality.get().pipe(takeUntil(this.destroy$)).subscribe({
      next: municipality => this.boundary = municipality.boundary,
      error: () => {
        // The map still works without the outline.
      }
    });

    this.accountService.currentUser$.pipe(takeUntil(this.destroy$)).subscribe(user => {
      this.loggedIn = !!user;
      if (user) {
        this.loadActiveReservation();
      } else {
        this.activeReservation = null;
        this.cancelReservation();
      }
    });

    this.loadSpaces();
    document.addEventListener('visibilitychange', this.visibilityHandler);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    document.removeEventListener('visibilitychange', this.visibilityHandler);
  }

  handleVisibilityChange(): void {
    // Free counts change as reservations start and end, so refresh when the tab comes back.
    if (!document.hidden) {
      this.loadSpaces();
      if (this.loggedIn) this.loadActiveReservation();
    }
  }

  async loadSpaces(): Promise<void> {
    try {
      this.spaces = await lastValueFrom(this.parkingService.getParkingSpaces().pipe(takeUntil(this.destroy$)));
      this.error = '';
      this.refreshMarkers();
    } catch (error) {
      this.error = getErrorMessages(error)[0];
    } finally {
      this.loading = false;
    }
  }

  async loadActiveReservation(): Promise<void> {
    try {
      this.activeReservation = await lastValueFrom(this.parkingService.getActiveReservation().pipe(takeUntil(this.destroy$)));
    } catch {
      this.activeReservation = null;
    }
  }

  onMarkerClick(id: number | string): void {
    this.selectedMapId = Number(id);
  }

  isSelected(space: ParkingSpace): boolean {
    return this.selectedParkingSpaceId === space.id;
  }

  selectParkingSpace(space: ParkingSpace): void {
    this.selectedParkingSpaceId = space.id;
    this.selectedMapId = space.id;
    this.reservationDuration = null;
    this.reservationTime = new Date();
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

    const request: ReservationRequest = {
      parkingSpaceId: spaceId,
      reservationTime: this.reservationTime.toISOString(),
      duration
    };

    this.reserving = true;
    this.reservationError = '';
    try {
      await lastValueFrom(this.parkingService.reserveParkingSpace(request).pipe(takeUntil(this.destroy$)));
      this.toastr.success('Parking space reserved.');
      this.cancelReservation();
      await Promise.all([this.loadSpaces(), this.loadActiveReservation()]);
    } catch (error) {
      // Show the server's reason inline, for example "Parking space is full".
      this.reservationError = getErrorMessages(error)[0];
    } finally {
      this.reserving = false;
    }
  }

  occupancyPercent(space: ParkingSpace): number {
    if (!space.maxVehicles) return 0;
    return Math.min(100, Math.round((space.currentVehicles / space.maxVehicles) * 100));
  }

  isFull(space: ParkingSpace): boolean {
    return space.freeNow <= 0;
  }

  /** Bootstrap colour class for free or full. */
  availabilityClass(space: ParkingSpace): string {
    return this.isFull(space) ? 'bg-danger' : 'bg-success';
  }

  hasActiveReservation(): boolean {
    return !!this.activeReservation;
  }

  spaceName(id: number): string {
    return this.spaces.find(space => space.id === id)?.name ?? `Space ${id}`;
  }

  getEndTime(reservation: ActiveReservation): Date {
    return new Date(new Date(reservation.reservationTime).getTime() + reservation.duration * 60000);
  }

  private refreshMarkers(): void {
    this.markers = this.spaces.map(space => ({
      id: space.id,
      lat: space.latitude,
      lng: space.longitude,
      color: this.isFull(space) ? FULL_COLOUR : FREE_COLOUR,
      iconClass: parkingTypeIcon(space.type),
      title: space.name,
      subtitle: `${space.type} · ${space.freeNow} of ${space.maxVehicles} free`
    }));
  }
}
