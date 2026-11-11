import { Injectable } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { User } from '../_models/user';
import { HttpClient, HttpContext } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;
  private currentUserSource = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient) {
    this.restoreUser();
  }

  /** Signs the user back in from local storage, before any route guard runs. */
  private restoreUser() {
    try {
      const userString = localStorage.getItem('user');
      if (!userString) return;
      this.setCurrentUser(JSON.parse(userString) as User);
    } catch {
      localStorage.removeItem('user');
    }
  }

  login(model: any) {
    return this.http.post<User>(`${this.baseUrl}account/login`, model).pipe(
      map((response: User) => {
        if (response) {
          this.setCurrentUser(response);
        }
        return response;
      })
    );
  }

  register(model: any) {
    // The register form shows its own errors inline, so suppress the interceptor toast.
    const context = new HttpContext().set(SKIP_ERROR_TOAST, true);
    return this.http.post<User>(`${this.baseUrl}account/register`, model, { context }).pipe(
      map((user: User) => {
        if (user) {
          this.setCurrentUser(user);
        }
        return user;
      })
    );
  }

  setCurrentUser(user: User) {
    const decodedToken = this.getDecodedToken(user.token);
    user.roles = Array.isArray(decodedToken.role) ? decodedToken.role : [decodedToken.role];
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSource.next(user);
  }

  getDecodedToken(token: string) {
    const parts = token.split('.');
    return JSON.parse(atob(parts[1]));
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUserSource.next(null);
  }
}
