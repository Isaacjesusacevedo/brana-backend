using System.Text.Json.Serialization;

namespace EsotericStore.API.Models.DTOs;

// ─── ApiResponse genérico ─────────────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error) => new()
    { Success = false, Errors = new List<string> { error } };
}

// ─── Paginación ───────────────────────────────────────────────────────────────
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

// ─── ProductDto ──────────────────────────────────────────────────────────────
/// <summary>
/// Respuesta de la API que mapea 1:1 con la interfaz Product de product.ts:
/// 
///   id, nombre?, titulo?, imagen, imagenes?, precio?, precioAnterior?,
///   categoria?, nuevo?, colores?, descripcion?, caracteristicas?,
///   stock?, tallas?, ruta?, badge?, size?
/// </summary>
public class ProductDto
{
    // id: number | string
    public string Id { get; set; } = string.Empty;

    // nombre?: string
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Nombre { get; set; }

    // titulo?: string (para compatibilidad con CarouselItem)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Titulo { get; set; }

    // imagen: string — imagen principal (primera del array)
    public string Imagen { get; set; } = string.Empty;

    // imagenes?: string[] — todas las imágenes
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Imagenes { get; set; }

    // precio?: number
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Precio { get; set; }

    // precioAnterior?: number
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? PrecioAnterior { get; set; }

    // categoria?: string — nombre de la categoría (no el ID)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Categoria { get; set; }

    // nuevo?: boolean
    public bool Nuevo { get; set; }

    // colores?: string[] — array de hex strings ["#000000", "#DAA520"]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Colores { get; set; }

    // descripcion?: string
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Descripcion { get; set; }

    // caracteristicas?: string[]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Caracteristicas { get; set; }

    // stock?: number
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Stock { get; set; }

    // tallas?: string[]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tallas { get; set; }

    // ruta?: string
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ruta { get; set; }

    // badge?: string
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Badge { get; set; }

    // size?: 'normal' | 'featured' | 'wide' | 'tall'
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Size { get; set; }
}

// ─── CategoryDto ─────────────────────────────────────────────────────────────
/// <summary>
/// Mapea con: Category { id, nombre, descripcion, icono, ruta?, productos }
/// </summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ruta { get; set; }

    public List<ProductDto> Productos { get; set; } = new();
}

// ─── OrderCreateDto ──────────────────────────────────────────────────────────
public class OrderCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string ProductoId { get; set; } = string.Empty;
    public string Talla { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public string? NotasAdicionales { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
    public string Talla { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? NotasAdicionales { get; set; }
    public ProductDto? Producto { get; set; }
}

// ─── Inputs para crear/editar ─────────────────────────────────────────────────
public class ProductCreateDto
{
    public string? Nombre { get; set; }
    public string? Titulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal? Precio { get; set; }
    public decimal? PrecioAnterior { get; set; }
    public int CategoriaId { get; set; }
    public string? Badge { get; set; }
    public string? Size { get; set; }
    public bool Nuevo { get; set; }
    public int? Stock { get; set; }
    public string? Ruta { get; set; }

    /// <summary>Lista de URLs de imágenes (subidas a Cloudinary previamente)</summary>
    public List<string> ImagenUrls { get; set; } = new();

    /// <summary>Array de hex strings: ["#000000", "#DAA520"]</summary>
    public List<string> Colores { get; set; } = new();

    public List<string>? Tallas { get; set; }
    public List<string>? Caracteristicas { get; set; }
}

public class ProductUpdateDto : ProductCreateDto
{
    public string Id { get; set; } = string.Empty;
}

// ─── Query params ─────────────────────────────────────────────────────────────
public class ProductQueryParams
{
    /// <summary>Filtrar por slug de categoría: "remeras", "buzos", "pantalones"</summary>
    public string? Categoria { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrecio { get; set; }
    public decimal? MaxPrecio { get; set; }
    public bool? SoloNuevos { get; set; }
    public string OrderBy { get; set; } = "nombre";
    public bool Desc { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}