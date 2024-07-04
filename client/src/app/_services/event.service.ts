import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Event } from '../_models/event.model';
import { environment } from 'src/environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = environment.apiUrl + 'events';

  constructor(private http: HttpClient) {}

  getEvents(): Observable<Event[]> {
    return this.http.get<Event[]>(this.apiUrl);
  }

  declareInterest(eventId: number): Observable<Event> {
    return this.http.post<Event>(`${this.apiUrl}/${eventId}/interest`, {});
  }

  createEvent(event: Event): Observable<Event> {
    return this.http.post<Event>(this.apiUrl, event);
  }

  deleteEvent(eventId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}`);
  }
}
