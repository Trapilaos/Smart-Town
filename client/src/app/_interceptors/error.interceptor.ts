import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages, SKIP_ERROR_TOAST } from '../_helpers/api-error';

/**
 * Shows a toast for common API errors and rethrows every error so the calling
 * component can decide what else to show. Never navigates: a 404 may simply mean
 * "nothing here yet", and the component knows best how to present that.
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private toastr: ToastrService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const skipToast = request.context.get(SKIP_ERROR_TOAST);

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
              if (!skipToast) {
                const message = typeof error.error === 'string' && error.error.trim()
                  ? error.error
                  : 'Please log in to continue';
                this.toastr.error(message, error.status.toString());
              }
              break;
            case 403:
              this.toastr.error('You do not have permission to do that', error.status.toString());
              break;
            case 404:
              // Not an error page: the component decides what an empty result looks like.
              break;
            case 500:
              this.toastr.error('Something went wrong on our side. Please try again.');
              break;
            default:
              this.toastr.error('Something unexpected went wrong!');
              console.log(error);
              break;
          }
        }
        return throwError(() => error);
      })
    );
  }
}
