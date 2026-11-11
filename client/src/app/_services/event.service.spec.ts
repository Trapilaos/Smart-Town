import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EventService } from './event.service';
import { AdminEventDTO, AdminEventRequest, EventDTO } from '../_models/event.model';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';

describe('EventService', () => {
  let service: EventService;
  let controller: HttpTestingController;

  const event: EventDTO = {
    id: 7,
    title: 'Market',
    description: 'Local produce',
    photoUrl: null,
    location: 'Square',
    lat: null,
    lng: null,
    date: '2026-11-20T09:00:00Z',
    endDate: null,
    category: 'Culture',
    interestedCount: 2,
    isInterested: false
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EventService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('lists upcoming events by default, with no query string', () => {
    service.getEvents().subscribe();
    const req = controller.expectOne(r => r.url.endsWith('events'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([event]);
  });

  it('sends the category and includePast filters', () => {
    service.getEvents({ category: 'Sports', includePast: true }).subscribe();
    const req = controller.expectOne(r => r.url.endsWith('events'));
    expect(req.request.params.get('category')).toBe('Sports');
    expect(req.request.params.get('includePast')).toBe('true');
    req.flush([]);
  });

  it('declares interest with POST and withdraws it with DELETE, both on the interest route', () => {
    service.declareInterest(7).subscribe();
    const post = controller.expectOne(r => r.url.endsWith('events/7/interest'));
    expect(post.request.method).toBe('POST');
    post.flush({ ...event, isInterested: true });

    service.withdrawInterest(7).subscribe();
    const del = controller.expectOne(r => r.url.endsWith('events/7/interest'));
    expect(del.request.method).toBe('DELETE');
    del.flush(event);
  });

  it('skips the global error toast for a single-event read, so the page can show 404 inline', () => {
    service.getEvent(99).subscribe({ error: () => {} });
    const req = controller.expectOne(r => r.url.endsWith('events/99'));
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeTrue();
    req.flush('missing', { status: 404, statusText: 'Not Found' });
  });

  it('uses the admin endpoints for list, create, update and delete', () => {
    const admin: AdminEventDTO = {
      ...event, isPublished: false, createdAt: '2026-11-01T00:00:00Z'
    };
    const request: AdminEventRequest = {
      title: 'Market', date: '2026-11-20T09:00:00Z', category: 'Culture', isPublished: true
    };

    service.getAdminEvents().subscribe();
    controller.expectOne(r => r.method === 'GET' && r.url.endsWith('admin/events')).flush([admin]);

    service.createAdminEvent(request).subscribe();
    controller.expectOne(r => r.method === 'POST' && r.url.endsWith('admin/events')).flush(admin);

    service.updateAdminEvent(7, request).subscribe();
    controller.expectOne(r => r.method === 'PUT' && r.url.endsWith('admin/events/7')).flush(admin);

    service.deleteAdminEvent(7).subscribe();
    controller.expectOne(r => r.method === 'DELETE' && r.url.endsWith('admin/events/7')).flush(null, { status: 204, statusText: 'No Content' });

    expect(controller.match(() => true).length).toBe(0);
  });
});
