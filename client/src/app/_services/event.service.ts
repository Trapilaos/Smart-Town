import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Event } from '../_models/event.model';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';

/** Body for POST events. The date is the YYYY-MM-DD string from the date input. */
export interface CreateEventRequest {
  title: string;
  description: string;
  photoUrl: string;
  date: string;
}

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = `${environment.apiUrl}events`;

  constructor(private http: HttpClient) {}

  getEvents(): Observable<Event[]> {
    return this.http.get<Event[]>(this.apiUrl);
  }

  declareInterest(eventId: number): Observable<Event> {
    return this.http.post<Event>(`${this.apiUrl}/${eventId}/interest`, {});
  }

  /** The create form shows its errors inline, so the interceptor toast is suppressed. */
  createEvent(event: CreateEventRequest): Observable<Event> {
    const context = new HttpContext().set(SKIP_ERROR_TOAST, true);
    return this.http.post<Event>(this.apiUrl, event, { context });
  }

  deleteEvent(eventId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}`);
  }
}
