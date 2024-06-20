// src/app/_services/lighting.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { catchError, Observable , throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LightingService {
  private apiUrl = `${environment.apiUrl}lighting/status`;

  constructor(private http: HttpClient) {}

  getLightingStatus(town: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}?town=${town}`).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Unknown error!';
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Client-side error: ${error.error.message}`;
    } else {
      errorMessage = `Server-side error: ${error.status} ${error.message}`;
    }
    console.error(errorMessage);
    return throwError(() => new Error('There was a problem retrieving lighting status; please try again later.'));
  }
}
