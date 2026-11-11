import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { catchError, Observable, throwError } from 'rxjs';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';

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
    // The lighting page shows its own inline warning, so suppress the interceptor toast.
    const context = new HttpContext().set(SKIP_ERROR_TOAST, true);
    return this.http.get<LightingStatus>(this.apiUrl, { params: new HttpParams().set('town', town), context }).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    return throwError(() => new Error('There was a problem retrieving lighting status; please try again later.'));
  }
}
