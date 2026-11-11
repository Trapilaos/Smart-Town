import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ParkingSpace } from '../_models/parking-space.model';
import { TrafficData } from '../_models/traffic-data.model';
import { Reservation } from '../_models/reservation.model';
import { environment } from 'src/environments/environment';

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
      catchError(this.handleError)
    );
  }

  // The API returns 404 when the user has no active reservation. That is a normal state, not an error.
  getActiveReservation(): Observable<Reservation | null> {
    return this.http.get<Reservation>(`${this.parkingUrl}/active-reservation`).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of(null);
        }
        return this.handleError(error);
      })
    );
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => 'Something bad happened; please try again later.');
  }
}
