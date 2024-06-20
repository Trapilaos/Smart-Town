export interface Payment {
  userId: string;
  invoiceNumber: string;
  amount: number;
  currency: string;
  description: string;
  isPaid: boolean;
}
