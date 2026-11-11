import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable, catchError, of, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import {
  BoundaryCandidate,
  Municipality,
  MunicipalityUpdateRequest
} from '../_models/municipality.model';

/**
 * Reads and writes the municipality settings. The GET result is cached for the
 * life of the app, so every map and page shares one request. A successful write
 * replaces the cached value; a failed read is not cached.
 */
@Injectable({
  providedIn: 'root'
})
export class MunicipalityService {
  private readonly apiUrl = `${environment.apiUrl}municipality`;
  private readonly adminUrl = `${environment.apiUrl}admin/municipality`;
  private cached$: Observable<Municipality> | null = null;

  constructor(private http: HttpClient) {}

  /** The municipality, from the cache when it has already been loaded. */
  get(): Observable<Municipality> {
    if (!this.cached$) {
      this.cached$ = this.http.get<Municipality>(this.apiUrl).pipe(
        catchError(error => {
          this.cached$ = null;
          return throwError(() => error);
        }),
        shareReplay(1)
      );
    }
    return this.cached$;
  }

  /** Saves the settings (and optionally a new boundary). Admin forms show errors inline. */
  update(request: MunicipalityUpdateRequest): Observable<Municipality> {
    return this.http.put<Municipality>(this.adminUrl, request, { context: inlineErrors() }).pipe(
      tap(updated => this.cached$ = of(updated))
    );
  }

  searchBoundary(query: string): Observable<BoundaryCandidate[]> {
    return this.http.post<BoundaryCandidate[]>(`${this.adminUrl}/boundary/search`, { query }, { context: inlineErrors() });
  }

  applyBoundary(osmType: string, osmId: number): Observable<Municipality> {
    return this.http.post<Municipality>(`${this.adminUrl}/boundary/apply`, { osmType, osmId }, { context: inlineErrors() }).pipe(
      tap(updated => this.cached$ = of(updated))
    );
  }
}

function inlineErrors(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
