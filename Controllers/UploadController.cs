using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinary;

    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadController(ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    /// <summary>
    /// Sube una imagen a Cloudinary y retorna la URL pública optimizada.
    /// </summary>
    [HttpPost("imagen")]
    [ProducesResponseType(typeof(ApiResponse<UploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubirImagen(IFormFile archivo, [FromQuery] string carpeta = "brana/productos")
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No se recibió ningún archivo."));

        if (archivo.Length > MaxFileSizeBytes)
            return BadRequest(ApiResponse<object>.Fail("El archivo supera el límite de 10 MB."));

        if (!AllowedContentTypes.Contains(archivo.ContentType.ToLower()))
            return BadRequest(ApiResponse<object>.Fail("Tipo de archivo no permitido. Use JPEG, PNG, WebP o GIF."));

        var result = await _cloudinary.UploadImageAsync(archivo, carpeta);

        if (result.Error is not null)
            throw new Exception($"Error de Cloudinary: {result.Error.Message}");

        var dto = new UploadResultDto(
            result.SecureUrl.ToString(),
            result.PublicId,
            result.Width,
            result.Height,
            result.Bytes
        );

        return Ok(ApiResponse<UploadResultDto>.Ok(dto, "Imagen subida exitosamente."));
    }

    /// <summary>
    /// Elimina una imagen de Cloudinary por su publicId.
    /// </summary>
    [HttpDelete("imagen")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EliminarImagen([FromQuery] string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return BadRequest(ApiResponse<object>.Fail("El publicId es requerido."));

        var result = await _cloudinary.DeleteImageAsync(publicId);

        if (result.Error is not null)
            throw new Exception($"Error al eliminar imagen: {result.Error.Message}");

        return Ok(ApiResponse<string>.Ok(publicId, $"Imagen '{publicId}' eliminada."));
    }
}
