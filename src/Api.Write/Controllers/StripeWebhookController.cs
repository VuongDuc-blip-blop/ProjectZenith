using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Configuration;
using Stripe;

namespace ProjectZenith.Api.Write.Controllers
{
    [ApiController]
    [Route("api/stripe-webhooks")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly string _stripeWebhookSecret;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IMediator mediator,
            IOptions<StripeOptions> stripeOptions,
            ILogger<StripeWebhookController> logger)
        {
            _mediator = mediator;
            _logger = logger;
            _stripeWebhookSecret = stripeOptions.Value.WebhookSecret;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeWebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case "account.updated":

                        var account = stripeEvent.Data.Object as Account;
                        if (account == null)
                        {
                            _logger.LogWarning("Received account.updated event with null account object.");
                            return BadRequest();
                        }

                        var command = new ProcessStripeAccountUpdateCommand
                        (
                            account.Id,
                            account.PayoutsEnabled,
                            account.ChargesEnabled,
                            account.DetailsSubmitted
                        );

                        await _mediator.Send(command);
                        break;

                    default:
                        _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                        return Ok();
                }
                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe exception while processing webhook: {Message}", ex.Message);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing webhook: {Message}", ex.Message);
                return StatusCode(500);
            }
        }
    }
}
