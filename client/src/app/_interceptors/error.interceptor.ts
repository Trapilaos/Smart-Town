import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, take, throwError } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages, SKIP_ERROR_TOAST } from '../_helpers/api-error';
import { AccountService } from '../_services/account.service';

/**
 * Shows a toast for common API errors and rethrows every error so the calling
 * component can decide what else to show. The one exception is a 401 on a request
 * that carried an Authorization header: that means the session has ended, so the
 * user is logged out and sent to the home page. A 401 on a login request (no
 * header) is an ordinary wrong-password error and is only toasted.
 * A 404 never navigates: it may simply mean "nothing here yet".
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(
    private toastr: ToastrService,
    private accountService: AccountService,
    private router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const skipToast = request.context.get(SKIP_ERROR_TOAST);
    const wasAuthorized = request.headers.has('Authorization');

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error) {
          switch (error.status) {
            case 400:
              if (!skipToast) {
                getErrorMessages(error).forEach(message => this.toastr.error(message, 'Error'));
              }
              break;
            case 401:
              if (wasAuthorized) {
                this.endExpiredSession(skipToast);
                break;
              }
              if (!skipToast) {
                const message = typeof error.error === 'string' && error.error.trim()
                  ? error.error
                  : 'Please log in to continue';
                this.toastr.error(message, error.status.toString());
              }
              break;
            case 403:
              if (!skipToast) {
                this.toastr.error('You do not have permission to do that', error.status.toString());
              }
              break;
            case 404:
              // Not an error page: the component decides what an empty result looks like.
              break;
            case 500:
              if (!skipToast) {
                this.toastr.error('Something went wrong on our side. Please try again.');
              }
              break;
            default:
              if (!skipToast) {
                this.toastr.error('Something unexpected went wrong!');
              }
              break;
          }
        }
        return throwError(() => error);
      })
    );
  }

  /** Ends a session the server has rejected: logs out and sends the user home. */
  private endExpiredSession(skipToast: boolean) {
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      // Only the request that finds a signed-in user toasts, so parallel 401s don't stack toasts.
      this.accountService.logout();
      this.router.navigateByUrl('/');
      if (user && !skipToast) {
        this.toastr.error('Your session has expired. Please log in again.', 'Session expired');
      }
    });
  }
}
