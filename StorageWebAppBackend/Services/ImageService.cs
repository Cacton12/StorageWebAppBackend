using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Services
{
    public class ImageService
    {
        // Resize image to max width/height while maintaining aspect ratio
        public async Task<Stream> ResizeImageAsync(Stream inputStream, int maxWidth = 2560, int maxHeight = 1440)
        {
            var image = await Image.LoadAsync(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            var outputStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 80 };
            await image.SaveAsJpegAsync(outputStream, encoder);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
