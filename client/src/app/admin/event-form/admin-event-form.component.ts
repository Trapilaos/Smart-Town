import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subscription, take } from 'rxjs';
import { EventService } from '../../_services/event.service';
import { EVENT_CATEGORIES, EventCategory } from '../../_models/event.model';
import { MapMarker } from '../../_models/map-marker.model';
import { getErrorMessages } from '../../_helpers/api-error';
import {
  EventFormErrors,
  EventFormModel,
  emptyEventForm,
  formFromEvent,
  toEventRequest,
  validateEventForm
} from './event-form.helpers';

/** Shared create and edit form for events. The route decides the mode: /admin/events/new or /admin/events/:id/edit. */
@Component({
    selector: 'app-admin-event-form',
    templateUrl: './admin-event-form.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminEventFormComponent implements OnInit, OnDestroy {
  readonly categories: readonly EventCategory[] = EVENT_CATEGORIES;

  /** Null when creating. */
  eventId: number | null = null;
  model: EventFormModel = emptyEventForm();

  loading = false;
  loadError: string | null = null;
  saving = false;
  /** Set on the first submit, so errors only show once the admin has tried to save. */
  attempted = false;
  serverErrors: string[] = [];

  markers: MapMarker[] = [];

  private routeSub: Subscription | null = null;
  private loadSub: Subscription | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private toastr: ToastrService
  ) {}

  get isEdit(): boolean {
    return this.eventId !== null;
  }

  get errors(): EventFormErrors {
    return validateEventForm(this.model);
  }

  get hasErrors(): boolean {
    return Object.keys(this.errors).length > 0;
  }

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe(params => {
      const raw = params.get('id');
      this.eventId = raw === null ? null : Number(raw);
      if (this.eventId !== null) this.loadEvent(this.eventId);
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.loadSub?.unsubscribe();
  }

  /** Edit mode has no single-event admin endpoint, so the admin list is searched for the event. */
  private loadEvent(id: number): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.loadError = null;
    this.loadSub = this.eventService.getAdminEvents().pipe(take(1)).subscribe({
      next: events => {
        const found = Number.isInteger(id) ? events.find(event => event.id === id) : undefined;
        if (!found) {
          this.loadError = 'This event could not be found.';
        } else {
          this.model = formFromEvent(found);
          this.syncMarkers();
        }
        this.loading = false;
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  onMapClick(point: { lat: number; lng: number }): void {
    this.model.lat = roundCoordinate(point.lat);
    this.model.lng = roundCoordinate(point.lng);
    this.syncMarkers();
  }

  clearLocation(): void {
    this.model.lat = null;
    this.model.lng = null;
    this.syncMarkers();
  }

  save(): void {
    this.attempted = true;
    this.serverErrors = [];
    if (this.saving || this.hasErrors) return;

    const request = toEventRequest(this.model);
    this.saving = true;

    const save$ = this.eventId === null
      ? this.eventService.createAdminEvent(request)
      : this.eventService.updateAdminEvent(this.eventId, request, true);

    save$.pipe(take(1)).subscribe({
      next: saved => {
        this.saving = false;
        this.toastr.success(this.eventId === null ? `${saved.title} was created.` : `${saved.title} was saved.`);
        this.router.navigate(['/admin/events']);
      },
      error: error => {
        this.saving = false;
        this.serverErrors = getErrorMessages(error);
      }
    });
  }

  private syncMarkers(): void {
    const { lat, lng, title, location } = this.model;
    this.markers = lat === null || lng === null ? [] : [{
      id: 'picked',
      lat,
      lng,
      color: '#0d6efd',
      iconClass: 'fas fa-calendar-alt',
      title: title.trim() || 'Event location',
      subtitle: location.trim() || undefined
    }];
  }
}

/** Six decimals is about 0.1 m, which is more than the map click can resolve. */
function roundCoordinate(value: number): number {
  return Math.round(value * 1e6) / 1e6;
}
