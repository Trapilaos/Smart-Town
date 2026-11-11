import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import { CollectionRoute, WasteBin, WasteBinRequest, WasteType } from '../_models/waste-bin.model';

/**
 * Bins and collection routes. Every call sets SKIP_ERROR_TOAST, because the
 * pages show errors inline (for example "Bin must be inside the municipality boundary").
 */
@Injectable({
  providedIn: 'root'
})
export class WasteService {
  private readonly binsUrl = `${environment.apiUrl}waste/bins`;
  private readonly adminBinsUrl = `${environment.apiUrl}admin/waste/bins`;
  private readonly routeUrl = `${environment.apiUrl}admin/waste/route`;

  constructor(private http: HttpClient) {}

  /** Active bins, optionally filtered by type. */
  getBins(type: WasteType | '' = ''): Observable<WasteBin[]> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.get<WasteBin[]>(this.binsUrl, { params, context: inlineErrors() });
  }

  createBin(request: WasteBinRequest): Observable<WasteBin> {
    return this.http.post<WasteBin>(this.adminBinsUrl, request, { context: inlineErrors() });
  }

  updateBin(id: number, request: WasteBinRequest): Observable<WasteBin> {
    return this.http.put<WasteBin>(`${this.adminBinsUrl}/${id}`, request, { context: inlineErrors() });
  }

  /** Soft delete on the server (the bin is set inactive). */
  deleteBin(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminBinsUrl}/${id}`, { context: inlineErrors() });
  }

  emptyBin(id: number): Observable<WasteBin> {
    return this.http.put<WasteBin>(`${this.adminBinsUrl}/${id}/empty`, {}, { context: inlineErrors() });
  }

  /** The collection route for bins at or above `minFillPercent`. */
  getRoute(type: WasteType | '', minFillPercent: number): Observable<CollectionRoute> {
    let params = new HttpParams().set('minFillPercent', minFillPercent);
    if (type) params = params.set('type', type);
    return this.http.get<CollectionRoute>(this.routeUrl, { params, context: inlineErrors() });
  }
}

function inlineErrors(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
