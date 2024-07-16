import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ParkingSpace } from '../_models/parking-space.model';
import { TrafficData } from '../_models/traffic-data.model';
import { Reservation } from '../_models/reservation.model';
import { environment } from 'src/environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class TrafficAndParkingService {
  private trafficUrl = `${environment.apiUrl}traffic/current`;
  private parkingUrl = `${environment.apiUrl}parking`;

  constructor(private http: HttpClient) { }

  getTrafficData(): Observable<TrafficData[]> {
    return this.http.get<TrafficData[]>(this.trafficUrl).pipe(
      catchError(this.handleError)
    );
  }

  getParkingSpaces(): Observable<ParkingSpace[]> {
    return this.http.get<ParkingSpace[]>(this.parkingUrl).pipe(
      catchError(this.handleError)
    );
  }

  reserveParkingSpace(reservation: Reservation): Observable<any> {
    return this.http.post(`${this.parkingUrl}/reserve`, reservation).pipe(
      tap(response => {
        console.log('API response:', response);
      }),
      catchError(this.handleError)
    );
  }

  getActiveReservation(): Observable<Reservation> {
    return this.http.get<Reservation>(`${this.parkingUrl}/active-reservation`).pipe(
      tap(response => {
        console.log('API response:', response);
      }),
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse) {
    if (error.error instanceof ErrorEvent) {
      console.error('An error occurred:', error.error.message);
    } else {
      console.error(
        `Backend returned code ${error.status}, ` +
        `body was: ${error.error}`);
    }
    return throwError('Something bad happened; please try again later.');
  }
}
