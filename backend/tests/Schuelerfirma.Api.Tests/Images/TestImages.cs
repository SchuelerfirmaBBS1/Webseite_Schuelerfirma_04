using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace Schuelerfirma.Api.Tests.Images;

/// <summary>Builds image files for tests, so no binary files have to live in the repository.</summary>
internal static class TestImages
{
    /// <summary>Text that is written into the metadata of test files; it must never come out again.</summary>
    public const string SecretMarker = "SECRET-METADATA-4711";

    /// <summary>
    /// A picture whose left half is red and right half blue, so the orientation can be read from the pixels.
    /// </summary>
    public static byte[] Create(SKEncodedImageFormat format, int width = 40, int height = 20, bool transparent = false)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(transparent ? SKColors.Transparent : SKColors.Red);
            if (!transparent)
            {
                using var blue = new SKPaint { Color = SKColors.Blue };
                canvas.DrawRect(width / 2f, 0, width / 2f, height, blue);
            }
        }

        using var data = bitmap.Encode(format, 90);
        return data.ToArray();
    }

    public static byte[] Jpeg(int width = 40, int height = 20) => Create(SKEncodedImageFormat.Jpeg, width, height);

    public static byte[] Png(int width = 40, int height = 20, bool transparent = false) =>
        Create(SKEncodedImageFormat.Png, width, height, transparent);

    public static byte[] Webp(int width = 40, int height = 20) => Create(SKEncodedImageFormat.Webp, width, height);

    /// <summary>A 1×1 GIF.</summary>
    public static byte[] Gif() => Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    /// <summary>An SVG with a script, i.e. what an attacker would upload as <c>bild.jpg</c>.</summary>
    public static byte[] Svg() => Encoding.UTF8.GetBytes(
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><script>alert(document.cookie)</script></svg>");

    /// <summary>An HTML page.</summary>
    public static byte[] Html() => Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><script>alert(1)</script></body></html>");

    /// <summary>
    /// Inserts an EXIF block right after the start marker of a JPEG: the orientation, a GPS position and an image
    /// description with <see cref="SecretMarker"/>.
    /// </summary>
    public static byte[] WithExif(byte[] jpeg, ushort orientation = 1)
    {
        var description = Encoding.ASCII.GetBytes(SecretMarker + "\0");
        var tiff = new List<byte>();
        void U16(int value) { var b = new byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)value); tiff.AddRange(b); }
        void U32(uint value) { var b = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, value); tiff.AddRange(b); }
        void Entry(int tag, int type, uint count, uint value) { U16(tag); U16(type); U32(count); U32(value); }

        const uint ifd0 = 8;
        const uint ifd0Size = 2 + (3 * 12) + 4;
        const uint gpsIfd = ifd0 + ifd0Size;
        const uint gpsIfdSize = 2 + (2 * 12) + 4;
        const uint gpsData = gpsIfd + gpsIfdSize;
        const uint descriptionOffset = gpsData + 24;

        // TIFF header, little endian.
        tiff.AddRange("II"u8.ToArray());
        U16(42);
        U32(ifd0);

        // IFD0: ImageDescription, Orientation, pointer to the GPS IFD.
        U16(3);
        Entry(0x010E, 2, (uint)description.Length, descriptionOffset);
        Entry(0x0112, 3, 1, orientation);
        Entry(0x8825, 4, 1, gpsIfd);
        U32(0);

        // GPS IFD: latitude reference "N" and latitude 53° 15' 0" (Lüneburg).
        U16(2);
        Entry(0x0001, 2, 2, 'N');
        Entry(0x0002, 5, 3, gpsData);
        U32(0);
        foreach (var (numerator, denominator) in new[] { (53u, 1u), (15u, 1u), (0u, 1u) })
        {
            U32(numerator);
            U32(denominator);
        }

        tiff.AddRange(description);

        var payload = "Exif\0\0"u8.ToArray().Concat(tiff).ToArray();
        var segment = new byte[4 + payload.Length];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(payload.Length + 2));
        payload.CopyTo(segment, 4);

        return [.. jpeg.Take(2), .. segment, .. jpeg.Skip(2)];
    }

    /// <summary>Inserts a <c>tEXt</c> chunk with <see cref="SecretMarker"/> right after the header of a PNG.</summary>
    public static byte[] WithPngText(byte[] png)
    {
        var body = Encoding.ASCII.GetBytes("Comment\0" + SecretMarker);
        var typeAndBody = "tEXt"u8.ToArray().Concat(body).ToArray();
        var chunk = new byte[12 + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)body.Length);
        typeAndBody.CopyTo(chunk, 4);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + body.Length), Crc32(typeAndBody));

        // Signature (8 bytes) + IHDR chunk (25 bytes).
        const int afterHeader = 8 + 25;
        return [.. png.Take(afterHeader), .. chunk, .. png.Skip(afterHeader)];
    }

    /// <summary>Whether <paramref name="haystack"/> contains <paramref name="text"/> as ASCII.</summary>
    public static bool Contains(byte[] haystack, string text) =>
        haystack.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }
}
