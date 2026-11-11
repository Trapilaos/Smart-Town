import { Component, OnDestroy, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Subscription, take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../../_services/account.service';
import { EventService } from '../../_services/event.service';
import { EVENT_CATEGORIES, EventCategory, EventDTO } from '../../_models/event.model';
import { getErrorMessages } from '../../_helpers/api-error';
import { interestLabel, isPastEvent } from '../event.helpers';

/** Public events list with category chips, an upcoming/past toggle, and interest for logged-in users. */
@Component({
    selector: 'app-events-list',
    templateUrl: './events-list.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class EventsListComponent implements OnInit, OnDestroy {
  readonly categories: readonly EventCategory[] = EVENT_CATEGORIES;

  /** null means every category. */
  selectedCategory: EventCategory | null = null;
  showPast = false;

  events: EventDTO[] = [];
  loading = true;
  error: string | null = null;

  loggedIn = false;
  /** Event whose interest request is in flight. */
  pendingId: number | null = null;

  readonly interestLabel = interestLabel;

  private loadSub: Subscription | null = null;
  private userSub: Subscription | null = null;

  constructor(
    private eventService: EventService,
    private accountService: AccountService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.userSub = this.accountService.currentUser$.subscribe(user => this.loggedIn = !!user);
    this.load();
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
    this.loadSub?.unsubscribe();
  }

  selectCategory(category: EventCategory | null): void {
    if (this.selectedCategory === category) return;
    this.selectedCategory = category;
    this.load();
  }

  setShowPast(showPast: boolean): void {
    if (this.showPast === showPast) return;
    this.showPast = showPast;
    this.load();
  }

  load(): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.error = null;

    this.loadSub = this.eventService.getEvents({
      category: this.selectedCategory,
      includePast: this.showPast
    }).subscribe({
      next: events => {
        // The past view shows only events that have ended, whatever the API returns.
        const now = new Date();
        this.events = this.showPast ? events.filter(event => isPastEvent(event, now)) : events;
        this.loading = false;
      },
      error: error => {
        this.error = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  toggleInterest(event: EventDTO): void {
    if (this.pendingId !== null) return;

    this.pendingId = event.id;
    const request = event.isInterested
      ? this.eventService.withdrawInterest(event.id)
      : this.eventService.declareInterest(event.id);

    request.pipe(take(1)).subscribe({
      next: updated => {
        this.events = this.events.map(item => item.id === updated.id ? updated : item);
        this.pendingId = null;
        this.toastr.success(updated.isInterested
          ? `You are marked as interested in ${updated.title}.`
          : `You are no longer interested in ${updated.title}.`);
      },
      error: () => {
        // The ErrorInterceptor shows the server message.
        this.pendingId = null;
      }
    });
  }
}
