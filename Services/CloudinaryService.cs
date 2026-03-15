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
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Cloudinary:CloudName no configurado.");
        var apiKey    = configuration["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Cloudinary:ApiKey no configurado.");
        var apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret no configurado.");

        var account   = new Account(cloudName, apiKey, apiSecret);
        _cloudinary   = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder = "brana/productos")
    {
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
        var deleteParams = new DeletionParams(publicId);
        return await _cloudinary.DestroyAsync(deleteParams);
    }
}
