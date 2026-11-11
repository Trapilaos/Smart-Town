import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';
import { HttpEvent, HttpHandler, HttpRequest, HttpInterceptor } from '@angular/common/http';
import { AccountService } from '../_services/account.service';
import { isTokenExpired } from '../_helpers/jwt';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private accountService: AccountService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return this.accountService.currentUser$.pipe(
      take(1),
      switchMap(user => {
        if (user && isTokenExpired(user.token)) {
          // Don't send a token the server will reject; end the session locally instead.
          this.accountService.logout();
          return next.handle(request);
        }
        if (user) {
          request = request.clone({
            setHeaders: {
              Authorization: `Bearer ${user.token}`
            }
          });
        }
        return next.handle(request);
      })
    );
  }
}
