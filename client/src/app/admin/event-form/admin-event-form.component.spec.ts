import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ToastrService } from 'ngx-toastr';
import { of } from 'rxjs';
import { AdminEventFormComponent } from './admin-event-form.component';

describe('AdminEventFormComponent', () => {
  let fixture: ComponentFixture<AdminEventFormComponent>;
  let component: AdminEventFormComponent;
  let controller: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  let toastr: { success: jasmine.Spy; error: jasmine.Spy };

  function createWithRoute(params: Record<string, string>): void {
    TestBed.configureTestingModule({
      declarations: [AdminEventFormComponent],
      imports: [FormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap(params)) } },
        { provide: Router, useValue: router },
        { provide: ToastrService, useValue: toastr }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    controller = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminEventFormComponent);
    component = fixture.componentInstance;
  }

  beforeEach(() => {
    router = { navigate: jasmine.createSpy('navigate').and.resolveTo(true) };
    toastr = { success: jasmine.createSpy('success'), error: jasmine.createSpy('error') };
  });

  afterEach(() => controller?.verify());

  it('shows inline errors and sends nothing when a new event is saved empty', () => {
    createWithRoute({});
    fixture.detectChanges();

    component.save();
    fixture.detectChanges();

    controller.match(() => true).forEach(req => req.flush({}));
    expect(component.isEdit).toBeFalse();
    expect(component.attempted).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Enter a title.');
    expect(fixture.nativeElement.textContent).toContain('Choose a category.');
  });

  it('creates an event with UTC times and navigates back to the list', () => {
    createWithRoute({});
    fixture.detectChanges();

    component.model.title = 'Summer concert';
    component.model.category = 'Culture';
    component.model.date = '2026-07-01T20:00';
    component.model.isPublished = true;
    component.save();

    const req = controller.expectOne(r => r.method === 'POST' && r.url.endsWith('admin/events'));
    expect(req.request.body.date).toBe('2026-07-01T17:00:00Z');
    expect(req.request.body.endDate).toBeNull();
    expect(req.request.body.isPublished).toBeTrue();
    req.flush({ id: 1, title: 'Summer concert' });

    expect(toastr.success).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/events']);
  });

  it('loads the event in edit mode and sends a PUT with the picked coordinates', () => {
    createWithRoute({ id: '5' });
    fixture.detectChanges();

    controller.expectOne(r => r.method === 'GET' && r.url.endsWith('admin/events'))
      .flush([{
        id: 5, title: 'Market', description: '', photoUrl: null, location: 'Square',
        lat: 37.9, lng: 23.7, date: '2026-07-01T17:00:00Z', endDate: null,
        category: 'Culture', interestedCount: 0, isPublished: false, createdAt: '2026-06-01T00:00:00Z'
      }]);
    fixture.detectChanges();

    expect(component.isEdit).toBeTrue();
    expect(component.model.date).toBe('2026-07-01T20:00');
    expect(component.markers.length).toBe(1);

    component.save();
    const req = controller.expectOne(r => r.method === 'PUT' && r.url.endsWith('admin/events/5'));
    expect(req.request.body.lat).toBe(37.9);
    expect(req.request.body.lng).toBe(23.7);
    req.flush({ id: 5, title: 'Market' });
  });
});
