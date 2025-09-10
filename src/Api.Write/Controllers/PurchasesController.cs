using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectZenith.Contracts.Commands.Purchase;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/purchases")]
    public class PurchasesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PurchasesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchase(
        [FromBody] CreatePurchaseRequest request,
        CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid user ID in token." });
            }

            var command = new CreatePurchaseCommand(
                userId,
                request.AppId,
                request.Price,
                request.PaymentMethodId,
                request.PaymentProvider);

            var purchaseId = await _mediator.Send(command, cancellationToken);
            return Accepted(new { PurchaseId = purchaseId });
        }
    }
    public record CreatePurchaseRequest(Guid AppId, decimal Price, string PaymentMethodId, string PaymentProvider);
}
