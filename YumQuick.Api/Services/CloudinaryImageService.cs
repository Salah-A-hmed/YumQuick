using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using YumQuick.Core.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace YumQuick.Api.Services
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryImageService(IConfiguration config)
        {
            var account = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Timeout = (int)TimeSpan.FromMinutes(3).TotalMicroseconds;
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var image = await Image.LoadAsync(file.OpenReadStream());

            if (image.Width > 800)
            {
                image.Mutate(x => x.Resize(800, 0));
            }

            using var compressedStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 60 };
            await image.SaveAsync(compressedStream, encoder);

            compressedStream.Position = 0;

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(Path.ChangeExtension(file.FileName, ".jpg"), compressedStream),
                Folder = "YumQuick"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }
    }
}