import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { catchError, Observable, throwError } from 'rxjs';

// Mirrors API/DTOs/LightingStatusDTO.cs (status is "On" or "Off").
export interface LightingStatus {
  status: string;
  brightness: string | null;
  nextOnTime: string | null;
  nextOffTime: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class LightingService {
  private apiUrl = `${environment.apiUrl}lighting/status`;

  constructor(private http: HttpClient) { }

  getLightingStatus(town: string): Observable<LightingStatus> {
    return this.http.get<LightingStatus>(this.apiUrl, { params: new HttpParams().set('town', town) }).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    const details = error.error instanceof ErrorEvent
      ? `Client-side error: ${error.error.message}`
      : `Server-side error: ${error.status} ${error.message}`;
    console.error(details);
    return throwError(() => new Error('There was a problem retrieving lighting status; please try again later.'));
  }
}
