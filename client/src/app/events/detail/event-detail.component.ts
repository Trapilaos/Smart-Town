import { Component, OnDestroy, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { Subscription, take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../../_services/account.service';
import { EventService } from '../../_services/event.service';
import { EventDTO } from '../../_models/event.model';
import { MapMarker } from '../../_models/map-marker.model';
import { getErrorMessages } from '../../_helpers/api-error';
import { interestLabel, isPastEvent } from '../event.helpers';

/** One public event, with a map when it has coordinates. */
@Component({
    selector: 'app-event-detail',
    templateUrl: './event-detail.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class EventDetailComponent implements OnInit, OnDestroy {
  event: EventDTO | null = null;
  markers: MapMarker[] = [];
  loading = true;
  notFound = false;
  error: string | null = null;

  loggedIn = false;
  interestPending = false;

  readonly interestLabel = interestLabel;
  readonly isPastEvent = isPastEvent;

  private routeSub: Subscription | null = null;
  private loadSub: Subscription | null = null;
  private userSub: Subscription | null = null;

  constructor(
    private route: ActivatedRoute,
    private eventService: EventService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.userSub = this.accountService.currentUser$.subscribe(user => this.loggedIn = !!user);
    this.routeSub = this.route.paramMap.subscribe(params => this.load(Number(params.get('id'))));
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.loadSub?.unsubscribe();
    this.userSub?.unsubscribe();
  }

  load(id: number): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.notFound = false;
    this.error = null;
    this.event = null;
    this.markers = [];

    if (!Number.isInteger(id) || id <= 0) {
      this.notFound = true;
      this.loading = false;
      return;
    }

    this.loadSub = this.eventService.getEvent(id).subscribe({
      next: event => {
        this.event = event;
        this.markers = eventMarkers(event);
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.notFound = true;
        } else {
          this.error = getErrorMessages(error)[0];
        }
      }
    });
  }

  toggleInterest(): void {
    const event = this.event;
    if (!event || this.interestPending) return;

    this.interestPending = true;
    const request = event.isInterested
      ? this.eventService.withdrawInterest(event.id)
      : this.eventService.declareInterest(event.id);

    request.pipe(take(1)).subscribe({
      next: updated => {
        this.event = updated;
        this.interestPending = false;
        this.toastr.success(updated.isInterested
          ? `You are marked as interested in ${updated.title}.`
          : `You are no longer interested in ${updated.title}.`);
      },
      error: () => {
        // The ErrorInterceptor shows the server message.
        this.interestPending = false;
      }
    });
  }
}

/** A single marker for the event's location, or none when it has no coordinates. */
function eventMarkers(event: EventDTO): MapMarker[] {
  if (event.lat === null || event.lng === null) return [];
  return [{
    id: event.id,
    lat: event.lat,
    lng: event.lng,
    color: '#0d6efd',
    iconClass: 'fas fa-calendar-alt',
    title: event.title,
    subtitle: event.location ?? undefined
  }];
}
