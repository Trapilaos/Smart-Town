import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Payment } from '../_models/payment.model';

export interface InvoiceStatus {
  amount: number;
  currency: string;
  userId: string;
  isPaid: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private baseUrl = `${environment.apiUrl}payment/`;

  constructor(private http: HttpClient) {}

  checkInvoice(invoiceNumber: string): Observable<InvoiceStatus> {
    return this.http.get<InvoiceStatus>(`${this.baseUrl}check/${encodeURIComponent(invoiceNumber)}`).pipe(
      catchError(this.handleError)
    );
  }

  processPayment(payment: Payment): Observable<Payment> {
    return this.http.post<Payment>(this.baseUrl, payment).pipe(
      catchError(this.handleError)
    );
  }

  // Rethrow the HttpErrorResponse so callers can tell a 404 (unknown invoice) apart from other failures.
  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
