import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { NavigationExtras, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages, SKIP_ERROR_TOAST } from '../_helpers/api-error';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private router: Router, private toastr: ToastrService) {}

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
              this.router.navigateByUrl('/not-found');
              break;
            case 500:
              const navigationExtras: NavigationExtras = { state: { error: error.error } };
              this.router.navigateByUrl('/server-error', navigationExtras);
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
