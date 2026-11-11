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
  isPublished: boolean;
}

/** Upcoming published events, and upcoming unpublished drafts counted apart. */
export interface UpcomingEventCounts {
  published: number;
  drafts: number;
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

  /** Events whose date is now or later. Unpublished events are drafts and are not counted as published. */
  getUpcomingEventCounts(): Observable<UpcomingEventCounts> {
    const now = Date.now();
    return this.http.get<DashboardEvent[]>(this.eventsUrl).pipe(
      map(events => {
        const upcoming = events.filter(event => new Date(event.date).getTime() >= now);
        const published = upcoming.filter(event => event.isPublished).length;
        return { published, drafts: upcoming.length - published };
      })
    );
  }

  getAudit(take: number): Observable<AuditEntry[]> {
    const params = new HttpParams().set('take', String(take));
    return this.http.get<AuditEntry[]>(this.auditUrl, { params });
  }
}
