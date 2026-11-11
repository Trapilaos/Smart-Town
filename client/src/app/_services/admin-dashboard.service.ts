import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AuditEntry } from '../_models/audit.model';
import { WasteBin } from '../_models/waste-bin.model';

/** The admin event fields the dashboard needs. */
export interface DashboardEvent {
  id: number;
  title: string;
  date: string;
}

/** Data sources for the admin dashboard and audit log. Event lists come from here, not from event.service. */
@Injectable({
  providedIn: 'root'
})
export class AdminDashboardService {
  private readonly binsUrl = `${environment.apiUrl}waste/bins`;
  private readonly eventsUrl = `${environment.apiUrl}admin/events`;
  private readonly auditUrl = `${environment.apiUrl}admin/audit`;

  constructor(private http: HttpClient) {}

  getBins(): Observable<WasteBin[]> {
    return this.http.get<WasteBin[]>(this.binsUrl);
  }

  /** Number of events whose date is now or later. */
  getUpcomingEventCount(): Observable<number> {
    const now = Date.now();
    return this.http.get<DashboardEvent[]>(this.eventsUrl).pipe(
      map(events => events.filter(event => new Date(event.date).getTime() >= now).length)
    );
  }

  getAudit(take: number): Observable<AuditEntry[]> {
    const params = new HttpParams().set('take', String(take));
    return this.http.get<AuditEntry[]>(this.auditUrl, { params });
  }
}
