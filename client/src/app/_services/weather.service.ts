// src/app/_services/weather.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment.development';
import { catchError } from 'rxjs/operators';
import { throwError, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class WeatherService {
    private weatherUrl = `${environment.weatherApiUrl}/current.json`;

    constructor(private http: HttpClient) {}

    getWeather(location: string) {
        return this.http.get(`${this.weatherUrl}?key=${environment.weatherApiKey}&q=${location}`);
    }
    private handleError(error: HttpErrorResponse) {
        let errorMessage = 'Unknown error!';

        if (error.error instanceof ErrorEvent) {
            // Client-side or network error
            errorMessage = `Client-side error: ${error.error.message}`;
        } else {
            // Backend error
            errorMessage = `Server-side error: ${error.status} ${error.message}`;
        }

        // Log the error to the console (or send to logging server)
        console.error(errorMessage);

        // Return a user-friendly error message
        return throwError(() => new Error('There was a problem retrieving weather data; please try again later.'));

    }
}
