import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, of, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import { TrafficData } from '../_models/traffic-data.model';
import {
  ActiveReservation,
  ParkingReservation,
  ParkingSpace,
  ParkingSpaceRequest,
  ReservationRequest
} from '../_models/parking.model';

/**
 * Traffic readings, public parking spaces, reservations and admin parking CRUD.
 * Write calls set SKIP_ERROR_TOAST because the pages show errors inline.
 */
@Injectable({
  providedIn: 'root'
})
export class TrafficAndParkingService {
  private readonly trafficUrl = `${environment.apiUrl}traffic/current`;
  private readonly parkingUrl = `${environment.apiUrl}parking`;
  private readonly adminParkingUrl = `${environment.apiUrl}admin/parking`;

  constructor(private http: HttpClient) {}

  getTrafficData(): Observable<TrafficData[]> {
    return this.http.get<TrafficData[]>(this.trafficUrl);
  }

  /** Active public parking spaces with free and full counts. */
  getParkingSpaces(): Observable<ParkingSpace[]> {
    return this.http.get<ParkingSpace[]>(this.parkingUrl);
  }

  reserveParkingSpace(request: ReservationRequest): Observable<void> {
    return this.http.post<void>(`${this.parkingUrl}/reserve`, request, { context: inlineErrors() });
  }

  /** The API returns 404 when the member has no active reservation. That is a normal state, not an error. */
  getActiveReservation(): Observable<ActiveReservation | null> {
    return this.http.get<ActiveReservation>(`${this.parkingUrl}/active-reservation`).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of(null);
        }
        return throwError(() => error);
      })
    );
  }

  getAdminParking(): Observable<ParkingSpace[]> {
    return this.http.get<ParkingSpace[]>(this.adminParkingUrl, { context: inlineErrors() });
  }

  createParking(request: ParkingSpaceRequest): Observable<ParkingSpace> {
    return this.http.post<ParkingSpace>(this.adminParkingUrl, request, { context: inlineErrors() });
  }

  updateParking(id: number, request: ParkingSpaceRequest): Observable<ParkingSpace> {
    return this.http.put<ParkingSpace>(`${this.adminParkingUrl}/${id}`, request, { context: inlineErrors() });
  }

  deleteParking(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminParkingUrl}/${id}`, { context: inlineErrors() });
  }

  getReservations(id: number): Observable<ParkingReservation[]> {
    return this.http.get<ParkingReservation[]>(`${this.adminParkingUrl}/${id}/reservations`, { context: inlineErrors() });
  }
}

function inlineErrors(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
