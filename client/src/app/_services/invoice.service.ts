import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { SKIP_ERROR_TOAST } from '../_helpers/api-error';
import {
  AdminInvoice,
  CreatedInvoice,
  InvoiceLookup,
  InvoicePaymentResult,
  InvoiceRequest,
  InvoiceStatusFilter,
  InvoiceSummary,
  MyInvoice
} from '../_models/invoice.model';

/**
 * Public bill payment, member invoices and admin invoicing. Every call sets
 * SKIP_ERROR_TOAST: the pages show 404, 409 and 429 messages inline.
 */
@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private readonly publicUrl = `${environment.apiUrl}public/invoices`;
  private readonly mineUrl = `${environment.apiUrl}invoices/mine`;
  private readonly invoicesUrl = `${environment.apiUrl}invoices`;
  private readonly adminUrl = `${environment.apiUrl}admin/invoices`;

  constructor(private http: HttpClient) {}

  /** Checks an invoice number and payment code. The API returns the same 404 for an unknown number or a wrong code. */
  lookup(invoiceNumber: string, paymentCode: string): Observable<InvoiceLookup> {
    return this.http.post<InvoiceLookup>(`${this.publicUrl}/lookup`, { invoiceNumber, paymentCode }, { context: inlineErrors() });
  }

  payPublic(invoiceNumber: string, paymentCode: string): Observable<InvoicePaymentResult> {
    return this.http.post<InvoicePaymentResult>(`${this.publicUrl}/pay`, { invoiceNumber, paymentCode }, { context: inlineErrors() });
  }

  getMine(): Observable<MyInvoice[]> {
    return this.http.get<MyInvoice[]>(this.mineUrl, { context: inlineErrors() });
  }

  payMine(invoiceNumber: string): Observable<InvoicePaymentResult> {
    return this.http.post<InvoicePaymentResult>(`${this.invoicesUrl}/${encodeURIComponent(invoiceNumber)}/pay`, {}, { context: inlineErrors() });
  }

  getAdminInvoices(status: InvoiceStatusFilter | ''): Observable<AdminInvoice[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<AdminInvoice[]>(this.adminUrl, { params, context: inlineErrors() });
  }

  createInvoice(request: InvoiceRequest): Observable<CreatedInvoice> {
    return this.http.post<CreatedInvoice>(this.adminUrl, request, { context: inlineErrors() });
  }

  getSummary(): Observable<InvoiceSummary> {
    return this.http.get<InvoiceSummary>(`${this.adminUrl}/summary`, { context: inlineErrors() });
  }
}

function inlineErrors(): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, true);
}
