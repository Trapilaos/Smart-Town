import { Component, ChangeDetectionStrategy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { EventService, CreateEventRequest } from '../../_services/event.service';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages } from '../../_helpers/api-error';

type EventField = 'title' | 'description' | 'photoUrl' | 'date';

export const TITLE_MAX = 100;
export const DESCRIPTION_MAX = 1000;
export const PHOTO_URL_MAX = 2048;
const HTTP_URL = /^https?:\/\/\S+$/i;

/** Formats a date as YYYY-MM-DD in local time, the format of a date input. */
export function toDateInputValue(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

@Component({
    selector: 'app-event-create',
    templateUrl: './event-create.component.html',
    styleUrls: ['./event-create.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class EventCreateComponent {
  readonly titleMax = TITLE_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;
  readonly today = toDateInputValue(new Date());

  model = { title: '', description: '', photoUrl: '', date: this.today };

  touched: Partial<Record<EventField, boolean>> = {};
  submitAttempted = false;
  submitting = false;
  /** Errors from a 400 or 401 response, shown inline because the interceptor toast is suppressed. */
  submitErrors: string[] = [];

  constructor(private eventService: EventService, private router: Router, private toastr: ToastrService) {}

  get errors(): Partial<Record<EventField, string>> {
    const errors: Partial<Record<EventField, string>> = {};
    const title = this.model.title.trim();
    const photoUrl = this.model.photoUrl.trim();

    if (!title) {
      errors.title = 'Enter a title.';
    } else if (title.length > TITLE_MAX) {
      errors.title = `The title can be at most ${TITLE_MAX} characters.`;
    }

    if (this.model.description.length > DESCRIPTION_MAX) {
      errors.description = `The description can be at most ${DESCRIPTION_MAX} characters.`;
    }

    if (photoUrl && (photoUrl.length > PHOTO_URL_MAX || !HTTP_URL.test(photoUrl))) {
      errors.photoUrl = 'Enter a full link that starts with http:// or https://.';
    }

    if (!this.model.date) {
      errors.date = 'Choose a date.';
    } else if (this.model.date < this.today) {
      errors.date = 'The date cannot be in the past.';
    }

    return errors;
  }

  get isValid(): boolean {
    return Object.keys(this.errors).length === 0;
  }

  touch(field: EventField): void {
    this.touched[field] = true;
  }

  showError(field: EventField): boolean {
    return !!this.errors[field] && (this.submitAttempted || !!this.touched[field]);
  }

  createEvent(): void {
    this.submitAttempted = true;
    this.submitErrors = [];
    if (!this.isValid || this.submitting) {
      return;
    }

    const request: CreateEventRequest = {
      title: this.model.title.trim(),
      description: this.model.description.trim(),
      photoUrl: this.model.photoUrl.trim(),
      date: this.model.date
    };

    this.submitting = true;
    this.eventService.createEvent(request).subscribe({
      next: () => {
        this.submitting = false;
        this.toastr.success('Event created successfully');
        this.router.navigateByUrl('/admin');
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;
        // The interceptor toasts 403 and unexpected errors itself; 400 and 401 are shown here.
        if (error.status === 400 || error.status === 401) {
          this.submitErrors = getErrorMessages(error);
        }
      }
    });
  }
}
