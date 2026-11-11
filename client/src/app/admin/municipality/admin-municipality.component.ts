import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages } from '../../_helpers/api-error';
import { BoundaryCandidate, Municipality, MunicipalityUpdateRequest } from '../../_models/municipality.model';
import { MunicipalityService } from '../../_services/municipality.service';

type SettingsForm = Pick<MunicipalityUpdateRequest,
  'name' | 'centerLatitude' | 'centerLongitude' | 'defaultZoom' | 'streetlightCount' | 'streetlightWatts'>;

/** Municipality settings and the boundary (OpenStreetMap search, or pasted GeoJSON as the advanced fallback). */
@Component({
    selector: 'app-admin-municipality',
    templateUrl: './admin-municipality.component.html',
    styleUrls: ['./admin-municipality.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminMunicipalityComponent implements OnInit {
  municipality: Municipality | null = null;
  loading = true;
  loadError = '';

  settings: SettingsForm = {
    name: '',
    centerLatitude: 0,
    centerLongitude: 0,
    defaultZoom: 14,
    streetlightCount: 0,
    streetlightWatts: 0
  };
  saving = false;
  settingsErrors: string[] = [];

  searchQuery = '';
  searching = false;
  searched = false;
  searchErrors: string[] = [];
  candidates: BoundaryCandidate[] = [];
  /** The search result shown on the map, waiting for "Use this boundary". */
  preview: BoundaryCandidate | null = null;
  applying = false;
  applyErrors: string[] = [];

  geoJsonText = '';
  savingGeoJson = false;
  geoJsonErrors: string[] = [];

  constructor(private municipalityService: MunicipalityService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.load();
  }

  /** The geometry the map shows: the previewed candidate, otherwise the saved boundary. */
  get mapBoundary(): GeoJSON.Geometry | null {
    if (this.preview) return this.preview.geometry;
    return this.municipality?.boundary ?? null;
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.municipalityService.get().subscribe({
      next: municipality => {
        this.applyMunicipality(municipality);
        if (!this.searchQuery) this.searchQuery = municipality.name;
        this.loading = false;
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  saveSettings(): void {
    if (this.saving) return;
    this.saving = true;
    this.settingsErrors = [];
    this.municipalityService.update({ ...this.settings }).subscribe({
      next: updated => {
        this.applyMunicipality(updated);
        this.saving = false;
        this.toastr.success('Municipality settings saved');
      },
      error: error => {
        this.settingsErrors = getErrorMessages(error);
        this.saving = false;
      }
    });
  }

  searchBoundary(): void {
    const query = this.searchQuery.trim();
    if (!query || this.searching) return;

    this.searching = true;
    this.searched = false;
    this.searchErrors = [];
    this.candidates = [];
    this.preview = null;
    this.municipalityService.searchBoundary(query).subscribe({
      next: results => {
        this.candidates = results;
        this.searched = true;
        this.searching = false;
      },
      error: error => {
        this.searchErrors = getErrorMessages(error);
        this.searching = false;
      }
    });
  }

  previewCandidate(candidate: BoundaryCandidate): void {
    this.preview = candidate;
    this.applyErrors = [];
  }

  cancelPreview(): void {
    this.preview = null;
    this.applyErrors = [];
  }

  applyPreview(): void {
    const candidate = this.preview;
    if (!candidate || this.applying) return;

    this.applying = true;
    this.applyErrors = [];
    this.municipalityService.applyBoundary(candidate.osmType, candidate.osmId).subscribe({
      next: updated => {
        this.applyMunicipality(updated);
        this.preview = null;
        this.candidates = [];
        this.searched = false;
        this.applying = false;
        this.toastr.success('Boundary updated');
      },
      error: error => {
        this.applyErrors = getErrorMessages(error);
        this.applying = false;
      }
    });
  }

  /** Advanced fallback: saves a pasted GeoJSON geometry, keeping the stored settings. */
  saveGeoJson(): void {
    const current = this.municipality;
    if (!current || this.savingGeoJson) return;

    this.geoJsonErrors = [];
    let boundary: GeoJSON.Geometry;
    try {
      boundary = JSON.parse(this.geoJsonText) as GeoJSON.Geometry;
    } catch {
      this.geoJsonErrors = ['That is not valid JSON.'];
      return;
    }
    if (!boundary || typeof boundary !== 'object') {
      this.geoJsonErrors = ['Paste a GeoJSON Polygon, MultiPolygon, Feature or FeatureCollection.'];
      return;
    }

    this.savingGeoJson = true;
    this.municipalityService.update({
      name: current.name,
      centerLatitude: current.centerLatitude,
      centerLongitude: current.centerLongitude,
      defaultZoom: current.defaultZoom,
      streetlightCount: current.streetlightCount,
      streetlightWatts: current.streetlightWatts,
      boundary
    }).subscribe({
      next: updated => {
        this.applyMunicipality(updated);
        this.preview = null;
        this.geoJsonText = '';
        this.savingGeoJson = false;
        this.toastr.success('Boundary saved');
      },
      error: error => {
        this.geoJsonErrors = getErrorMessages(error);
        this.savingGeoJson = false;
      }
    });
  }

  private applyMunicipality(municipality: Municipality): void {
    this.municipality = municipality;
    this.settings = {
      name: municipality.name,
      centerLatitude: municipality.centerLatitude,
      centerLongitude: municipality.centerLongitude,
      defaultZoom: municipality.defaultZoom,
      streetlightCount: municipality.streetlightCount,
      streetlightWatts: municipality.streetlightWatts
    };
  }
}
