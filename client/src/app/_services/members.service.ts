import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment.development';
import { Member } from '../_models/member';
import { BehaviorSubject, tap } from 'rxjs';

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
    return this.http.get<Member>(this.baseUrl + 'users/' + username);
  }

  setCurrentUser(user: Member) {
    localStorage.setItem('userId', user.id.toString());
    console.log('setCurrentUser called');
    this.currentUserSource.next(user);
  }

  getMemberByUsername(username: string) {
    const token = localStorage.getItem('token');
    const httpOptions = {
      headers: new HttpHeaders({
        Authorization: `Bearer ${token}`
      })
    };

    return this.http.get<Member>(this.baseUrl + 'users/' + username, httpOptions).pipe(
      tap(user => {
        this.setCurrentUser(user);
        return user;
      })
    );
  }
}
