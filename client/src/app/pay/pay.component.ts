import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';
import { AccountService } from '../_services/account.service';
import { InvoiceService } from '../_services/invoice.service';
import { getErrorMessages } from '../_helpers/api-error';
import { InvoiceLookup, InvoicePaymentResult } from '../_models/invoice.model';

export const TOO_MANY_ATTEMPTS = 'Too many attempts. Please wait a minute and try again.';

/** Invoice numbers look like INV-2026-000123. */
const INVOICE_NUMBER_PATTERN = /^INV-\d{4}-\d{6}$/;
/** Payment codes are 8 characters from a set that leaves out 0, 1, I and O. */
const PAYMENT_CODE_PATTERN = /^[A-HJ-NP-Z2-9]{8}$/;

/** A receipt shown after a successful payment. */
export interface PaymentReceipt extends InvoicePaymentResult {
  invoiceNumber: string;
  description: string;
  amount: number;
}

/** Public bill payment: lookup, details, pay, then a printable receipt. No account needed. */
@Component({
    selector: 'app-pay',
    templateUrl: './pay.component.html',
    styleUrls: ['./pay.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class PayComponent implements OnInit, OnDestroy {
  invoiceNumber = '';
  paymentCode = '';

  /** The invoice found by the last successful lookup. */
  invoice: InvoiceLookup | null = null;
  /** The normalised values that were looked up. Paying uses these, not the form fields. */
  private checked: { invoiceNumber: string; paymentCode: string } | null = null;
  receipt: PaymentReceipt | null = null;

  lookupError = '';
  payError = '';
  looking = false;
  paying = false;
  loggedIn = false;

  private readonly destroy$ = new Subject<void>();

  constructor(private invoiceService: InvoiceService, private accountService: AccountService) {}

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(takeUntil(this.destroy$)).subscribe(user => this.loggedIn = !!user);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  lookup(): void {
    if (this.looking) return;

    const invoiceNumber = this.invoiceNumber.trim().toUpperCase();
    const paymentCode = normaliseCode(this.paymentCode);

    const problem = this.inputProblem(invoiceNumber, paymentCode);
    if (problem) {
      this.lookupError = problem;
      return;
    }

    this.looking = true;
    this.lookupError = '';
    this.payError = '';
    this.invoiceService.lookup(invoiceNumber, paymentCode).pipe(takeUntil(this.destroy$)).subscribe({
      next: invoice => {
        this.invoice = invoice;
        this.checked = { invoiceNumber, paymentCode };
        this.receipt = null;
        this.looking = false;
      },
      error: error => {
        this.invoice = null;
        this.checked = null;
        this.lookupError = this.messageFor(error);
        this.looking = false;
      }
    });
  }

  pay(): void {
    const invoice = this.invoice;
    const checked = this.checked;
    if (!invoice || !checked || this.paying || invoice.isPaid) return;

    this.paying = true;
    this.payError = '';
    this.invoiceService.payPublic(checked.invoiceNumber, checked.paymentCode).pipe(takeUntil(this.destroy$)).subscribe({
      next: result => {
        this.receipt = {
          ...result,
          invoiceNumber: checked.invoiceNumber,
          description: invoice.description,
          amount: invoice.amount
        };
        this.invoice = { ...invoice, isPaid: true, paidAt: result.paidAt };
        this.paying = false;
      },
      error: error => {
        this.payError = this.messageFor(error);
        this.paying = false;
      }
    });
  }

  /** Clears the form and the result so another invoice can be paid. */
  startAgain(): void {
    this.invoiceNumber = '';
    this.paymentCode = '';
    this.invoice = null;
    this.checked = null;
    this.receipt = null;
    this.lookupError = '';
    this.payError = '';
  }

  print(): void {
    window.print();
  }

  /** Field-level check before any request, so a typo never reaches the rate-limited endpoint. */
  private inputProblem(invoiceNumber: string, paymentCode: string): string {
    if (!INVOICE_NUMBER_PATTERN.test(invoiceNumber)) {
      return 'Enter the invoice number as printed on your bill, for example INV-2026-000123.';
    }
    if (!PAYMENT_CODE_PATTERN.test(paymentCode)) {
      return 'Enter the 8-character payment code printed on your bill.';
    }
    return '';
  }

  /** Inline message for a failed call. A 429 has an empty body, so it gets its own text. */
  private messageFor(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 429) {
      return TOO_MANY_ATTEMPTS;
    }
    return getErrorMessages(error)[0];
  }
}

/** Removes spaces and dashes a payer may type into the code, and upper-cases it. */
function normaliseCode(value: string): string {
  return value.replace(/[\s-]/g, '').toUpperCase();
}
