using Microsoft.AspNetCore.Mvc;

namespace Schuelerfirma.Api.Tests.Integration;

/// <summary>
/// Controller that always fails. It is only discovered by hosts that add this assembly
/// as an application part, so it does not leak into other tests.
/// </summary>
[ApiController]
[Route("test/throw")]
public class ThrowingController : ControllerBase
{
    public const string SecretDetail = "secret-internal-detail";

    [HttpGet]
    public IActionResult Get() => throw new InvalidOperationException(SecretDetail);
}
