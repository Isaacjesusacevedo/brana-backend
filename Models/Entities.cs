// ✅ CORREGIDO: En C# solo puede haber UNA declaración de namespace file-scoped por archivo.
//    El archivo original repetía "namespace EsotericStore.API.Models.Entities;" múltiples
//    veces, lo cual es inválido y causaba errores de compilación en cascada.
//    Solución: una sola declaración al inicio y todas las clases dentro.

namespace EsotericStore.API.Models.Entities;

// ─── Product ──────────────────────────────────────────────────────────────────
public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>nombre?: string — campo principal</summary>
    public string? Nombre { get; set; }

    /// <summary>titulo?: string — alias para compatibilidad con CarouselItem</summary>
    public string? Titulo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>precio?: number — nullable en el TS (featured items sin precio)</summary>
    public decimal? Precio { get; set; }

    public decimal? PrecioAnterior { get; set; }

    /// <summary>badge?: string — "EXCLUSIVO", "NUEVO", etc.</summary>
    public string? Badge { get; set; }

    /// <summary>size?: 'normal' | 'featured' | 'wide' | 'tall'</summary>
    public string? Size { get; set; }

    public bool Nuevo { get; set; }

    /// <summary>stock?: number</summary>
    public int? Stock { get; set; }

    /// <summary>tallas?: string[] — guardado como JSON. Ej: ["XS","S","M","L","XL","XXL"]</summary>
    public string? TallasJson { get; set; }

    /// <summary>caracteristicas?: string[] — guardado como JSON.</summary>
    public string? CaracteristicasJson { get; set; }

    /// <summary>ruta?: string — "/categoria/remeras"</summary>
    public string? Ruta { get; set; }

    // FK
    public int CategoriaId { get; set; }
    public Category Categoria { get; set; } = null!;

    public ICollection<ProductImage> Imagenes { get; set; } = new List<ProductImage>();
    public ICollection<ProductColor> Colores   { get; set; } = new List<ProductColor>();
}

// ─── Category ─────────────────────────────────────────────────────────────────
public class Category
{
    public int Id { get; set; }

    /// <summary>Slug para URL y filtros: "remeras" | "buzos" | "pantalones"</summary>
    public string Slug { get; set; } = string.Empty;

    public string Nombre      { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono       { get; set; } = string.Empty;

    /// <summary>ruta?: string — "/categoria/remeras"</summary>
    public string? Ruta { get; set; }

    public ICollection<Product> Productos { get; set; } = new List<Product>();
}

// ─── ProductImage ─────────────────────────────────────────────────────────────
public class ProductImage
{
    public int    Id          { get; set; }
    public string Url         { get; set; } = string.Empty;

    /// <summary>La primera imagen mapea a 'imagen: string' (campo principal en TS)</summary>
    public bool EsPrincipal { get; set; }
    public int  Orden       { get; set; }

    public string  ProductoId { get; set; } = string.Empty;
    public Product Producto   { get; set; } = null!;
}

// ─── ProductColor ─────────────────────────────────────────────────────────────
public class ProductColor
{
    public int Id { get; set; }

    /// <summary>Hex string que va directo al array colores[] del TS: "#DAA520"</summary>
    public string Hex { get; set; } = string.Empty;

    /// <summary>Nombre legible solo para panel admin: "Dorado"</summary>
    public string? Nombre { get; set; }

    public string  ProductoId { get; set; } = string.Empty;
    public Product Producto   { get; set; } = null!;
}

// ─── Order ────────────────────────────────────────────────────────────────────
public class Order
{
    public Guid   Id       { get; set; } = Guid.NewGuid();
    public string Nombre   { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    public string? NotasAdicionales { get; set; }
    public DateTime FechaPedido     { get; set; } = DateTime.UtcNow;

    /// <summary>pendiente | confirmado | procesando | enviado | entregado | cancelado | devuelto</summary>
    public string Estado { get; set; } = "pendiente";

    public string  ProductoId { get; set; } = string.Empty;
    public Product Producto   { get; set; } = null!;

    /// <summary>Talla elegida por el cliente: "S", "M", "L", etc.</summary>
    public string Talla { get; set; } = string.Empty;

    /// <summary>Color hex elegido: "#DAA520"</summary>
    public string Color { get; set; } = string.Empty;

    public int Cantidad { get; set; } = 1;
}