using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using YumQuick.Core.Interfaces;
using YumQuick.Core.Settings;

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
		}

		public async Task<string> UploadImageAsync(IFormFile file)
		{
			if (file == null || file.Length == 0) return null;

			using var stream = file.OpenReadStream();
			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(file.FileName, stream),
				Folder = "YumQuick"
			};

			var uploadResult = await _cloudinary.UploadAsync(uploadParams);
			return uploadResult.SecureUrl.ToString();
		}
	}
}