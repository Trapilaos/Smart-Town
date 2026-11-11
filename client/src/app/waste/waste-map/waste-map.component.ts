import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MapMarker } from '../../_models/map-marker.model';
import { WASTE_TYPES, WasteBin, WasteType } from '../../_models/waste-bin.model';
import { WasteService } from '../../_services/waste.service';
import { MunicipalityService } from '../../_services/municipality.service';
import { fillBadgeClass, fillColor, wasteTypeIcon } from '../../_helpers/waste.helpers';

/** Public map of the active waste bins, coloured by fill level. */
@Component({
    selector: 'app-waste-map',
    templateUrl: './waste-map.component.html',
    styleUrls: ['./waste-map.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class WasteMapComponent implements OnInit {
  readonly types = WASTE_TYPES;
  readonly fillColor = fillColor;
  readonly fillBadgeClass = fillBadgeClass;
  readonly wasteTypeIcon = wasteTypeIcon;

  selectedType: WasteType | '' = '';
  bins: WasteBin[] = [];
  markers: MapMarker[] = [];
  boundary: GeoJSON.Geometry | null = null;
  selectedBinId: number | null = null;

  loading = true;
  error = '';

  constructor(private wasteService: WasteService, private municipality: MunicipalityService) {}

  ngOnInit(): void {
    this.municipality.get().subscribe({
      next: municipality => this.boundary = municipality.boundary,
      error: () => {
        // The map still works without the outline; it falls back to the default view.
      }
    });
    this.loadBins();
  }

  loadBins(): void {
    this.loading = true;
    this.error = '';
    this.wasteService.getBins(this.selectedType).subscribe({
      next: bins => {
        this.bins = bins;
        this.markers = bins.map(bin => this.toMarker(bin));
        if (this.selectedBinId !== null && !bins.some(bin => bin.id === this.selectedBinId)) {
          this.selectedBinId = null;
        }
        this.loading = false;
      },
      error: () => {
        this.bins = [];
        this.markers = [];
        this.error = 'Waste bins are unavailable right now.';
        this.loading = false;
      }
    });
  }

  selectBin(id: number | string): void {
    this.selectedBinId = Number(id);
  }

  get selectedBin(): WasteBin | null {
    return this.bins.find(bin => bin.id === this.selectedBinId) ?? null;
  }

  private toMarker(bin: WasteBin): MapMarker {
    return {
      id: bin.id,
      lat: bin.latitude,
      lng: bin.longitude,
      color: fillColor(bin.fillPercent),
      iconClass: wasteTypeIcon(bin.type),
      title: bin.location,
      subtitle: `${bin.type} · ${bin.fillPercent}% full`
    };
  }
}
