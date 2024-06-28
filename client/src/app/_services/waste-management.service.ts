import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { WasteBin } from '../_models/waste-bin.model';

@Injectable({
  providedIn: 'root'
})
export class WasteManagementService {
  private wasteBinsUrl = 'https://localhost:5001/api/waste';
  private optimalPathUrl = 'https://localhost:5001/api/waste/optimalpath';

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
