import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { getErrorMessages } from '../_helpers/api-error';
import { MyInvoice } from '../_models/invoice.model';
import { InvoiceService } from '../_services/invoice.service';

/** The signed-in member's invoices, with Pay for the unpaid ones. */
@Component({
    selector: 'app-my-invoices',
    templateUrl: './my-invoices.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class MyInvoicesComponent implements OnInit {
  invoices: MyInvoice[] = [];
  loading = true;
  loadError = '';
  /** Invoice number of the row being paid. */
  payingNumber: string | null = null;
  payError = '';

  constructor(private invoiceService: InvoiceService, private toastr: ToastrService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.invoiceService.getMine().subscribe({
      next: invoices => {
        this.invoices = invoices;
        this.loading = false;
      },
      error: error => {
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  pay(invoice: MyInvoice): void {
    if (this.payingNumber) return;

    this.payingNumber = invoice.invoiceNumber;
    this.payError = '';
    this.invoiceService.payMine(invoice.invoiceNumber).subscribe({
      next: result => {
        this.invoices = this.invoices.map(item => item.invoiceNumber === invoice.invoiceNumber
          ? { ...item, isPaid: true, isOverdue: false, paidAt: result.paidAt, receiptReference: result.receiptReference }
          : item);
        this.payingNumber = null;
        this.toastr.success(`Receipt ${result.receiptReference}`, 'Invoice paid');
      },
      error: error => {
        this.payError = getErrorMessages(error)[0];
        this.payingNumber = null;
      }
    });
  }

  /** Badge text and class for an invoice's status. */
  statusOf(invoice: MyInvoice): { label: string; className: string } {
    if (invoice.isPaid) return { label: 'Paid', className: 'bg-success' };
    if (invoice.isOverdue) return { label: 'Overdue', className: 'bg-danger' };
    return { label: 'Unpaid', className: 'bg-warning text-on-warning' };
  }
}
