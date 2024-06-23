import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ParkingSpace } from '../_models/parking-space.model';
import { TrafficData } from '../_models/traffic-data.model';
import { Reservation } from '../_models/reservation.model';

@Injectable({
  providedIn: 'root'
})
export class TrafficAndParkingService {
  private trafficUrl = 'https://localhost:5001/api/traffic/current';
  private parkingUrl = 'https://localhost:5001/api/parking';

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

  reserveParkingSpace(reservation: Reservation): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.parkingUrl}/reserve`, reservation).pipe(
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
