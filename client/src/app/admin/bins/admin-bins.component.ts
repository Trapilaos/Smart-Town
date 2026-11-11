import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages } from '../../_helpers/api-error';
import { fillBadgeClass, fillColor, wasteTypeIcon } from '../../_helpers/waste.helpers';
import { MapMarker } from '../../_models/map-marker.model';
import { WASTE_TYPES, WasteBin, WasteBinRequest, WasteType } from '../../_models/waste-bin.model';
import { MunicipalityService } from '../../_services/municipality.service';
import { WasteService } from '../../_services/waste.service';

/** Marker id used for the bin that is being added (not saved yet). */
const NEW_BIN_MARKER_ID = 'new-bin';

interface BinForm {
  location: string;
  type: WasteType;
  capacity: number | null;
  currentFillLevel: number | null;
}

/** Map-based bin editor: click the map to add a bin, click a marker to edit, empty or remove it. */
@Component({
    selector: 'app-admin-bins',
    templateUrl: './admin-bins.component.html',
    styleUrls: ['./admin-bins.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminBinsComponent implements OnInit {
  readonly types = WASTE_TYPES;
  readonly fillBadgeClass = fillBadgeClass;

  boundary: GeoJSON.Geometry | null = null;
  bins: WasteBin[] = [];
  markers: MapMarker[] = [];
  loaded = false;
  loadError = '';

  /** The bin open in the side panel. Null when adding or when nothing is open. */
  selectedBin: WasteBin | null = null;
  /** The point clicked on the map for a new bin. */
  draft: { lat: number; lng: number } | null = null;
  form: BinForm = emptyForm();

  panelErrors: string[] = [];
  saving = false;
  emptying = false;
  removing = false;
  /** Remove needs a second click, like the event delete on the dashboard. */
  confirmingRemove = false;

  constructor(
    private wasteService: WasteService,
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
    this.loadBins();
  }

  /** The id the map should highlight: the open bin, or the bin being added. */
  get selectedMarkerId(): number | string | null {
    if (this.selectedBin) return this.selectedBin.id;
    return this.draft ? NEW_BIN_MARKER_ID : null;
  }

  get panelOpen(): boolean {
    return this.draft !== null || this.selectedBin !== null;
  }

  loadBins(selectId?: number): void {
    this.wasteService.getBins().subscribe({
      next: bins => {
        this.bins = bins;
        this.loadError = '';
        this.loaded = true;
        if (selectId !== undefined) {
          const bin = bins.find(item => item.id === selectId);
          if (bin) {
            this.startEdit(bin);
            return;
          }
        }
        this.refreshMarkers();
      },
      error: () => {
        this.loadError = 'Bins are unavailable right now.';
        this.loaded = true;
      }
    });
  }

  onMapClick(point: { lat: number; lng: number }): void {
    this.draft = { lat: point.lat, lng: point.lng };
    this.selectedBin = null;
    this.form = emptyForm();
    this.resetPanel();
  }

  onMarkerClick(id: number | string): void {
    if (id === NEW_BIN_MARKER_ID) return;
    const bin = this.bins.find(item => String(item.id) === String(id));
    if (bin) this.startEdit(bin);
  }

  cancel(): void {
    this.draft = null;
    this.selectedBin = null;
    this.resetPanel();
  }

  save(): void {
    if (this.saving) return;

    const errors = this.validate();
    if (errors.length > 0) {
      this.panelErrors = errors;
      return;
    }

    const fields = this.requestFields();
    this.saving = true;
    this.panelErrors = [];

    if (this.draft) {
      const request: WasteBinRequest = { ...fields, latitude: this.draft.lat, longitude: this.draft.lng };
      this.wasteService.createBin(request).subscribe({
        next: created => {
          this.saving = false;
          this.toastr.success('Bin added');
          this.loadBins(created.id);
        },
        error: error => this.failSave(error)
      });
    } else if (this.selectedBin) {
      const bin = this.selectedBin;
      const request: WasteBinRequest = { ...fields, latitude: bin.latitude, longitude: bin.longitude };
      this.wasteService.updateBin(bin.id, request).subscribe({
        next: () => {
          this.saving = false;
          this.toastr.success('Bin saved');
          this.loadBins(bin.id);
        },
        error: error => this.failSave(error)
      });
    }
  }

  empty(): void {
    const bin = this.selectedBin;
    if (!bin || this.emptying) return;

    this.emptying = true;
    this.panelErrors = [];
    this.wasteService.emptyBin(bin.id).subscribe({
      next: () => {
        this.emptying = false;
        this.toastr.success('Bin emptied');
        this.loadBins(bin.id);
      },
      error: error => {
        this.panelErrors = getErrorMessages(error);
        this.emptying = false;
      }
    });
  }

  remove(): void {
    const bin = this.selectedBin;
    if (!bin || this.removing) return;

    if (!this.confirmingRemove) {
      this.confirmingRemove = true;
      return;
    }

    this.removing = true;
    this.panelErrors = [];
    this.wasteService.deleteBin(bin.id).subscribe({
      next: () => {
        this.removing = false;
        this.toastr.success('Bin removed');
        this.bins = this.bins.filter(item => item.id !== bin.id);
        this.cancel();
      },
      error: error => {
        this.panelErrors = getErrorMessages(error);
        this.removing = false;
      }
    });
  }

  private startEdit(bin: WasteBin): void {
    this.selectedBin = bin;
    this.draft = null;
    this.form = {
      location: bin.location,
      type: bin.type,
      capacity: bin.capacity,
      currentFillLevel: bin.currentFillLevel
    };
    this.resetPanel();
    this.refreshMarkers();
  }

  private resetPanel(): void {
    this.panelErrors = [];
    this.confirmingRemove = false;
    this.refreshMarkers();
  }

  private failSave(error: unknown): void {
    // The server's message (for example "Bin must be inside the municipality boundary") is shown inline.
    this.panelErrors = getErrorMessages(error);
    this.saving = false;
  }

  private validate(): string[] {
    const errors: string[] = [];
    const form = this.form;
    if (!form.location.trim()) errors.push('Enter a location for the bin.');
    if (form.location.trim().length > 200) errors.push('The location must be 200 characters or fewer.');
    if (form.capacity === null || form.capacity <= 0) errors.push('Capacity must be more than 0 litres.');
    if (form.currentFillLevel === null || form.currentFillLevel < 0) errors.push('Fill level cannot be negative.');
    if (form.capacity !== null && form.currentFillLevel !== null && form.currentFillLevel > form.capacity) {
      errors.push('Fill level cannot be more than the capacity.');
    }
    return errors;
  }

  private requestFields(): Omit<WasteBinRequest, 'latitude' | 'longitude'> {
    return {
      location: this.form.location.trim(),
      type: this.form.type,
      capacity: this.form.capacity ?? 0,
      currentFillLevel: this.form.currentFillLevel ?? 0
    };
  }

  private refreshMarkers(): void {
    const markers: MapMarker[] = this.bins.map(bin => ({
      id: bin.id,
      lat: bin.latitude,
      lng: bin.longitude,
      color: fillColor(bin.fillPercent),
      iconClass: wasteTypeIcon(bin.type),
      title: bin.location,
      subtitle: `${bin.type} · ${bin.fillPercent}% full`
    }));

    if (this.draft) {
      markers.push({
        id: NEW_BIN_MARKER_ID,
        lat: this.draft.lat,
        lng: this.draft.lng,
        color: '#0d6efd',
        iconClass: 'fas fa-plus',
        title: 'New bin',
        subtitle: 'Not saved yet'
      });
    }

    this.markers = markers;
  }
}

function emptyForm(): BinForm {
  return { location: '', type: 'General', capacity: 240, currentFillLevel: 0 };
}
