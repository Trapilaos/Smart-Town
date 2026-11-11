import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Municipality, MunicipalityUpdateRequest } from '../_models/municipality.model';
import { MunicipalityService } from './municipality.service';

describe('MunicipalityService', () => {
  let service: MunicipalityService;
  let controller: HttpTestingController;

  const municipality = (name: string): Municipality => ({
    name,
    centerLatitude: 37.865,
    centerLongitude: 23.753,
    defaultZoom: 14,
    timeZoneId: 'Europe/Athens',
    boundary: null,
    boundaryIsApproximate: true,
    boundarySource: null,
    boundaryFetchedAt: null,
    streetlightCount: 1200,
    streetlightWatts: 60,
    updatedAt: '2026-01-01T00:00:00Z'
  });

  const isGet = (req: { method: string; url: string }) => req.method === 'GET' && req.url.endsWith('municipality');
  const isPut = (req: { method: string; url: string }) => req.method === 'PUT' && req.url.endsWith('municipality');

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(MunicipalityService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('requests the municipality once and serves later reads from the cache', () => {
    const seen: string[] = [];
    service.get().subscribe(m => seen.push(m.name));
    service.get().subscribe(m => seen.push(m.name));

    controller.expectOne(isGet).flush(municipality('Glyfada'));

    expect(seen).toEqual(['Glyfada', 'Glyfada']);
    service.get().subscribe(m => seen.push(m.name));
    expect(seen.length).toBe(3);
  });

  it('does not cache a failed read, so the next call asks again', () => {
    let failed = false;
    service.get().subscribe({ error: () => failed = true });
    controller.expectOne(isGet).flush('boom', { status: 500, statusText: 'Server Error' });
    expect(failed).toBeTrue();

    let name = '';
    service.get().subscribe(m => name = m.name);
    controller.expectOne(isGet).flush(municipality('Glyfada'));
    expect(name).toBe('Glyfada');
  });

  it('replaces the cached value after a successful update', () => {
    service.get().subscribe();
    controller.expectOne(isGet).flush(municipality('Glyfada'));

    const request: MunicipalityUpdateRequest = {
      name: 'Glyfada Municipality',
      centerLatitude: 37.865,
      centerLongitude: 23.753,
      defaultZoom: 15,
      streetlightCount: 1300,
      streetlightWatts: 50
    };
    service.update(request).subscribe();
    controller.expectOne(isPut).flush(municipality('Glyfada Municipality'));

    let name = '';
    service.get().subscribe(m => name = m.name);
    controller.expectNone(isGet);
    expect(name).toBe('Glyfada Municipality');
  });
});
