import { Injectable } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { User } from '../_models/user';
import { HttpClient, HttpContext } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import { decodeJwtPayload, isTokenExpired } from '../_helpers/jwt';

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

  /** Signs the user back in from local storage, before any route guard runs. Expired sessions are dropped. */
  private restoreUser() {
    try {
      const userString = localStorage.getItem('user');
      if (!userString) return;
      const user = JSON.parse(userString) as User;
      if (!user.token || isTokenExpired(user.token)) {
        localStorage.removeItem('user');
        return;
      }
      this.setCurrentUser(user);
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
    let decodedToken;
    try {
      decodedToken = this.getDecodedToken(user.token);
    } catch {
      this.logout();
      throw new Error('The sign-in token could not be read. Please log in again.');
    }
    const role = decodedToken.role;
    user.roles = Array.isArray(role) ? role : [role].filter((r): r is string => typeof r === 'string');
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSource.next(user);
  }

  getDecodedToken(token: string) {
    return decodeJwtPayload(token);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUserSource.next(null);
  }
}
