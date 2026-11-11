import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { getErrorMessages } from '../../_helpers/api-error';
import { fillColor, wasteTypeIcon } from '../../_helpers/waste.helpers';
import { MapMarker } from '../../_models/map-marker.model';
import { WASTE_TYPES, WasteType, CollectionRoute } from '../../_models/waste-bin.model';
import { MunicipalityService } from '../../_services/municipality.service';
import { WasteService } from '../../_services/waste.service';

const DEPOT_MARKER_ID = 'depot';

/** Collection route: filters, the route on the map, the ordered stops and the total distance. */
@Component({
    selector: 'app-admin-route',
    templateUrl: './admin-route.component.html',
    styleUrls: ['./admin-route.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminRouteComponent implements OnInit {
  readonly types = WASTE_TYPES;
  readonly fillColor = fillColor;

  typeFilter: WasteType | '' = '';
  minFillPercent = 60;

  boundary: GeoJSON.Geometry | null = null;
  route: CollectionRoute | null = null;
  polyline: { lat: number; lng: number }[] | null = null;
  markers: MapMarker[] = [];
  computedAt: Date | null = null;

  loading = false;
  errors: string[] = [];

  constructor(private wasteService: WasteService, private municipality: MunicipalityService) {}

  ngOnInit(): void {
    this.municipality.get().subscribe({
      next: municipality => this.boundary = municipality.boundary,
      error: () => {
        // The route still works without the outline.
      }
    });
    this.compute();
  }

  get hasStops(): boolean {
    return (this.route?.stops.length ?? 0) > 0;
  }

  compute(): void {
    if (this.loading) return;

    const minFill = Number(this.minFillPercent);
    if (!Number.isFinite(minFill) || minFill < 0 || minFill > 100) {
      this.errors = ['Minimum fill must be between 0 and 100%.'];
      return;
    }

    this.loading = true;
    this.errors = [];
    this.wasteService.getRoute(this.typeFilter, minFill).subscribe({
      next: route => {
        this.route = route;
        this.polyline = routeLine(route);
        this.markers = routeMarkers(route);
        this.computedAt = new Date();
        this.loading = false;
      },
      error: error => {
        this.errors = getErrorMessages(error);
        this.loading = false;
      }
    });
  }

  print(): void {
    window.print();
  }
}

/** Depot, then each stop in order, then back to the depot. Null when there is nothing to draw. */
function routeLine(route: CollectionRoute): { lat: number; lng: number }[] | null {
  if (route.stops.length === 0) return null;
  return [route.depot, ...route.stops.map(stop => ({ lat: stop.lat, lng: stop.lng })), route.depot];
}

function routeMarkers(route: CollectionRoute): MapMarker[] {
  const stops: MapMarker[] = route.stops.map(stop => ({
    id: stop.binId,
    lat: stop.lat,
    lng: stop.lng,
    color: fillColor(stop.fillPercent),
    iconClass: wasteTypeIcon(stop.type),
    title: `${stop.order}. ${stop.location}`,
    subtitle: `${stop.type} · ${stop.fillPercent}% full`
  }));

  return [
    {
      id: DEPOT_MARKER_ID,
      lat: route.depot.lat,
      lng: route.depot.lng,
      color: '#212529',
      iconClass: 'fas fa-warehouse',
      title: 'Depot',
      subtitle: 'Start and end of the route'
    },
    ...stops
  ];
}
