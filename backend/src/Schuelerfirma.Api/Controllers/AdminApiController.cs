using Microsoft.AspNetCore.Mvc;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Base class for the admin area (<c>/api/admin/...</c>), used by every role that manages content. What a user may
/// do is decided by the permissions of their role, not by this path.
/// </summary>
/// <remarks>
/// Rules (checked by a test over all endpoints): every endpoint under <c>/api/admin</c> has
/// <c>[HasPermission(...)]</c> and none has <c>[AllowAnonymous]</c>. Responses are never cached.
/// </remarks>
[ApiController]
[Route("api/admin/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class AdminApiController : ControllerBase;
