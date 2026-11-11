import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LightingNow, LightingScheduleDay } from '../_models/lighting.model';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LightingService {
  private apiUrl = `${environment.apiUrl}lighting`;

  constructor(private http: HttpClient) {}

  /** Nightly schedule for the next `days` days (1 to 31). */
  getSchedule(days = 7): Observable<LightingScheduleDay[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<LightingScheduleDay[]>(`${this.apiUrl}/schedule`, { params });
  }

  /** Current lighting status and the next change. */
  getNow(): Observable<LightingNow> {
    return this.http.get<LightingNow>(`${this.apiUrl}/now`);
  }
}
