import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RouterModule, provideRouter } from '@angular/router';
import { PayComponent, TOO_MANY_ATTEMPTS } from './pay.component';

describe('PayComponent', () => {
  let fixture: ComponentFixture<PayComponent>;
  let component: PayComponent;
  let controller: HttpTestingController;

  const invoiceNumber = 'INV-2026-000123';
  const paymentCode = 'ABCD2345';

  const isLookup = (req: { method: string; url: string }) => req.method === 'POST' && req.url.endsWith('public/invoices/lookup');
  const isPay = (req: { method: string; url: string }) => req.method === 'POST' && req.url.endsWith('public/invoices/pay');

  const invoice = {
    invoiceNumber,
    description: 'Water bill, March',
    amount: 42.5,
    dueDate: '2026-04-30T00:00:00Z',
    isPaid: false,
    paidAt: null
  };

  const pageText = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [PayComponent],
      imports: [FormsModule, RouterModule],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    fixture = TestBed.createComponent(PayComponent);
    component = fixture.componentInstance;
    controller = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => controller.verify());

  function enterDetails(): void {
    component.invoiceNumber = invoiceNumber;
    component.paymentCode = paymentCode;
  }

  it('looks up an invoice, pays it and shows the receipt', () => {
    enterDetails();
    component.lookup();

    const lookup = controller.expectOne(isLookup);
    expect(lookup.request.body).toEqual({ invoiceNumber, paymentCode });
    lookup.flush(invoice);
    fixture.detectChanges();

    expect(component.invoice?.description).toBe('Water bill, March');
    expect(pageText()).toContain('Water bill, March');

    component.pay();
    controller.expectOne(isPay).flush({ receiptReference: 'RCP-7F3K9Q', paidAt: '2026-03-10T09:15:00Z' });
    fixture.detectChanges();

    expect(component.receipt?.receiptReference).toBe('RCP-7F3K9Q');
    expect(component.receipt?.amount).toBe(42.5);
    expect(pageText()).toContain('RCP-7F3K9Q');
    expect(pageText()).toContain('Payment received');
  });

  it('normalises the payment code the payer typed and sends it upper-cased without spaces', () => {
    component.invoiceNumber = ' inv-2026-000123 ';
    component.paymentCode = 'abcd 2345';
    component.lookup();

    const lookup = controller.expectOne(isLookup);
    expect(lookup.request.body).toEqual({ invoiceNumber, paymentCode });
    lookup.flush(invoice);
  });

  it('shows the same inline message for an unknown invoice or a wrong code (404)', () => {
    enterDetails();
    component.lookup();

    controller.expectOne(isLookup).flush(
      { statusCode: 404, message: 'Invoice not found or payment code is incorrect', details: null },
      { status: 404, statusText: 'Not Found' }
    );
    fixture.detectChanges();

    expect(component.lookupError).toBe('Invoice not found or payment code is incorrect');
    expect(component.invoice).toBeNull();
    expect(component.receipt).toBeNull();
    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Invoice not found or payment code is incorrect');
  });

  it('shows the friendly rate-limit message for a 429 with an empty body', () => {
    enterDetails();
    component.lookup();

    controller.expectOne(isLookup).flush('', { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(component.lookupError).toBe(TOO_MANY_ATTEMPTS);
    expect(pageText()).toContain('Too many attempts. Please wait a minute and try again.');
  });

  it('does not send a request when the invoice number or code is malformed', () => {
    component.invoiceNumber = 'not-an-invoice';
    component.paymentCode = 'abc';
    component.lookup();

    controller.expectNone(isLookup);
    expect(component.lookupError).toContain('invoice number');
  });
});
