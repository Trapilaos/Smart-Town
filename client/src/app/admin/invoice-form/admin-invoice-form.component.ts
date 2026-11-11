import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { getErrorMessages } from '../../_helpers/api-error';
import { CreatedInvoice, InvoiceRequest } from '../../_models/invoice.model';
import { Municipality } from '../../_models/municipality.model';
import { AdminService, ManagedUser } from '../../_services/admin.service';
import { InvoiceService } from '../../_services/invoice.service';
import { MunicipalityService } from '../../_services/municipality.service';

interface InvoiceForm {
  payerName: string;
  description: string;
  amount: number | null;
  dueDate: string;
  /** The citizen account the invoice belongs to, or null for a payer without an account. */
  accountId: number | null;
}

/**
 * Issue an invoice. The payment code is returned only once, so the page shows it
 * next to a print-friendly bill and warns the admin to keep it.
 */
@Component({
    selector: 'app-admin-invoice-form',
    templateUrl: './admin-invoice-form.component.html',
    styleUrls: ['./admin-invoice-form.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AdminInvoiceFormComponent implements OnInit {
  form: InvoiceForm = emptyForm();
  errors: string[] = [];
  saving = false;

  /** Set after a successful create. While it is set, the form is replaced by the one-time code and bill. */
  created: CreatedInvoice | null = null;
  municipality: Municipality | null = null;

  /** Citizen accounts an invoice can be linked to. Empty when the list cannot be loaded; the form still works. */
  accounts: ManagedUser[] = [];

  constructor(
    private invoiceService: InvoiceService,
    private municipalityService: MunicipalityService,
    private adminService: AdminService
  ) {}

  ngOnInit(): void {
    this.municipalityService.get().subscribe({
      next: municipality => this.municipality = municipality,
      error: () => {
        // The bill still prints without the municipality name.
      }
    });
    this.adminService.getUsersWithRoles().subscribe({
      next: users => this.accounts = users,
      error: () => {
        // Without the list the account select offers only "no account".
      }
    });
  }

  /** Pre-fills the payer name from the chosen account, but never overwrites a name the admin typed. */
  onAccountChange(accountId: number | null): void {
    const account = accountId === null ? undefined : this.accounts.find(user => user.id === accountId);
    if (account && !this.form.payerName.trim()) {
      this.form.payerName = account.username;
    }
  }

  submit(): void {
    if (this.saving) return;

    this.errors = this.validate();
    if (this.errors.length > 0) return;

    const request: InvoiceRequest = {
      payerName: this.form.payerName.trim(),
      description: this.form.description.trim(),
      amount: roundToCents(this.form.amount ?? 0),
      dueDate: endOfLocalDay(this.form.dueDate),
      userId: this.form.accountId
    };

    this.saving = true;
    this.invoiceService.createInvoice(request).subscribe({
      next: created => {
        this.created = created;
        this.saving = false;
      },
      error: error => {
        this.errors = getErrorMessages(error);
        this.saving = false;
      }
    });
  }

  /** Clears the result so the next invoice can be issued. */
  newInvoice(): void {
    this.created = null;
    this.form = emptyForm();
    this.errors = [];
  }

  print(): void {
    window.print();
  }

  private validate(): string[] {
    const errors: string[] = [];
    const form = this.form;
    const payer = form.payerName.trim();
    const description = form.description.trim();

    if (!payer) errors.push('Enter the name of the person or organisation to bill.');
    if (payer.length > 120) errors.push('The payer name must be 120 characters or fewer.');
    if (!description) errors.push('Enter a description, for example "Water bill, March".');
    if (description.length > 200) errors.push('The description must be 200 characters or fewer.');

    const amount = form.amount;
    if (amount === null || Number.isNaN(amount)) {
      errors.push('Enter the amount.');
    } else if (amount < 0.01 || amount > 100000) {
      errors.push('The amount must be between 0.01 and 100,000 euro.');
    } else if (Math.abs(amount * 100 - Math.round(amount * 100)) > 1e-6) {
      errors.push('The amount can have at most two decimal places.');
    }

    if (!form.dueDate) errors.push('Choose a due date.');
    return errors;
  }
}

function emptyForm(): InvoiceForm {
  return { payerName: '', description: '', amount: null, dueDate: '', accountId: null };
}

function roundToCents(value: number): number {
  return Math.round(value * 100) / 100;
}

/** The due date is a day, so it ends at 23:59:59 local time, sent as a UTC ISO string. */
function endOfLocalDay(date: string): string {
  return new Date(`${date}T23:59:59`).toISOString();
}
