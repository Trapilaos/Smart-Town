import { Component, ChangeDetectionStrategy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { PaymentService, InvoiceStatus } from '../_services/payment.service';
import { Payment } from '../_models/payment.model';
import { ToastrService } from 'ngx-toastr';

interface CheckedInvoice extends InvoiceStatus {
  invoiceNumber: string;
}

@Component({
    selector: 'app-pay',
    templateUrl: './pay.component.html',
    styleUrls: ['./pay.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class PayComponent {
  invoiceNumber = '';
  invoice: CheckedInvoice | null = null;
  lookupError = '';
  checking = false;
  paying = false;
  private lookupSubmitted = false;

  constructor(private paymentService: PaymentService, private toastr: ToastrService) { }

  get invoiceError(): string {
    return this.lookupSubmitted && !this.invoiceNumber.trim() ? 'Enter an invoice number to continue.' : '';
  }

  checkInvoice(): void {
    this.lookupSubmitted = true;
    const number = this.invoiceNumber.trim();
    if (!number || this.checking) return;

    this.checking = true;
    this.lookupError = '';
    this.invoice = null;
    this.paymentService.checkInvoice(number).subscribe({
      next: (response) => {
        this.invoice = { ...response, invoiceNumber: number };
        this.checking = false;
      },
      error: (error: HttpErrorResponse) => {
        // The interceptor toasts 400/401/403 messages, so the lookup error stays inline.
        this.lookupError = error.status === 404
          ? 'We could not find an invoice with that number.'
          : 'We could not check this invoice. Please try again.';
        this.checking = false;
      }
    });
  }

  payInvoice(): void {
    const invoice = this.invoice;
    if (!invoice || invoice.isPaid || this.paying) return;

    const payment: Payment = {
      userId: '', // ignored by the server, which uses the authenticated user
      invoiceNumber: invoice.invoiceNumber,
      amount: invoice.amount,
      currency: invoice.currency,
      description: '',
      isPaid: false
    };

    this.paying = true;
    this.paymentService.processPayment(payment).subscribe({
      next: () => {
        this.paying = false;
        this.invoice = { ...invoice, isPaid: true };
        this.toastr.success('Payment processed successfully!');
      },
      error: () => {
        // The interceptor shows the API message for 400/401/403, so only reset the button here.
        this.paying = false;
      }
    });
  }
}
