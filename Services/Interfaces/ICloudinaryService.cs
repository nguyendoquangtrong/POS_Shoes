using CloudinaryDotNet.Actions;

namespace POS_Shoes.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "products");
        Task<DeletionResult> DeleteImageAsync(string publicId);
        string GetOptimizedUrl(string publicId, int width = 0, int height = 0);
    }
}