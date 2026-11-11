import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';

/** A row from GET admin/users-with-roles. */
export interface ManagedUser {
  username: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getUsersWithRoles(): Observable<ManagedUser[]> {
    return this.http.get<ManagedUser[]>(`${this.baseUrl}admin/users-with-roles`);
  }

  updateUserRoles(username: string, roles: string[]): Observable<object> {
    const params = new HttpParams().set('roles', roles.join(','));
    return this.http.post(`${this.baseUrl}admin/edit-roles/${encodeURIComponent(username)}`, {}, { params });
  }
}
