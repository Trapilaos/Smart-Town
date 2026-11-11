import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom } from 'rxjs';
import { getErrorMessages } from '../../_helpers/api-error';
import { parkingTypeIcon } from '../../parking/parking.helpers';
import { MapMarker } from '../../_models/map-marker.model';
import { PARKING_TYPES, ParkingReservation, ParkingSpace, ParkingSpaceRequest, ParkingType } from '../../_models/parking.model';
import { MunicipalityService } from '../../_services/municipality.service';
import { TrafficAndParkingService } from '../../_services/traffic-and-parking.service';

/** Marker id used for the space being added (not saved yet). */
const NEW_SPACE_MARKER_ID = 'new-space';
const ACTIVE_COLOUR = '#198754';
const INACTIVE_COLOUR = '#6c757d';

interface SpaceForm {
  name: string;
  address: string;
  type: ParkingType;
  maxVehicles: number | null;
  hourlyRate: number | null;
  isActive: boolean;
}

/** Map-based parking editor: click the map to add a space, click a space to edit it, or open its reservations drawer. */
@Component({
    selector: 'app-admin-parking',
    templateUrl: './admin-parking.component.html',
    styleUrls: ['./admin-parking.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminParkingComponent implements OnInit {
  readonly types = PARKING_TYPES;
  readonly parkingTypeIcon = parkingTypeIcon;

  boundary: GeoJSON.Geometry | null = null;
  spaces: ParkingSpace[] = [];
  markers: MapMarker[] = [];
  loaded = false;
  loadError = '';

  /** The space open in the side panel. Null when adding or when nothing is open. */
  selectedSpace: ParkingSpace | null = null;
  /** The point clicked on the map for a new space. */
  draft: { lat: number; lng: number } | null = null;
  form: SpaceForm = emptyForm();

  panelErrors: string[] = [];
  saving = false;
  removing = false;
  /** Remove needs a second click. */
  confirmingRemove = false;

  /** The reservations drawer for the selected space. */
  drawerOpen = false;
  reservations: ParkingReservation[] = [];
  reservationsLoading = false;
  reservationsError = '';

  constructor(
    private parkingService: TrafficAndParkingService,
    private municipality: MunicipalityService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.municipality.get().subscribe({
      next: municipality => this.boundary = municipality.boundary,
      error: () => {
        // The editor still works without the outline. Server-side boundary checks still apply.
      }
    });
    this.loadSpaces();
  }

  /** The id the map should highlight: the open space, or the space being added. */
  get selectedMarkerId(): number | string | null {
    if (this.selectedSpace) return this.selectedSpace.id;
    return this.draft ? NEW_SPACE_MARKER_ID : null;
  }

  get panelOpen(): boolean {
    return this.draft !== null || this.selectedSpace !== null;
  }

  loadSpaces(selectId?: number): void {
    this.parkingService.getAdminParking().subscribe({
      next: spaces => {
        this.spaces = spaces;
        this.loadError = '';
        this.loaded = true;
        if (selectId !== undefined) {
          const space = spaces.find(item => item.id === selectId);
          if (space) {
            this.startEdit(space);
            return;
          }
        }
        this.refreshMarkers();
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loaded = true;
      }
    });
  }

  onMapClick(point: { lat: number; lng: number }): void {
    this.draft = { lat: point.lat, lng: point.lng };
    this.selectedSpace = null;
    this.form = emptyForm();
    this.closeDrawer();
    this.resetPanel();
  }

  onMarkerClick(id: number | string): void {
    if (id === NEW_SPACE_MARKER_ID) return;
    const space = this.spaces.find(item => String(item.id) === String(id));
    if (space) this.startEdit(space);
  }

  cancel(): void {
    this.draft = null;
    this.selectedSpace = null;
    this.closeDrawer();
    this.resetPanel();
  }

  save(): void {
    if (this.saving) return;

    const errors = this.validate();
    if (errors.length > 0) {
      this.panelErrors = errors;
      return;
    }

    this.saving = true;
    this.panelErrors = [];

    if (this.draft) {
      const request = this.requestFields(this.draft.lat, this.draft.lng);
      this.parkingService.createParking(request).subscribe({
        next: created => {
          this.saving = false;
          this.toastr.success('Parking space added');
          this.loadSpaces(created.id);
        },
        error: error => this.failSave(error)
      });
    } else if (this.selectedSpace) {
      const space = this.selectedSpace;
      const request = this.requestFields(space.latitude, space.longitude);
      this.parkingService.updateParking(space.id, request).subscribe({
        next: () => {
          this.saving = false;
          this.toastr.success('Parking space saved');
          this.loadSpaces(space.id);
        },
        error: error => this.failSave(error)
      });
    }
  }

  remove(): void {
    const space = this.selectedSpace;
    if (!space || this.removing) return;

    if (!this.confirmingRemove) {
      this.confirmingRemove = true;
      return;
    }

    this.removing = true;
    this.panelErrors = [];
    this.parkingService.deleteParking(space.id).subscribe({
      next: () => {
        this.removing = false;
        this.toastr.success('Parking space removed');
        this.spaces = this.spaces.filter(item => item.id !== space.id);
        this.cancel();
        this.refreshMarkers();
      },
      error: error => {
        this.panelErrors = getErrorMessages(error);
        this.removing = false;
      }
    });
  }

  openDrawer(): void {
    const space = this.selectedSpace;
    if (!space) return;

    this.drawerOpen = true;
    this.reservations = [];
    this.reservationsError = '';
    this.reservationsLoading = true;
    this.parkingService.getReservations(space.id).subscribe({
      next: reservations => {
        this.reservations = reservations;
        this.reservationsLoading = false;
      },
      error: error => {
        this.reservationsError = getErrorMessages(error)[0];
        this.reservationsLoading = false;
      }
    });
  }

  closeDrawer(): void {
    this.drawerOpen = false;
    this.reservations = [];
    this.reservationsError = '';
    this.reservationsLoading = false;
  }

  private startEdit(space: ParkingSpace): void {
    this.selectedSpace = space;
    this.draft = null;
    this.closeDrawer();
    this.form = {
      name: space.name,
      address: space.address ?? '',
      type: space.type,
      maxVehicles: space.maxVehicles,
      hourlyRate: space.hourlyRate,
      isActive: space.isActive
    };
    this.resetPanel();
  }

  private resetPanel(): void {
    this.panelErrors = [];
    this.confirmingRemove = false;
    this.refreshMarkers();
  }

  private failSave(error: unknown): void {
    // The server's message (for example "Parking space must be inside the municipality boundary") is shown inline.
    this.panelErrors = getErrorMessages(error);
    this.saving = false;
  }

  private validate(): string[] {
    const errors: string[] = [];
    const form = this.form;
    const name = form.name.trim();
    if (!name) errors.push('Enter a name for the parking space.');
    if (name.length > 120) errors.push('The name must be 120 characters or fewer.');
    if (form.maxVehicles === null || !Number.isInteger(form.maxVehicles) || form.maxVehicles < 1 || form.maxVehicles > 1000) {
      errors.push('Capacity must be a whole number from 1 to 1000.');
    }
    if (form.hourlyRate === null || form.hourlyRate < 0 || form.hourlyRate > 1000) {
      errors.push('The hourly rate must be between 0 and 1000 euro.');
    }
    return errors;
  }

  private requestFields(latitude: number, longitude: number): ParkingSpaceRequest {
    const address = this.form.address.trim();
    return {
      name: this.form.name.trim(),
      address: address || null,
      type: this.form.type,
      latitude,
      longitude,
      maxVehicles: this.form.maxVehicles ?? 0,
      hourlyRate: this.form.hourlyRate ?? 0,
      isActive: this.form.isActive
    };
  }

  private refreshMarkers(): void {
    const markers: MapMarker[] = this.spaces.map(space => ({
      id: space.id,
      lat: space.latitude,
      lng: space.longitude,
      color: space.isActive ? ACTIVE_COLOUR : INACTIVE_COLOUR,
      iconClass: parkingTypeIcon(space.type),
      title: space.name,
      subtitle: `${space.type}${space.isActive ? '' : ' · inactive'} · ${space.maxVehicles} spaces`
    }));

    if (this.draft) {
      markers.push({
        id: NEW_SPACE_MARKER_ID,
        lat: this.draft.lat,
        lng: this.draft.lng,
        color: '#0d6efd',
        iconClass: 'fas fa-plus',
        title: 'New parking space',
        subtitle: 'Not saved yet'
      });
    }

    this.markers = markers;
  }
}

function emptyForm(): SpaceForm {
  return { name: '', address: '', type: 'Street', maxVehicles: 10, hourlyRate: 0, isActive: true };
}
