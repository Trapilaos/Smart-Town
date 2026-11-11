import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LightingService } from './lighting.service';

describe('LightingService', () => {
  let service: LightingService;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(LightingService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('requests a 7-day schedule by default', () => {
    service.getSchedule().subscribe();
    const req = controller.expectOne(r => r.url.endsWith('lighting/schedule'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('days')).toBe('7');
    req.flush([]);
  });

  it('passes the requested number of days', () => {
    service.getSchedule(31).subscribe();
    const req = controller.expectOne(r => r.url.endsWith('lighting/schedule'));
    expect(req.request.params.get('days')).toBe('31');
    req.flush([]);
  });

  it('reads the current status', () => {
    let status = '';
    service.getNow().subscribe(now => status = now.status);
    const req = controller.expectOne(r => r.url.endsWith('lighting/now'));
    req.flush({ status: 'Dimmed', brightnessPercent: 50, nextChange: { at: '2026-06-21T05:00:00+03:00', toStatus: 'Off' } });
    expect(status).toBe('Dimmed');
  });
});
