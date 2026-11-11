import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminEventDTO, AdminEventRequest, EventCategory, EventDTO } from '../_models/event.model';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import { environment } from 'src/environments/environment';

export interface EventListQuery {
  from?: string;
  category?: EventCategory | null;
  includePast?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = `${environment.apiUrl}events`;
  private adminUrl = `${environment.apiUrl}admin/events`;

  constructor(private http: HttpClient) {}

  /** Published events. Upcoming only unless `includePast` is set. */
  getEvents(query: EventListQuery = {}): Observable<EventDTO[]> {
    let params = new HttpParams();
    if (query.from) params = params.set('from', query.from);
    if (query.category) params = params.set('category', query.category);
    if (query.includePast) params = params.set('includePast', 'true');
    return this.http.get<EventDTO[]>(this.apiUrl, { params });
  }

  /** One published event. A missing or unpublished event returns 404, which the caller shows inline. */
  getEvent(id: number): Observable<EventDTO> {
    return this.http.get<EventDTO>(`${this.apiUrl}/${id}`, { context: inlineErrors() });
  }

  /** Member: marks the user as interested (idempotent). Returns the updated event. */
  declareInterest(eventId: number): Observable<EventDTO> {
    return this.http.post<EventDTO>(`${this.apiUrl}/${eventId}/interest`, {});
  }

  /** Member: withdraws interest. Returns the updated event. */
  withdrawInterest(eventId: number): Observable<EventDTO> {
    return this.http.delete<EventDTO>(`${this.apiUrl}/${eventId}/interest`);
  }

  /** Admin: every event, including unpublished ones. */
  getAdminEvents(): Observable<AdminEventDTO[]> {
    return this.http.get<AdminEventDTO[]>(this.adminUrl);
  }

  /** Admin form: validation errors are shown inline, so the interceptor toast is skipped. */
  createAdminEvent(request: AdminEventRequest): Observable<AdminEventDTO> {
    return this.http.post<AdminEventDTO>(this.adminUrl, request, { context: inlineErrors() });
  }

  updateAdminEvent(id: number, request: AdminEventRequest, inlineErrorsOnly = false): Observable<AdminEventDTO> {
    return this.http.put<AdminEventDTO>(`${this.adminUrl}/${id}`, request,
      inlineErrorsOnly ? { context: inlineErrors() } : {});
  }

  deleteAdminEvent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}`);
  }
}

function inlineErrors(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
