using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.IO;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Services
{
    public class ImageService
    {
        public async Task<Stream> ResizeImageAsync(Stream inputStream, int maxWidth = 2560, int maxHeight = 1440)
        {
            var image = await Image.LoadAsync(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight),
                Sampler = KnownResamplers.Lanczos3 // high‑quality resampling
            }));

            var outputStream = new MemoryStream();
            var encoder = new JpegEncoder
            {
                Quality = 95,
                // Use RGB encoding to preserve full color fidelity
                ColorType = JpegEncodingColor.Rgb
            };

            await image.SaveAsJpegAsync(outputStream, encoder);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
