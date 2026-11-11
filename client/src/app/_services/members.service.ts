import { Injectable } from '@angular/core';
import { BehaviorSubject, of, switchMap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Member } from '../_models/member';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  private currentUserSource = new BehaviorSubject<Member | null>(null);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient) { }

  getMembers() {
    return this.http.get<Member[]>(this.baseUrl + 'users');
  }

  getMember(username: string) {
    return this.http.get<Member>(`${this.baseUrl}users/${encodeURIComponent(username)}`);
  }

  setCurrentUser(user: Member) {
    localStorage.setItem('userId', user.id.toString());
    this.currentUserSource.next(user);
  }

  getMemberByUsername(username: string) {
    return this.http.get<Member>(`${this.baseUrl}users/${encodeURIComponent(username)}`).pipe(
      switchMap(user => {
        this.setCurrentUser(user);
        return of(user);
      })
    );
  }
}
