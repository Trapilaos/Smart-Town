using API.DTOs;
using API.Errors;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/invoices")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminInvoicesController : BaseApiController
    {
        private static readonly string[] Statuses = { "paid", "unpaid", "overdue" };

        private readonly IInvoiceService _invoices;
        private readonly IAuditService _audit;

        public AdminInvoicesController(IInvoiceService invoices, IAuditService audit)
        {
            _invoices = invoices;
            _audit = audit;
        }

        /// <summary>All invoices, or those filtered by status: paid, unpaid or overdue. Payment codes are not listed.</summary>
        [HttpGet]
        public async Task<ActionResult<List<AdminInvoiceDTO>>> GetAll([FromQuery] string status)
        {
            if (!string.IsNullOrEmpty(status) && !Statuses.Contains(status))
                return BadRequest("Status must be paid, unpaid or overdue");

            return Ok(await _invoices.GetForAdminAsync(string.IsNullOrEmpty(status) ? null : status));
        }

        [HttpGet("summary")]
        public async Task<ActionResult<InvoiceSummaryDTO>> GetSummary()
        {
            return Ok(await _invoices.GetSummaryAsync());
        }

        /// <summary>Creates an invoice. The payment code is returned here and not shown again.</summary>
        [HttpPost]
        public async Task<ActionResult<AdminInvoiceDTO>> Create([FromBody] InvoiceInputDTO input)
        {
            try
            {
                var created = await _invoices.CreateAsync(input);
                await _audit.RecordAsync(ActorId, ActorName, "invoice.create", "Invoice", created.InvoiceNumber,
                    FormattableString.Invariant($"EUR {created.Amount:0.00} for {created.PayerName}: {created.Description}"));
                return Ok(created);
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
