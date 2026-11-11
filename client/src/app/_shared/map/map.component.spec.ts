import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import * as L from 'leaflet';
import { MapComponent } from './map.component';
import { MunicipalityService } from '../../_services/municipality.service';
import { Municipality } from '../../_models/municipality.model';
import { MapMarker } from '../../_models/map-marker.model';

describe('MapComponent', () => {
  let fixture: ComponentFixture<MapComponent>;
  let component: MapComponent;

  const centre = { centerLatitude: 37.865, centerLongitude: 23.753, defaultZoom: 14 } as Municipality;

  const makeMarkers = (count: number): MapMarker[] =>
    Array.from({ length: count }, (_, index) => ({
      id: index + 1,
      lat: 37.86 + index * 0.001,
      lng: 23.75,
      color: '#198754',
      iconClass: 'fas fa-trash',
      title: `Bin ${index + 1}`
    }));

  const leafletMap = () => (component as unknown as { map: L.Map | null }).map;
  const markerElements = (): HTMLElement[] =>
    Array.from(fixture.nativeElement.querySelectorAll('.app-map-marker') as NodeListOf<HTMLElement>);

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MapComponent],
      providers: [{ provide: MunicipalityService, useValue: { get: () => of(centre) } }]
    });
    fixture = TestBed.createComponent(MapComponent);
    component = fixture.componentInstance;
  });

  it('creates a Leaflet map with OpenStreetMap attribution', () => {
    fixture.detectChanges();

    expect(leafletMap()).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.leaflet-container')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.leaflet-control-attribution').textContent).toContain('OpenStreetMap');
  });

  it('centres on the municipality when there is no boundary', () => {
    fixture.detectChanges();

    expect(leafletMap()!.getCenter().lat).toBeCloseTo(centre.centerLatitude, 3);
    expect(leafletMap()!.getCenter().lng).toBeCloseTo(centre.centerLongitude, 3);
    expect(leafletMap()!.getZoom()).toBe(centre.defaultZoom);
  });

  it('renders one marker per input marker and replaces them on change', () => {
    fixture.componentRef.setInput('markers', makeMarkers(3));
    fixture.detectChanges();
    expect(markerElements().length).toBe(3);

    fixture.componentRef.setInput('markers', makeMarkers(5));
    fixture.detectChanges();
    expect(markerElements().length).toBe(5);

    fixture.componentRef.setInput('markers', makeMarkers(1));
    fixture.detectChanges();
    expect(markerElements().length).toBe(1);
  });

  it('draws the boundary outline and the dimmed area outside it', () => {
    fixture.componentRef.setInput('boundary', {
      type: 'Polygon',
      coordinates: [[[23.74, 37.85], [23.76, 37.85], [23.76, 37.87], [23.74, 37.87], [23.74, 37.85]]]
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.leaflet-overlay-pane path').length).toBeGreaterThanOrEqual(2);
  });

  it('emits mapClick with the clicked point when editable', () => {
    fixture.componentRef.setInput('editable', true);
    fixture.detectChanges();
    const spy = spyOn(component.mapClick, 'emit');

    leafletMap()!.fire('click', { latlng: L.latLng(37.87, 23.75) } as unknown as L.LeafletEvent);

    expect(spy).toHaveBeenCalledOnceWith({ lat: 37.87, lng: 23.75 });
  });

  it('ignores map clicks when not editable', () => {
    fixture.detectChanges();
    const spy = spyOn(component.mapClick, 'emit');

    leafletMap()!.fire('click', { latlng: L.latLng(37.87, 23.75) } as unknown as L.LeafletEvent);

    expect(spy).not.toHaveBeenCalled();
  });

  it('emits markerClick with the marker id', () => {
    fixture.componentRef.setInput('markers', makeMarkers(2));
    fixture.detectChanges();
    const spy = spyOn(component.markerClick, 'emit');

    markerElements()[1].click();

    expect(spy).toHaveBeenCalledOnceWith(2);
  });

  it('builds popups with text only, so markup in data is shown as text', () => {
    fixture.componentRef.setInput('markers', [{
      id: 'x',
      lat: 37.86,
      lng: 23.75,
      color: '#dc3545',
      iconClass: 'fas fa-trash',
      title: '<img src="x" onerror="alert(1)">',
      subtitle: '<b>bold</b>'
    }]);
    fixture.detectChanges();

    const entries = (component as unknown as { markerEntries: { marker: L.Marker }[] }).markerEntries;
    const popup = entries[0].marker.getPopup()!.getContent() as HTMLElement;

    expect(popup.querySelector('img')).toBeNull();
    expect(popup.querySelector('b')).toBeNull();
    expect(popup.textContent).toContain('<img src="x" onerror="alert(1)">');
  });

  /**
   * Replaces requestAnimationFrame with a queue that the test flushes by hand,
   * so resize handling runs at a known point and never depends on real frame timing.
   */
  const stubFrames = () => {
    let nextId = 1;
    const queue = new Map<number, FrameRequestCallback>();
    spyOn(window, 'requestAnimationFrame').and.callFake((callback: FrameRequestCallback) => {
      const id = nextId++;
      queue.set(id, callback);
      return id;
    });
    spyOn(window, 'cancelAnimationFrame').and.callFake((id: number) => {
      queue.delete(id);
    });
    return {
      pending: () => queue.size,
      flush: () => {
        const callbacks = Array.from(queue.values());
        queue.clear();
        callbacks.forEach(callback => callback(0));
      }
    };
  };

  const scheduleResize = () => (component as unknown as { scheduleResize: () => void }).scheduleResize();
  const userAdjusted = () => (component as unknown as { userAdjusted: boolean }).userAdjusted;

  it('calls invalidateSize when the container changes size', () => {
    fixture.detectChanges();
    const frames = stubFrames();
    const spy = spyOn(leafletMap()!, 'invalidateSize').and.callThrough();

    fixture.componentRef.setInput('height', '300px');
    fixture.detectChanges();
    scheduleResize();
    frames.flush();

    expect(spy).toHaveBeenCalled();
  });

  it('does not count a programmatic fit as a user move', () => {
    fixture.detectChanges();
    const spy = spyOn(leafletMap()!, 'fitBounds').and.callThrough();

    fixture.componentRef.setInput('markers', makeMarkers(3));
    fixture.detectChanges();
    leafletMap()!.fire('zoomstart');

    expect(spy).toHaveBeenCalledWith(jasmine.anything(), jasmine.objectContaining({ animate: false }));
    expect(userAdjusted()).toBeFalse();
  });

  it('re-fits on a second resize as well as the first', () => {
    fixture.componentRef.setInput('markers', makeMarkers(3));
    fixture.detectChanges();
    const frames = stubFrames();
    const fitView = spyOn(component as unknown as { fitView: () => void }, 'fitView').and.callThrough();

    scheduleResize();
    frames.flush();
    expect(fitView).toHaveBeenCalledTimes(1);

    fitView.calls.reset();
    scheduleResize();
    frames.flush();
    expect(fitView).toHaveBeenCalledTimes(1);
  });

  it('stops automatic fits after a pointerdown on the container', () => {
    fixture.componentRef.setInput('markers', makeMarkers(3));
    fixture.detectChanges();
    const frames = stubFrames();
    const fitBounds = spyOn(leafletMap()!, 'fitBounds').and.callThrough();
    const invalidate = spyOn(leafletMap()!, 'invalidateSize').and.callThrough();

    const container = fixture.nativeElement.querySelector('.leaflet-container') as HTMLElement;
    container.dispatchEvent(new Event('pointerdown'));
    expect(userAdjusted()).toBeTrue();

    fitBounds.calls.reset();
    scheduleResize();
    frames.flush();

    expect(invalidate).toHaveBeenCalled();
    expect(fitBounds).not.toHaveBeenCalled();
  });

  it('cancels a pending resize frame when destroyed', () => {
    fixture.detectChanges();
    const frames = stubFrames();
    const cancel = window.cancelAnimationFrame as jasmine.Spy;

    scheduleResize();
    expect(frames.pending()).toBe(1);

    fixture.destroy();

    expect(cancel).toHaveBeenCalled();
    expect(frames.pending()).toBe(0);
  });

  it('fits the view to the markers when there is no boundary', () => {
    fixture.componentRef.setInput('markers', makeMarkers(3));
    fixture.detectChanges();

    const centre = leafletMap()!.getCenter();
    expect(centre.lat).toBeGreaterThan(37.859);
    expect(centre.lat).toBeLessThan(37.863);
  });

  it('removes the map when destroyed', () => {
    fixture.detectChanges();

    fixture.destroy();

    expect(leafletMap()).toBeNull();
  });
});
