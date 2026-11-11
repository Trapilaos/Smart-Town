/** Status filter for GET /api/admin/invoices. Empty means all invoices. */
export type InvoiceStatusFilter = 'paid' | 'unpaid' | 'overdue';

/** Response of POST /api/public/invoices/lookup. No user data. */
export interface InvoiceLookup {
  invoiceNumber: string;
  description: string;
  amount: number;
  dueDate: string;
  isPaid: boolean;
  paidAt: string | null;
}

/** Response of the pay endpoints (public and member). */
export interface InvoicePaymentResult {
  receiptReference: string;
  paidAt: string;
}

/** One row from GET /api/invoices/mine. */
export interface MyInvoice {
  invoiceNumber: string;
  description: string;
  amount: number;
  issuedAt: string;
  dueDate: string;
  isPaid: boolean;
  paidAt: string | null;
  isOverdue: boolean;
  paymentCode: string;
  receiptReference: string | null;
}

/** One row from GET /api/admin/invoices. */
export interface AdminInvoice {
  id: number;
  invoiceNumber: string;
  payerName: string;
  username: string | null;
  description: string;
  amount: number;
  issuedAt: string;
  dueDate: string;
  isPaid: boolean;
  paidAt: string | null;
  isOverdue: boolean;
  receiptReference: string | null;
}

/** Response of POST /api/admin/invoices. `paymentCode` is shown only in this response. */
export interface CreatedInvoice extends AdminInvoice {
  paymentCode: string;
}

/** Body for POST /api/admin/invoices. `dueDate` is a UTC ISO string. `userId` is the citizen account, if any. */
export interface InvoiceRequest {
  payerName: string;
  description: string;
  amount: number;
  dueDate: string;
  userId?: number | null;
}

/** Response of GET /api/admin/invoices/summary. */
export interface InvoiceSummary {
  unpaidCount: number;
  unpaidTotal: number;
  overdueCount: number;
  paidThisMonthTotal: number;
}
