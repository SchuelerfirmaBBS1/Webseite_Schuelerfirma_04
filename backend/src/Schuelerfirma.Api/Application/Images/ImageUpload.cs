namespace Schuelerfirma.Api.Application.Images;

/// <summary>One uploaded file as it came from the client, before any check.</summary>
/// <param name="FileName">The file name the client sent; only for display and logs, never used as a path.</param>
/// <param name="Data">The file content.</param>
public sealed record ImageUpload(string? FileName, byte[] Data);
