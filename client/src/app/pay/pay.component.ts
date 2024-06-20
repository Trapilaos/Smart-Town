import { Component } from '@angular/core';
import { PaymentService } from '../_services/payment.service';
import { Payment } from '../_models/payment.model';
import { ToastrService } from 'ngx-toastr';
import { EMPTY, catchError, map, tap } from 'rxjs';

@Component({
  selector: 'app-pay',
  templateUrl: './pay.component.html',
  styleUrls: ['./pay.component.css']
})
export class PayComponent {
  payment: Payment = this.initializePayment();
  paymentStatus: string = '';
  invoiceChecked: boolean = false;
  errorMessage: string = '';
  loading: boolean = false;

  constructor(private paymentService: PaymentService, private toastr: ToastrService) { }

  initializePayment(): Payment {
    return {
      userId: '',
      invoiceNumber: '',
      amount: 0,
      currency: 'EUR',
      description: '',
      isPaid: false
    };
  }

  checkInvoice() {
    console.log('checkInvoice called');
    if (!this.payment.invoiceNumber) {
      this.toastr.error('Please enter an invoice number');
      return;
    }

    this.loading = true;
    this.paymentService.checkInvoice(this.payment.invoiceNumber).pipe(
      map((response: any) => {
        if (response.isPaid) {
          throw new Error('Invoice already paid');
        }
        return response;
      }),
      tap((response) => {
        console.log('response:', response);
        this.invoiceChecked = true;
        this.paymentStatus = `Invoice amount: ${response.amount} ${response.currency}`;
        this.payment.amount = response.amount;
        this.payment.currency = response.currency;
        this.payment.userId = response.userId;
        this.errorMessage = '';
        this.loading = false;
      }),
      catchError((error) => {
        console.log('error:', error);
        this.handleError(error);
        return EMPTY;
      })
    ).subscribe();
  }

  processPayment() {
    if (!this.invoiceChecked) {
      this.toastr.error('Please check the invoice before paying.');
      return;
    }

    this.loading = true;
    this.paymentService.processPayment(this.payment).pipe(
      tap((response) => {
        this.toastr.success('Payment processed successfully!');
        this.invoiceChecked = false;
        this.payment = this.initializePayment();
        this.errorMessage = '';
        this.loading = false;
      }),
      catchError((error) => {
        this.handleError(error);
        return EMPTY;
      })
    ).subscribe();
  }

  handleError(error: any) {
    console.log('handleError called with:', error);
    this.loading = false;
    if (error.status === 404) {
      this.toastr.error('Invoice not found.');
      this.errorMessage = 'Invoice not found.';
    } else if (error.message === 'Invoice already paid') {
      this.toastr.error('This invoice is already paid.');
      this.errorMessage = 'This invoice is already paid.';
    } else {
      this.toastr.error(error.message || 'Error checking invoice');
      this.errorMessage = error.message || 'Error checking invoice';
    }

    this.invoiceChecked = false;
    this.paymentStatus = '';
  }

  closeAlert() {
    this.errorMessage = '';
  }
}
