using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Errors;

namespace API.Controllers
{
    [Authorize]
    public class PaymentController : BaseApiController
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Pays one of the caller's own invoices. The user id always comes from the token, never from the body.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentDTO paymentDTO)
        {
            paymentDTO.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invoice = await _paymentService.ProcessPayment(paymentDTO);
            if (invoice == null)
                return BadRequest(new ApiException(400, "Invalid invoice details or invoice already paid", null));

            return Ok(invoice);
        }

        [HttpGet("check/{invoiceNumber}")]
        public async Task<IActionResult> CheckInvoice(string invoiceNumber)
        {
            if (string.IsNullOrEmpty(invoiceNumber))
                return BadRequest(new ApiException(400, "Invoice number is required", null));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var invoice = await _paymentService.CheckInvoice(invoiceNumber, userId);
            if (invoice == null)
                return NotFound(new ApiException(404, "Invoice not found", null));

            return Ok(new
            {
                amount = invoice.Amount,
                currency = "EUR",
                userId = invoice.UserId,
                isPaid = invoice.IsPaid
            });
        }
    }
}
