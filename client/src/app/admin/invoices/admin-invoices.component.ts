import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { getErrorMessages } from '../../_helpers/api-error';
import { AdminInvoice, InvoiceStatusFilter, InvoiceSummary } from '../../_models/invoice.model';
import { InvoiceService } from '../../_services/invoice.service';

interface InvoiceTab {
  label: string;
  value: InvoiceStatusFilter | '';
}

/** Admin invoice list with status tabs, plus a short summary. */
@Component({
    selector: 'app-admin-invoices',
    templateUrl: './admin-invoices.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminInvoicesComponent implements OnInit {
  readonly tabs: InvoiceTab[] = [
    { label: 'All', value: '' },
    { label: 'Unpaid', value: 'unpaid' },
    { label: 'Overdue', value: 'overdue' },
    { label: 'Paid', value: 'paid' }
  ];

  activeTab: InvoiceTab = this.tabs[0];
  invoices: AdminInvoice[] = [];
  loading = true;
  loadError = '';
  /** Ignores responses for a tab the user has already left. */
  private requestId = 0;

  summary: InvoiceSummary | null = null;
  summaryError = '';

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit(): void {
    this.loadSummary();
    this.loadInvoices();
  }

  selectTab(tab: InvoiceTab): void {
    if (tab === this.activeTab) return;
    this.activeTab = tab;
    this.loadInvoices();
  }

  loadInvoices(): void {
    const id = ++this.requestId;
    this.loading = true;
    this.loadError = '';
    this.invoiceService.getAdminInvoices(this.activeTab.value).subscribe({
      next: invoices => {
        if (id !== this.requestId) return;
        this.invoices = invoices;
        this.loading = false;
      },
      error: error => {
        if (id !== this.requestId) return;
        this.invoices = [];
        this.loadError = getErrorMessages(error)[0];
        this.loading = false;
      }
    });
  }

  loadSummary(): void {
    this.summaryError = '';
    this.invoiceService.getSummary().subscribe({
      next: summary => this.summary = summary,
      error: error => this.summaryError = getErrorMessages(error)[0]
    });
  }

  /** Badge text and class for an invoice's status. */
  statusOf(invoice: AdminInvoice): { label: string; className: string } {
    if (invoice.isPaid) return { label: 'Paid', className: 'bg-success' };
    if (invoice.isOverdue) return { label: 'Overdue', className: 'bg-danger' };
    return { label: 'Unpaid', className: 'bg-warning text-on-warning' };
  }
}
