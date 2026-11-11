using API.DTOs;
using API.Errors;
using API.Extensions;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>The signed-in member's own invoices.</summary>
    [ApiController]
    [Route("api/invoices")]
    [Authorize(Policy = "RequireMemberRole")]
    public class InvoicesController : BaseApiController
    {
        private readonly IInvoiceService _invoices;

        public InvoicesController(IInvoiceService invoices)
        {
            _invoices = invoices;
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<MemberInvoiceDTO>>> GetMine()
        {
            return Ok(await _invoices.GetForUserAsync(ActorId));
        }

        [HttpPost("{invoiceNumber}/pay")]
        public async Task<IActionResult> PayMine(string invoiceNumber)
        {
            var result = await _invoices.PayAsOwnerAsync(ActorId, invoiceNumber);
            return result.Outcome switch
            {
                PayOutcome.NotFound => NotFound(new ApiException(404, "Invoice not found", null)),
                PayOutcome.AlreadyPaid => Conflict(new ApiException(409, "Invoice has already been paid", null)),
                _ => Ok(result.Receipt),
            };
        }
    }
}
