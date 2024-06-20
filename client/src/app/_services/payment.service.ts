import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment.development';
import { Observable, of, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Payment } from '../_models/payment.model';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private baseUrl = `${environment.apiUrl}payment/`;

  constructor(private http: HttpClient) {}

  checkInvoice(invoiceNumber: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}check/${invoiceNumber}`).pipe(
      catchError(this.handleError)
    );
  }

  processPayment(payment: Payment): Observable<Payment> {
    return this.http.post<Payment>(this.baseUrl, payment).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse) {
    console.error('Backend returned code', error.status, 'body was:', error.error);
    return throwError(() => error.error); // Rethrow the error as an observable
  }
  
  
}
