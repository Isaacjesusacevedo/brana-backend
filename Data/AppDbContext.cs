using EsotericStore.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EsotericStore.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products    => Set<Product>();
    public DbSet<Category> Categories  => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductColor> ProductColors => Set<ProductColor>();
    public DbSet<Order> Orders         => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Precio).HasColumnType("decimal(10,2)");
            e.Property(p => p.PrecioAnterior).HasColumnType("decimal(10,2)");
            e.HasOne(p => p.Categoria).WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Imagenes).WithOne(i => i.Producto)
                .HasForeignKey(i => i.ProductoId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Colores).WithOne(c => c.Producto)
                .HasForeignKey(c => c.ProductoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasOne(o => o.Producto).WithMany()
                .HasForeignKey(o => o.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// ─── Seeder ───────────────────────────────────────────────────────────────────
public static class DbSeeder
{
    private static readonly string TallasRopa     = JsonSerializer.Serialize(new[] { "XS", "S", "M", "L", "XL", "XXL" });
    private static readonly string TallasPantalon = JsonSerializer.Serialize(new[] { "28", "30", "32", "34", "36", "38" });

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        // ── Categorías ───────────────────────────────────────────────────────
        var remeras    = new Category { Slug = "remeras",    Nombre = "Remeras",    Descripcion = "Diseños únicos que cuentan historias", Icono = "✧", Ruta = "/categoria/remeras" };
        var buzos      = new Category { Slug = "buzos",      Nombre = "Buzos",      Descripcion = "Confort místico para tus días",         Icono = "◆", Ruta = "/categoria/buzos" };
        var pantalones = new Category { Slug = "pantalones", Nombre = "Pantalones", Descripcion = "Movimiento y estilo en armonía",        Icono = "☆", Ruta = "/categoria/pantalones" };

        await db.Categories.AddRangeAsync(remeras, buzos, pantalones);
        await db.SaveChangesAsync();

        // ── Remeras ──────────────────────────────────────────────────────────
        await db.Products.AddRangeAsync(
            MakeProduct("rem-1", "Arcana Nº1",       remeras.Id, 45,  60,  true,  TallasRopa,    "/images/remera-1.jpg",    "Remera premium con diseño místico y acabado suave.",
                new[] { "100% algodón orgánico", "Diseño serigrafado a mano", "Corte regular fit", "Edición limitada" },
                new[] { "#000000", "#FFFFFF", "#DAA520", "#8B4513" }, altImg: "/images/remera-1-alt.jpg"),
            MakeProduct("rem-2", "Mystic Circle",    remeras.Id, 42,  null, false, TallasRopa,    "/images/remera-2.jpg",    "Círculo místico bordado con detalles en hilo dorado.",
                null, new[] { "#000000", "#1a1a1a" }),
            MakeProduct("rem-3", "Digital Tarot",    remeras.Id, 48,  null, true,  TallasRopa,    "/images/remera-3.jpg",    "Fusión de tarot tradicional con estética cyberpunk.",
                null, new[] { "#FFFFFF", "#DAA520" }),
            MakeProduct("rem-4", "Sacred Geometry",  remeras.Id, 50,  65,   false, TallasRopa,    "/images/remera-4.jpg",    "Geometría sagrada en alta definición.",
                null, new[] { "#000000", "#2a2a2a", "#3a3a3a" })
        );

        // ── Buzos ────────────────────────────────────────────────────────────
        await db.Products.AddRangeAsync(
            MakeProduct("buz-1", "Ethereal Hoodie",  buzos.Id, 85,  110, true,  TallasRopa, "/images/buzo-1.jpg", "Buzo con capucha premium, ultra suave y cálido.",
                new[] { "Tejido premium extra suave", "Capucha ajustable", "Bolsillo canguro amplio", "Puños y bajo elastizados" },
                new[] { "#000000", "#1a1a1a", "#DAA520" }, altImg: "/images/buzo-1-alt.jpg"),
            MakeProduct("buz-2", "Cosmic Energy",    buzos.Id, 90,  null, false, TallasRopa, "/images/buzo-2.jpg", "Energía cósmica plasmada en tejido premium.",
                null, new[] { "#FFFFFF", "#f0f0f0" }),
            MakeProduct("buz-3", "Lunar Phase",      buzos.Id, 95,  null, true,  TallasRopa, "/images/buzo-3.jpg", "Fases lunares en diseño minimalista elegante.",
                null, new[] { "#000000", "#2a2a2a", "#FFFFFF" }),
            MakeProduct("buz-4", "Astral Journey",   buzos.Id, 88,  115, false, TallasRopa, "/images/buzo-4.jpg", "Viaje astral representado en bordados detallados.",
                null, new[] { "#DAA520", "#FFD700", "#8B4513" })
        );

        // ── Pantalones ───────────────────────────────────────────────────────
        await db.Products.AddRangeAsync(
            MakeProduct("pant-1", "Void Walker",         pantalones.Id, 75, null, true,  TallasPantalon, "/images/pantalon-1.jpg", "Pantalón cargo con múltiples bolsillos tácticos.",
                new[] { "Tela resistente y flexible", "8 bolsillos funcionales", "Cintura ajustable", "Corte carpenter" },
                new[] { "#000000", "#1a1a1a" }),
            MakeProduct("pant-2", "Dimensional Cargo",   pantalones.Id, 80, null, false, TallasPantalon, "/images/pantalon-2.jpg", "Cargo multidimensional con diseño futurista.",
                null, new[] { "#000000", "#2a2a2a", "#8B4513" }),
            MakeProduct("pant-3", "Quantum Jogger",      pantalones.Id, 72, 95,  false, TallasPantalon, "/images/pantalon-3.jpg", "Jogger deportivo con tecnología de tejido avanzada.",
                null, new[] { "#000000", "#FFFFFF" }),
            MakeProduct("pant-4", "Parallel Lines",      pantalones.Id, 78, null, true,  TallasPantalon, "/images/pantalon-4.jpg", "Líneas paralelas que desafían las dimensiones.",
                null, new[] { "#1a1a1a", "#3a3a3a" })
        );

        // ── Featured ─────────────────────────────────────────────────────────
        await db.Products.AddRangeAsync(
            new Product
            {
                Id = "featured-1", Nombre = "OBSIDIAN_ESSENCE", Badge = "EXCLUSIVO", Size = "featured",
                Precio = 89, CategoriaId = remeras.Id, Nuevo = false,
                Descripcion = "Pieza exclusiva de la colección Obsidian. Diseño único con detalles en oro.",
                TallasJson = TallasRopa,
                CaracteristicasJson = JsonSerializer.Serialize(new[] { "Edición limitada 50 unidades", "Bordado artesanal en oro", "Tela premium importada", "Certificado de autenticidad" }),
                Imagenes = Imgs("/images/featured/obsidian.jpg", "/images/featured/obsidian-alt.jpg"),
                Colores  = Cols("#1a1a1a", "#D4AF37", "#f5f5f5")
            },
            new Product
            {
                Id = "featured-2", Nombre = "GOLDEN_VOID", Size = "normal",
                Precio = 85, CategoriaId = remeras.Id, Nuevo = false,
                Descripcion = "El vacío dorado materializado en tela premium.",
                TallasJson = TallasRopa,
                Imagenes = Imgs("/images/featured/golden-void.jpg"),
                Colores  = Cols("#1a1a1a", "#6b7280", "#1e3a8a")
            },
            new Product
            {
                Id = "featured-3", Nombre = "AMBER_RITUAL", Nuevo = true,
                Precio = 92, CategoriaId = remeras.Id,
                Descripcion = "Ritual del ámbar. Diseño ceremonial para los elegidos.",
                TallasJson = TallasRopa,
                Imagenes = Imgs("/images/featured/amber.jpg"),
                Colores  = Cols("#D4AF37", "#1a1a1a", "#854d0e")
            },
            new Product
            {
                Id = "featured-4", Nombre = "ELDRITCH_LUXURY", Size = "wide",
                Precio = 88, CategoriaId = remeras.Id, Nuevo = false,
                Descripcion = "Lujo ancestral. Para quienes comprenden el misterio.",
                TallasJson = TallasRopa,
                Imagenes = Imgs("/images/featured/eldritch.jpg"),
                Colores  = Cols("#f5f5f5", "#1a1a1a")
            },
            new Product
            {
                Id = "featured-5", Nombre = "SOLAR_GOLD",
                Precio = 90, CategoriaId = remeras.Id, Nuevo = false,
                Descripcion = "Oro solar capturado en tejido premium.",
                TallasJson = TallasRopa,
                Imagenes = Imgs("/images/featured/solar.jpg"),
                Colores  = Cols("#D4AF37", "#FFD700")
            },
            new Product
            {
                Id = "featured-6", Nombre = "COSMIC_GOLD", Size = "tall", Nuevo = true,
                Precio = 95, CategoriaId = remeras.Id,
                Descripcion = "El cosmos dorado en su máxima expresión.",
                TallasJson = TallasRopa,
                Imagenes = Imgs("/images/featured/cosmic.jpg"),
                Colores  = Cols("#D4AF37", "#1a1a1a", "#FFD700")
            }
        );

        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Product MakeProduct(string id, string nombre, int catId, decimal precio, decimal? precioAnt,
        bool nuevo, string tallasJson, string imgUrl, string descripcion,
        string[]? caracteristicas, string[] colores, string? altImg = null)
    {
        var imagenes = new List<ProductImage>
        {
            new() { Url = imgUrl, EsPrincipal = true, Orden = 0 }
        };
        if (altImg is not null)
            imagenes.Add(new() { Url = altImg, Orden = 1 });

        return new Product
        {
            Id = id, Nombre = nombre, CategoriaId = catId,
            Precio = precio, PrecioAnterior = precioAnt, Nuevo = nuevo,
            TallasJson = tallasJson, Descripcion = descripcion,
            CaracteristicasJson = caracteristicas is not null ? JsonSerializer.Serialize(caracteristicas) : null,
            Imagenes = imagenes,
            Colores  = colores.Select(h => new ProductColor { Hex = h }).ToList()
        };
    }

    private static List<ProductImage> Imgs(params string[] urls) =>
        urls.Select((u, i) => new ProductImage { Url = u, EsPrincipal = i == 0, Orden = i }).ToList();

    private static List<ProductColor> Cols(params string[] hexes) =>
        hexes.Select(h => new ProductColor { Hex = h }).ToList();
}