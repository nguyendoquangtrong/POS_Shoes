// Services/CloudinaryService.cs
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using POS_Shoes.Services.Interfaces;

namespace POS_Shoes.Implementations.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is required", nameof(file));
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ArgumentException("Only image files are allowed");
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new ArgumentException("File size must be less than 10MB");
            }

            try
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"pos-shoes/{folder}",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                        .Width(1200)
                        .Height(1200)
                        .Crop("limit")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError($"Cloudinary upload error: {result.Error.Message}");
                    throw new Exception($"Upload failed: {result.Error.Message}");
                }

                _logger.LogInformation($"Image uploaded successfully: {result.PublicId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                throw;
            }
        }

        public async Task<DeletionResult> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                throw new ArgumentException("Public ID is required", nameof(publicId));
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);

                _logger.LogInformation($"Image deleted: {publicId}, Result: {result.Result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image from Cloudinary: {publicId}");
                throw;
            }
        }

        public string GetOptimizedUrl(string publicId, int width = 0, int height = 0)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return string.Empty;
            }

            try
            {
                var transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto");

                if (width > 0 && height > 0)
                {
                    transformation = transformation.Width(width).Height(height).Crop("fill");
                }
                else if (width > 0)
                {
                    transformation = transformation.Width(width).Crop("scale");
                }

                return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating optimized URL for: {publicId}");
                return string.Empty;
            }
        }
    }
}
