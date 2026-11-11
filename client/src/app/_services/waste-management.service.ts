import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { WasteBin } from '../_models/waste-bin.model';

@Injectable({
  providedIn: 'root'
})
export class WasteManagementService {
  private wasteBinsUrl = `${environment.apiUrl}waste`;
  private optimalPathUrl = `${environment.apiUrl}waste/optimalpath`;

  constructor(private http: HttpClient) {}

  getWasteBins(): Observable<WasteBin[]> {
    return this.http.get<WasteBin[]>(this.wasteBinsUrl).pipe(
      catchError(this.handleError)
    );
  }

  getOptimalPath(): Observable<string[]> {
    return this.http.get<string[]>(this.optimalPathUrl).pipe(
      catchError(this.handleError)
    );
  }

  updateWasteBin(wasteBin: WasteBin): Observable<WasteBin> {
    return this.http.put<WasteBin>(`${this.wasteBinsUrl}/${wasteBin.id}`, wasteBin).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse) {
    return throwError('Something bad happened; please try again later.');
  }
}
