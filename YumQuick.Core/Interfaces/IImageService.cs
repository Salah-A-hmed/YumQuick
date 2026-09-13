using Microsoft.AspNetCore.Http;

namespace YumQuick.Core.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}