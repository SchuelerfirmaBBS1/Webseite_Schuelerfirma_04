using Microsoft.Extensions.DependencyInjection.Extensions;
using Schuelerfirma.Api.Application.Images;

namespace Schuelerfirma.Api.Infrastructure.Images;

/// <summary>Registers the image processing for uploads.</summary>
public static class ImageProcessingExtensions
{
    /// <summary>
    /// Registers <see cref="ImageUploadOptions"/> (section <c>Images</c>, checked at startup) and
    /// <see cref="IImageProcessor"/>.
    /// </summary>
    public static IServiceCollection AddImageProcessing(this IServiceCollection services)
    {
        services.AddOptions<ImageUploadOptions>()
            .BindConfiguration(ImageUploadOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IImageProcessor, SkiaImageProcessor>();
        return services;
    }
}
