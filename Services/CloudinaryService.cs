using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EsotericStore.API.Services;

public interface ICloudinaryService
{
    Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "brana/productos");
    Task<DeletionResult> DeleteImageAsync(string publicId);
}

public class CloudinaryService : ICloudinaryService
{
    // Nullable: en desarrollo local puede no estar configurado.
    // Solo falla si se intenta hacer un upload/delete real sin credenciales.
    private readonly Cloudinary? _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        // Render usa variables planas (CLOUDINARY_CLOUD_NAME).
        // Dev local usa sección appsettings (Cloudinary__CloudName → Cloudinary:CloudName).
        var cloudName = configuration["CLOUDINARY_CLOUD_NAME"] ?? configuration["Cloudinary:CloudName"];
        var apiKey    = configuration["CLOUDINARY_API_KEY"]    ?? configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["CLOUDINARY_API_SECRET"] ?? configuration["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName) &&
            !string.IsNullOrEmpty(apiKey)    &&
            !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "brana/productos")
    {
        if (_cloudinary is null)
            throw new InvalidOperationException(
                "Cloudinary no está configurado. Verificá: CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET.");

        if (file == null || file.Length == 0)
            throw new ArgumentException("El archivo está vacío.");

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(file.FileName, stream),
            Folder         = folder,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
            UniqueFilename = true,
            Overwrite      = false
        };

        return await _cloudinary.UploadAsync(uploadParams);
    }

    public async Task<DeletionResult> DeleteImageAsync(string publicId)
    {
        if (_cloudinary is null)
            throw new InvalidOperationException(
                "Cloudinary no está configurado. Verificá: CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET.");

        var deleteParams = new DeletionParams(publicId);
        return await _cloudinary.DestroyAsync(deleteParams);
    }
}
