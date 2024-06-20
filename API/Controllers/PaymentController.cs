using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Errors;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : BaseApiController
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentDTO paymentDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            paymentDTO.UserId = userId;

            var invoice = await _paymentService.ProcessPayment(paymentDTO);
            if (invoice == null)
                return BadRequest(new ApiException(400, "Invalid invoice details or invoice already paid", null));

            return Ok(invoice);
        }

        [HttpGet("check/{invoiceNumber}")]
        public async Task<IActionResult> CheckInvoice(string invoiceNumber)
        {
            if (string.IsNullOrEmpty(invoiceNumber))
            {
                return BadRequest(new ApiException(400, "Invoice number is required", null));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var invoice = await _paymentService.CheckInvoice(invoiceNumber, userId);
                return Ok(new
                {
                    amount = invoice.Amount,
                    currency = "EUR",
                    userId = invoice.UserId,
                    isPaid = invoice.IsPaid
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "Invoice not found")
                {
                    return NotFound(new ApiException(404, ex.Message, null));
                }
                else
                {
                    return Ok(new
                    {
                        amount = 0,
                        currency = "EUR",
                        userId = "",
                        isPaid = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred");
                throw; // Let the middleware handle unexpected exceptions
            }
        }

    }
}
