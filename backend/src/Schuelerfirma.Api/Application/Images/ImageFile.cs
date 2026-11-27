namespace Schuelerfirma.Api.Application.Images;

/// <summary>A stored image as it is delivered.</summary>
/// <param name="ContentType">Media type, one of the accepted image types.</param>
/// <param name="Data">The image file.</param>
public sealed record ImageFile(string ContentType, byte[] Data);
