using API.DTOs;
using API.Errors;
using API.Extensions;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    /// <summary>Pay a bill without an account, using the invoice number and the payment code printed on it.</summary>
    [ApiController]
    [Route("api/public/invoices")]
    [EnableRateLimiting(RateLimitPolicies.PublicInvoice)]
    public class PublicInvoicesController : BaseApiController
    {
        public const string NotFoundMessage = "Invoice not found or payment code is incorrect";

        private readonly IInvoiceService _invoices;

        public PublicInvoicesController(IInvoiceService invoices)
        {
            _invoices = invoices;
        }

        [HttpPost("lookup")]
        public async Task<IActionResult> Lookup([FromBody] PublicInvoiceRequest request)
        {
            var invoice = await _invoices.LookupAsync(request.InvoiceNumber, request.PaymentCode);
            return invoice == null
                ? NotFound(new ApiException(404, NotFoundMessage, null))
                : Ok(invoice);
        }

        [HttpPost("pay")]
        public async Task<IActionResult> Pay([FromBody] PublicInvoiceRequest request)
        {
            var result = await _invoices.PayWithPaymentCodeAsync(request.InvoiceNumber, request.PaymentCode);
            return result.Outcome switch
            {
                PayOutcome.NotFound => NotFound(new ApiException(404, NotFoundMessage, null)),
                PayOutcome.AlreadyPaid => Conflict(new ApiException(409, "Invoice has already been paid", null)),
                _ => Ok(result.Receipt),
            };
        }
    }
}
