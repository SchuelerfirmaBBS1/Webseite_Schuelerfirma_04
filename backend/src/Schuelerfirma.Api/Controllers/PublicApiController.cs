using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Base class for endpoints that visitors of the website use without logging in (<c>/api/public/...</c>):
/// reading events, images and products, sending the contact form.
/// </summary>
/// <remarks>
/// Rules (checked by a test over all endpoints): everything under <c>/api/public</c> is anonymous, and every
/// endpoint that is not a GET needs a rate-limiting policy (<c>[EnableRateLimiting]</c>). Responses use their own
/// response classes, never the ones of the admin area, so an internal field cannot become public by accident.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/public/[controller]")]
public abstract class PublicApiController : ControllerBase;
