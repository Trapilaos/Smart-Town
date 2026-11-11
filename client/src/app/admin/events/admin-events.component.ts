import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { EventService } from '../../_services/event.service';
import { AdminEventDTO, AdminEventRequest } from '../../_models/event.model';
import { getErrorMessages } from '../../_helpers/api-error';

/** Admin table of every event, with a published toggle, edit, and delete. */
@Component({
    selector: 'app-admin-events',
    templateUrl: './admin-events.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminEventsComponent implements OnInit {
  events: AdminEventDTO[] = [];
  loading = true;
  loadError: string | null = null;

  /** Event whose published flag is being saved. */
  togglingId: number | null = null;
  /** Event waiting for a second click on "Delete". */
  confirmDeleteId: number | null = null;
  deletingId: number | null = null;

  constructor(private eventService: EventService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = null;
    this.eventService.getAdminEvents().subscribe({
      next: events => {
        this.events = events;
        this.loading = false;
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  /** Saves the published flag with the event's other fields unchanged. The switch reverts if the save fails. */
  togglePublished(event: AdminEventDTO): void {
    if (this.togglingId !== null) return;

    const next = !event.isPublished;
    this.togglingId = event.id;
    this.eventService.updateAdminEvent(event.id, toRequest(event, next), true).subscribe({
      next: updated => {
        this.replace(updated);
        this.togglingId = null;
        this.toastr.success(updated.isPublished ? `${updated.title} is now published.` : `${updated.title} is now hidden.`);
      },
      error: error => {
        this.togglingId = null;
        this.toastr.error(getErrorMessages(error)[0], 'Could not change the published flag');
      }
    });
  }

  askDelete(id: number): void {
    this.confirmDeleteId = id;
  }

  cancelDelete(): void {
    this.confirmDeleteId = null;
  }

  delete(event: AdminEventDTO): void {
    this.deletingId = event.id;
    this.eventService.deleteAdminEvent(event.id).subscribe({
      next: () => {
        this.events = this.events.filter(item => item.id !== event.id);
        this.confirmDeleteId = null;
        this.deletingId = null;
        this.toastr.success(`${event.title} was deleted.`);
      },
      error: () => {
        // The ErrorInterceptor shows the server message.
        this.deletingId = null;
      }
    });
  }

  private replace(updated: AdminEventDTO): void {
    this.events = this.events.map(item => item.id === updated.id ? updated : item);
  }
}

/** The PUT body keeps every field of the event; only isPublished changes. */
function toRequest(event: AdminEventDTO, isPublished: boolean): AdminEventRequest {
  return {
    title: event.title,
    description: event.description,
    photoUrl: event.photoUrl,
    location: event.location,
    lat: event.lat,
    lng: event.lng,
    date: event.date,
    endDate: event.endDate,
    category: event.category,
    isPublished
  };
}
