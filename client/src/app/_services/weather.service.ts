// src/app/_services/weather.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment.development';
import { throwError, Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { WeatherResponse } from '../_models/weather.model';

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  private weatherUrl = `${environment.weatherApiUrl}/current.json`;

  constructor(private http: HttpClient) {}

  getWeather(location: string): Observable<WeatherResponse> {
    return this.http.get<WeatherResponse>(`${this.weatherUrl}?key=${environment.weatherApiKey}&q=${location}`)
      .pipe(
        catchError(this.handleError.bind(this))
      );
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'Unknown error!';
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Client-side error: ${error.error.message}`;
    } else {
      errorMessage = `Server-side error: ${error.status} ${error.message}`;
    }
    console.error(errorMessage);
    return throwError(() => new Error('There was a problem retrieving weather data; please try again later.'));
  }
}
