import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import * as L from 'leaflet';
import { Subscription, take } from 'rxjs';
import { MapMarker } from '../../_models/map-marker.model';
import { MunicipalityService } from '../../_services/municipality.service';

const TILE_URL = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors';

/** A world-sized ring. Together with the boundary rings as holes, it dims everything outside the municipality. */
const WORLD_RING: L.LatLngTuple[] = [[-85, -180], [-85, 180], [85, 180], [85, -180]];

/**
 * Shared Leaflet map. Pages pass data in and listen for clicks; the component
 * never builds HTML from data. Markers are divIcons, and popups are DOM nodes
 * filled with textContent.
 *
 * Inputs and outputs are documented in the municipality plan (section 2.1).
 * `editable` controls whether a click on the empty map emits `mapClick`. When it
 * is false, the map ignores clicks and shows the normal cursor.
 */
@Component({
    selector: 'app-map',
    templateUrl: './map.component.html',
    styleUrls: ['./map.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class MapComponent implements AfterViewInit, OnChanges, OnDestroy {
  /** GeoJSON Polygon or MultiPolygon geometry. When set, the map fits to it and dims the area outside. */
  @Input() boundary: GeoJSON.Geometry | null = null;
  @Input() markers: MapMarker[] = [];
  /** Polyline points in order. */
  @Input() route: { lat: number; lng: number }[] | null = null;
  @Input() editable = false;
  @Input() height = '420px';
  @Input() selectedId: number | string | null = null;

  /** A click on the empty map (only when `editable` is true). */
  @Output() mapClick = new EventEmitter<{ lat: number; lng: number }>();
  /** The id of the marker that was clicked. */
  @Output() markerClick = new EventEmitter<number | string>();

  @ViewChild('container', { static: true }) private containerRef!: ElementRef<HTMLElement>;

  private map: L.Map | null = null;
  private boundaryLayer = L.layerGroup();
  private routeLayer = L.layerGroup();
  private markerLayer = L.layerGroup();
  private markerEntries: { id: number | string; marker: L.Marker }[] = [];
  private viewSub: Subscription | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private resizeTimer: ReturnType<typeof setTimeout> | null = null;
  /** True once the container has a non-zero size. The first size triggers a fit. */
  private sized = false;
  /** True after the user pans or zooms. Automatic fits stop after that. */
  private userAdjusted = false;
  /** True while the component moves the map itself, so those moves are not counted as user moves. */
  private programmatic = false;
  private readonly onWindowResize = () => this.handleContainerResize();

  constructor(private municipality: MunicipalityService) {}

  ngAfterViewInit(): void {
    const container = this.containerRef.nativeElement;
    const map = L.map(container, { zoomControl: true, attributionControl: true });
    L.tileLayer(TILE_URL, { maxZoom: 19, attribution: TILE_ATTRIBUTION }).addTo(map);
    this.boundaryLayer.addTo(map);
    this.routeLayer.addTo(map);
    this.markerLayer.addTo(map);
    map.on('click', (event: L.LeafletMouseEvent) => this.handleMapClick(event));
    map.on('dragstart zoomstart', () => this.markUserMove());
    this.map = map;

    this.drawBoundary();
    this.drawRoute();
    this.drawMarkers();
    this.fitView();

    // The container is laid out after this hook runs (cards, flex and grid settle later), so track its size.
    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => this.handleContainerResize());
      this.resizeObserver.observe(container);
    } else {
      this.resizeTimer = setTimeout(() => this.handleContainerResize());
      window.addEventListener('resize', this.onWindowResize);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.map) return;

    if (changes['boundary']) {
      this.drawBoundary();
      this.fitView();
    }
    if (changes['route']) this.drawRoute();
    if (changes['markers']) {
      this.drawMarkers();
      this.fitView();
    }
    if (changes['selectedId']) this.applySelection();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    if (this.resizeTimer !== null) clearTimeout(this.resizeTimer);
    window.removeEventListener('resize', this.onWindowResize);
    this.viewSub?.unsubscribe();
    this.map?.remove();
    this.map = null;
    this.markerEntries = [];
  }

  /** Every size change refreshes Leaflet's size. The first non-zero size also re-fits the view. */
  private handleContainerResize(): void {
    if (!this.map) return;

    this.map.invalidateSize();
    const { clientWidth, clientHeight } = this.containerRef.nativeElement;
    if (!this.sized && clientWidth > 0 && clientHeight > 0) {
      this.sized = true;
      this.fitView();
    }
  }

  private markUserMove(): void {
    if (!this.programmatic) this.userAdjusted = true;
  }

  private moveProgrammatically(action: () => void): void {
    this.programmatic = true;
    try {
      action();
    } finally {
      this.programmatic = false;
    }
  }

  private handleMapClick(event: L.LeafletMouseEvent): void {
    if (!this.editable) return;

    // Clicks on a marker or its popup are handled by those elements, not as map clicks.
    const target = event.originalEvent?.target;
    if (target instanceof Element && target.closest('.leaflet-marker-icon, .leaflet-popup')) return;

    this.mapClick.emit({ lat: event.latlng.lat, lng: event.latlng.lng });
  }

  /**
   * Fits to the boundary, else to the markers, else to the municipality centre.
   * Does nothing once the user has panned or zoomed.
   */
  private fitView(): void {
    if (!this.map || this.userAdjusted) return;

    const rings = boundaryRings(this.boundary);
    const boundsOfBoundary = rings.length > 0 ? L.latLngBounds(rings.flat()) : null;
    if (boundsOfBoundary?.isValid()) {
      this.fitTo(boundsOfBoundary, 18);
      return;
    }

    if (this.markers.length > 0) {
      const boundsOfMarkers = L.latLngBounds(this.markers.map(item => L.latLng(item.lat, item.lng)));
      if (boundsOfMarkers.isValid()) {
        this.fitTo(boundsOfMarkers, 16);
        return;
      }
    }

    this.viewSub?.unsubscribe();
    this.viewSub = this.municipality.get().pipe(take(1)).subscribe({
      next: centre => {
        if (this.userAdjusted) return;
        this.moveProgrammatically(() => this.map?.setView([centre.centerLatitude, centre.centerLongitude], centre.defaultZoom));
      },
      error: () => {
        // Without a centre the map keeps Leaflet's default view. The page shows its own error.
      }
    });
  }

  private fitTo(bounds: L.LatLngBounds, maxZoom: number): void {
    if (!this.map) return;
    this.moveProgrammatically(() => this.map?.fitBounds(bounds, { padding: [16, 16], maxZoom }));
  }

  private drawBoundary(): void {
    this.boundaryLayer.clearLayers();
    if (!this.boundary) return;

    const rings = boundaryRings(this.boundary);
    if (rings.length > 0) {
      L.polygon([WORLD_RING, ...rings], {
        stroke: false,
        fillColor: '#212529',
        fillOpacity: 0.25,
        fillRule: 'evenodd',
        interactive: false
      }).addTo(this.boundaryLayer);
    }

    L.geoJSON(this.boundary, {
      interactive: false,
      style: () => ({ color: '#0d6efd', weight: 2, fillOpacity: 0 })
    }).addTo(this.boundaryLayer);
  }

  private drawRoute(): void {
    this.routeLayer.clearLayers();
    if (!this.route || this.route.length < 2) return;

    const points = this.route.map(point => [point.lat, point.lng] as L.LatLngTuple);
    L.polyline(points, { color: '#0d6efd', weight: 4, opacity: 0.85, interactive: false }).addTo(this.routeLayer);
  }

  private drawMarkers(): void {
    this.markerLayer.clearLayers();
    this.markerEntries = this.markers.map(item => {
      const marker = L.marker([item.lat, item.lng], {
        icon: markerIcon(item),
        title: item.title,
        alt: item.title
      });
      marker.bindPopup(markerPopup(item));
      marker.on('click', () => this.markerClick.emit(item.id));
      marker.addTo(this.markerLayer);
      return { id: item.id, marker };
    });
    this.applySelection();
  }

  private applySelection(): void {
    for (const entry of this.markerEntries) {
      const element = entry.marker.getElement();
      if (!element) continue;
      const selected = this.selectedId !== null && String(entry.id) === String(this.selectedId);
      element.classList.toggle('app-map-marker--selected', selected);
    }
  }
}

/** Every ring of a Polygon or MultiPolygon, as [lat, lng] pairs. Empty for other geometry types. */
function boundaryRings(geometry: GeoJSON.Geometry | null): L.LatLngTuple[][] {
  if (!geometry) return [];

  let polygons: GeoJSON.Position[][][];
  if (geometry.type === 'Polygon') {
    polygons = [geometry.coordinates];
  } else if (geometry.type === 'MultiPolygon') {
    polygons = geometry.coordinates;
  } else {
    return [];
  }

  return polygons.flatMap(polygon =>
    polygon.map(ring => ring.map(([lng, lat]) => [lat, lng] as L.LatLngTuple))
  );
}

function markerIcon(item: MapMarker): L.DivIcon {
  const dot = document.createElement('span');
  dot.className = 'app-map-marker__dot';
  dot.style.backgroundColor = item.color;

  const icon = document.createElement('i');
  icon.className = item.iconClass;
  icon.setAttribute('aria-hidden', 'true');
  dot.appendChild(icon);

  return L.divIcon({
    className: 'app-map-marker',
    html: dot,
    iconSize: [30, 30],
    iconAnchor: [15, 15],
    popupAnchor: [0, -15]
  });
}

function markerPopup(item: MapMarker): HTMLElement {
  const root = document.createElement('div');

  const title = document.createElement('strong');
  title.textContent = item.title;
  root.appendChild(title);

  if (item.subtitle) {
    const subtitle = document.createElement('div');
    subtitle.className = 'small text-muted';
    subtitle.textContent = item.subtitle;
    root.appendChild(subtitle);
  }

  return root;
}
