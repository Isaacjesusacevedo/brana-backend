# Brana — Esoteric Store

> E-commerce de moda esotérica construido con Vue 3 + ASP.NET Core 9.

![Estado](https://img.shields.io/badge/estado-en%20desarrollo-yellow)
![Frontend](https://img.shields.io/badge/frontend-Vue%203.5-42b883)
![Backend](https://img.shields.io/badge/backend-ASP.NET%20Core%209-512bd4)
![Base de datos](https://img.shields.io/badge/db-PostgreSQL%20%28Supabase%29-336791)
![Imágenes](https://img.shields.io/badge/media-Cloudinary-3448c5)
![Docker](https://img.shields.io/badge/deploy-Docker%20%2B%20Render-46e3b7)
![type-check](https://img.shields.io/badge/type--check-0%20errores-brightgreen)
![dotnet build](https://img.shields.io/badge/dotnet%20build-0%20errores%200%20warnings-brightgreen)

---

## 📑 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Quick Start](#-quick-start)
- [Arquitectura del Sistema](#-arquitectura-del-sistema)
- [Estructura de Carpetas](#-estructura-de-carpetas)
- [Frontend (Brana)](#-frontend-brana)
- [Backend (backend)](#-backend-backend)
- [Base de Datos](#-base-de-datos)
- [Gestión de Imágenes con Cloudinary](#-gestión-de-imágenes-con-cloudinary)
- [Tecnologías Utilizadas](#-tecnologías-utilizadas)
- [Variables de Entorno](#-variables-de-entorno)
- [Flujo de Comunicación Frontend ↔ Backend](#-flujo-de-comunicación-frontend--backend)
- [Instalación y Ejecución](#-instalación-y-ejecución)
- [Despliegue en Producción](#-despliegue-en-producción)
- [Salud Técnica del Proyecto](#-salud-técnica-del-proyecto)
- [Limitaciones y Bugs Conocidos](#-limitaciones-y-bugs-conocidos)
- [Hoja de Ruta (Roadmap)](#-hoja-de-ruta-roadmap)
- [Evaluación de Calidad del Código](#-evaluación-de-calidad-del-código)
- [Recomendaciones Técnicas](#-recomendaciones-técnicas)
- [Contribuir](#-contribuir)
- [Licencia](#-licencia)

---

## 📋 Descripción General

**Brana** es una tienda de ropa con estética esotérica/mística que comercializa remeras, buzos y pantalones a través de una vitrina online de diseño propio. El sistema permite a los clientes explorar el catálogo, filtrar por categoría o precio, ver el detalle de cada producto y enviar pedidos de compra mediante un flujo de checkout como invitado.

- **Usuarios objetivo:** Consumidores interesados en moda esotérica.
- **Panel admin:** Gestión de pedidos, productos e imágenes para el propietario.
- **Frontend:** [Netlify → transcendent-torrone-b7c5f8.netlify.app](https://transcendent-torrone-b7c5f8.netlify.app)
- **Backend:** [Render → brana-backend.onrender.com](https://brana-backend.onrender.com)

### Estado actual del proyecto

| Área | Estado | Detalle |
|------|--------|---------|
| Catálogo de productos | ✅ Funcional | Filtros, paginación y detalle operativos |
| Creación de pedidos | ✅ Funcional | Checkout de invitado sin pago |
| Upload de imágenes | ✅ Funcional | `POST /api/upload/imagen` → Cloudinary CDN |
| Autenticación JWT admin | ✅ Funcional | `POST /api/auth/login` → token 24h |
| Panel de administración | 🚧 En desarrollo | Login y creación de productos funcionales; otras vistas en desarrollo |
| Pasarela de pago (MercadoPago) | 📋 Planificado | Sin implementar |
| Páginas secundarias (Nosotros, FAQ, etc.) | 📋 Planificado | Rutas redirigen al home temporalmente |

---

## ⚡ Quick Start

> **Requisitos:** Node.js ≥ 20.19.0, .NET SDK 9.0

```bash
# 1. Clonar el repositorio
git clone <url-del-repo>

# 2. Terminal 1 — Backend
cd backend
dotnet run
# → API en http://localhost:8080 · Swagger en http://localhost:8080/swagger

# 3. Terminal 2 — Frontend
cd Brana
npm install && npm run dev
# → App en http://localhost:5173
```

> **Sin PostgreSQL local:** Podés apuntar directamente a Supabase seteando `DATABASE_URL` como variable de entorno local.

---

## 🏗️ Arquitectura del Sistema

```
┌──────────────────────────────────────────────────────────┐
│                   NAVEGADOR DEL CLIENTE                  │
│                                                          │
│  Vue 3 SPA · TypeScript · Pinia · Vue Router 4           │
│  Build: Vite 7 → dist/ estático                         │
│  Hosting: Netlify (CDN global)                           │
└───────────────────────┬──────────────────────────────────┘
                        │  HTTPS · REST JSON
                        │  VITE_API_URL → brana-backend.onrender.com
                        ▼
┌──────────────────────────────────────────────────────────┐
│              ASP.NET Core 9 Web API (Docker)             │
│                                                          │
│  ExceptionMiddleware → CORS → JWT → Controllers          │
│                       ↓                                  │
│  IProductService · ICategoryService · IOrderService      │
│  ICloudinaryService                                      │
│           (EF Core 9 · Npgsql 9.0.4)                    │
│  Hosting: Render · Puerto: PORT env (default 8080)       │
└───────┬───────────────────────────────┬──────────────────┘
        │  PostgreSQL (Npgsql)           │  HTTPS · SDK
        │  Supavisor Transaction Pooler  │  CloudinaryDotNet 1.27.0
        │  IPv4 · Port 6543             │
        ▼                               ▼
┌───────────────────┐         ┌──────────────────────────┐
│  Supabase         │         │  Cloudinary CDN           │
│  PostgreSQL       │         │  Cloud: db7oid66k         │
│  aws-1-us-east-1  │         │  q_auto · f_auto          │
│  pooler.supabase  │         │  Carpeta: brana/productos │
└───────────────────┘         └──────────────────────────┘
```

**Ciclo de vida de una petición:**

1. El SPA llama a `apiFetch()` apuntando a `VITE_API_URL`.
2. El pipeline ejecuta: `ExceptionMiddleware` → CORS → JWT (si ruta protegida) → Controller.
3. El controller delega al servicio correspondiente.
4. El servicio consulta `AppDbContext` (EF Core → Npgsql → Supabase Pooler).
5. La respuesta se serializa como JSON **camelCase** dentro del envelope `ApiResponse<T>`.

> **Connection Pooling:** La conexión usa **Supavisor** (Transaction mode, puerto 6543, IPv4) con `Pooling=false` — requerido por el modo Transaction de Supavisor para evitar conflictos con el pool interno de Npgsql (causa del error `status 139` en Linux).

---

## 📁 Estructura de Carpetas

### `Brana/` — Frontend Vue 3

```
Brana/
├── .env.local              # Desarrollo: VITE_API_URL=http://localhost:8080/api
├── .env.production         # Producción Netlify: VITE_API_URL=https://brana-backend.onrender.com/api
├── index.html              # Entry point HTML
├── package.json
├── tsconfig.app.json
└── src/
    ├── main.ts             # Bootstrap: Vue, Pinia, Element Plus, Router
    ├── App.vue             # Componente raíz
    │
    ├── router/
    │   ├── index.ts        # Router principal — ÚNICA fuente de rutas activas
    │   └── admin.routes.ts # Rutas admin bajo AdminLayout
    │
    ├── views/
    │   ├── tienda/         # Vistas del cliente (todas funcionales)
    │   │   ├── HomeView.vue
    │   │   ├── CatalogoView.vue
    │   │   ├── ProductsView.vue
    │   │   ├── ProductoView.vue
    │   │   ├── CarritoView.vue
    │   │   └── CheckoutView.vue
    │   ├── admin/
    │   │   ├── AdminLoginView.vue      # ✅ Login JWT funcional
    │   │   ├── AdminDashboardView.vue  # 🚧 TODO: métricas
    │   │   ├── AdminPedidosView.vue    # 🚧 TODO: tabla + PATCH estado
    │   │   ├── AdminProductosView.vue  # ✅ Crear producto + upload Cloudinary
    │   │   └── AdminUsuariosView.vue   # 🚧 TODO: gestión de usuarios
    │   └── noticias/
    │       └── NoticiasView.vue
    │
    ├── components/
    │   ├── tienda/
    │   │   ├── common/     # Header, NavBar, Footer, StarBackground, BaseModal
    │   │   ├── home/       # HomeHero, HomeFeatured, HomeFusion, HomePhilosophy, HomePilares
    │   │   └── producto/   # ProductCard, CategorySection, ProductFilter, ProducQuickView
    │   └── ui/             # BaseButton, BaseInput
    │
    ├── stores/
    │   └── useCartStore.ts # Carrito con persistencia localStorage + códigos de descuento
    │
    ├── services/
    │   └── tienda/
    │       └── api.ts      # Wrapper fetch: productosApi, categoriasApi, pedidosApi
    │
    ├── types/
    │   ├── producto.types.ts   # Product, Category, FeaturedItem
    │   ├── api-types.ts        # Order, OrderStatus, Cart, CartItem
    │   └── carousel.ts
    │
    ├── layouts/
    │   └── AdminLayout.vue     # Layout con <RouterView> para el panel admin
    │
    ├── utils/
    │   └── cloudinary.ts       # buildCldImage() con f_auto/q_auto para el frontend
    └── data/
        └── brandPilares.ts     # Contenido estático de pilares de marca
```

### `backend/` — Backend ASP.NET Core 9

```
backend/
├── Dockerfile              # Multi-stage build; expone puerto 8080
├── Backend.csproj          # Dependencias NuGet
├── Backend.sln
├── appsettings.json        # Configuración local (sin secretos reales)
├── Program.cs              # Composición: DI, pipeline, JWT, CORS, auto-migración
│
├── Controllers/
│   ├── ProductsController.cs       # GET público / POST·PUT·DELETE [Authorize]
│   ├── Categoriescontroller.cs     # GET categorías — público
│   ├── OrdersController.cs         # GET/POST/PATCH pedidos
│   ├── UploadController.cs         # POST/DELETE imágenes → Cloudinary
│   └── AuthController.cs           # POST /api/auth/login → JWT
│
├── Services/
│   ├── ProductService.cs           # IProductService: filtros, paginación, CRUD
│   ├── CategoryService.cs          # ICategoryService: GetAll, GetBySlug
│   ├── OrderService.cs             # IOrderService: Create, GetById, GetAll, UpdateEstado
│   ├── CloudinaryService.cs        # ICloudinaryService: Upload, Delete
│   └── Serviceextencions.cs        # AddApplicationServices() — registra los 4 servicios
│
├── Data/
│   └── AppDbContext.cs             # DbContext + DbSeeder (idempotente)
│
├── Models/
│   ├── Entities.cs                 # Product, Category, ProductImage, ProductColor, Order, AdminUser
│   └── DTOs.cs                     # ApiResponse<T>, PagedResponse<T>, todos los DTOs
│
├── Helpers/
│   └── PasswordHelper.cs           # BCrypt hash/verify
│
├── Middleware/
│   └── ExceptionMiddleware.cs      # Captura global → ApiResponse tipado
│
└── Migrations/
    └── *_InitialPostgres.cs        # Migración compatible con Npgsql/PostgreSQL
```

---

## 🖥️ Frontend (Brana)

### Stack y versiones

| Herramienta | Versión |
|-------------|---------|
| Vue | ^3.5.27 |
| TypeScript | ~5.9.3 |
| Vite | ^7.3.1 |
| Pinia | ^3.0.4 |
| Vue Router | ^4.6.4 |
| Element Plus | ^2.13.2 |
| vue-tsc | ^3.2.4 |

### Auditoría de Contratos API — Coherencia Frontend ↔ Backend

El backend serializa en **camelCase** (`JsonNamingPolicy.CamelCase`). Verificación campo por campo:

| C# DTO | JSON (camelCase) | TypeScript | Estado |
|--------|-----------------|------------|--------|
| `string Id` | `id` | `id: string \| number` | ✅ |
| `string? Nombre` | `nombre` | `nombre?: string` | ✅ |
| `string? Titulo` | `titulo` | `titulo?: string` | ✅ |
| `string Imagen` | `imagen` | `imagen: string` | ✅ |
| `List<string>? Imagenes` | `imagenes` | `imagenes?: string[]` | ✅ |
| `decimal? Precio` | `precio` | `precio?: number` | ✅ |
| `decimal? PrecioAnterior` | `precioAnterior` | `precioAnterior?: number` | ✅ |
| `string? Categoria` | `categoria` | `categoria?: string` | ✅ |
| `bool Nuevo` | `nuevo` | `nuevo?: boolean` | ✅ |
| `List<string>? Colores` | `colores` | `colores?: string[]` | ✅ |
| `List<string>? Tallas` | `tallas` | `tallas?: string[]` | ✅ |
| `string? Badge` | `badge` | `badge?: string` | ✅ |
| `string? Size` | `size` | `size?: 'normal' \| 'featured' \| 'wide' \| 'tall'` | ✅ |

> **Resultado de auditoría:** Coherencia 100%. Sin discrepancias de nombres entre DTOs C# y types TypeScript.

### Switch automático de URL por entorno

| Comando | Archivo | `BASE_URL` efectivo |
|---------|---------|---------------------|
| `npm run dev` | `.env.local` | `http://localhost:8080/api` |
| `npm run build` | `.env.production` | `https://brana-backend.onrender.com/api` |

### Rutas del Frontend

| Ruta | Vista | Estado |
|------|-------|--------|
| `/` | `HomeView.vue` | ✅ Funcional |
| `/catalogo` | `CatalogoView.vue` | ✅ Funcional |
| `/productos` | `ProductsView.vue` | ✅ Funcional |
| `/producto/:id` | `ProductoView.vue` | ✅ Funcional |
| `/carrito` | `CarritoView.vue` | ✅ Funcional |
| `/checkout` | `CheckoutView.vue` | ✅ Funcional |
| `/noticias` | `NoticiasView.vue` | ✅ Funcional |
| `/admin/login` | `AdminLoginView.vue` | ✅ Funcional |
| `/admin` | `AdminDashboardView.vue` | 🚧 En desarrollo |
| `/admin/pedidos` | `AdminPedidosView.vue` | 🚧 En desarrollo |
| `/admin/productos` | `AdminProductosView.vue` | ✅ Funcional |
| `/admin/usuarios` | `AdminUsuariosView.vue` | 🚧 En desarrollo |
| `/filosofia`, `/contacto`, `/about`, etc. | — | ↪ Redirigen a `/` |

---

## ⚙️ Backend (backend)

### Stack y versiones

| Herramienta | Versión |
|-------------|---------|
| ASP.NET Core | 9.0 |
| C# | 13 (.NET 9) |
| Entity Framework Core | 9.0.0 |
| Npgsql.EFCore.PostgreSQL | 9.0.4 |
| CloudinaryDotNet | 1.27.0 |
| JwtBearer | 9.0.0 |
| Swashbuckle.AspNetCore | 6.9.0 |

### Patrón de Arquitectura

```
Controllers  →  IProductService    →  AppDbContext (EF Core → Npgsql)
             →  ICategoryService   →        ↓
             →  IOrderService      →  Supabase PostgreSQL (Supavisor Pooler)
             →  ICloudinaryService →  Cloudinary API
```

### Referencia Completa de Endpoints

#### Productos

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `GET` | `/api/products` | ❌ | Lista paginada con filtros |
| `GET` | `/api/products/featured` | ❌ | Destacados (`?limit=6`) |
| `GET` | `/api/products/{id}` | ❌ | Detalle de producto |
| `POST` | `/api/products` | ✅ JWT | Crear producto (`multipart/form-data` + imagen opcional) |
| `PUT` | `/api/products/{id}` | ✅ JWT | Actualizar producto |
| `DELETE` | `/api/products/{id}` | ✅ JWT | Eliminar → `204 No Content` |

#### Categorías

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `GET` | `/api/categories` | ❌ | Todas las categorías con productos |
| `GET` | `/api/categories/{slug}` | ❌ | Categoría por slug con paginación |

#### Pedidos

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/api/orders` | ❌ | Crear pedido de invitado → `201 Created` |
| `GET` | `/api/orders` | ❌ ⚠️ | Listar pedidos (`?estado=pendiente`) |
| `GET` | `/api/orders/{id:guid}` | ❌ | Detalle de un pedido |
| `PATCH` | `/api/orders/{id:guid}/estado` | ❌ | Cambiar estado del pedido |

> **⚠️ `GET /api/orders` sin auth** — expone datos personales. Pendiente agregar `[Authorize]`.

#### Imágenes (Cloudinary)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/api/upload/imagen` | ❌ | Sube imagen → retorna URL HTTPS de Cloudinary |
| `DELETE` | `/api/upload/imagen?publicId=...` | ❌ | Elimina imagen por publicId |

**Respuesta de upload:**
```json
{
  "success": true,
  "data": {
    "url": "https://res.cloudinary.com/db7oid66k/image/upload/...",
    "publicId": "brana/productos/nombre-archivo",
    "width": 800,
    "height": 600,
    "bytes": 123456
  }
}
```

> La `url` es directamente usable en `<img src="...">` o `productosApi`.

#### Autenticación Admin

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| `POST` | `/api/auth/login` | ❌ | Credenciales admin → JWT válido 24h |

**Request:**
```json
{ "username": "admin", "password": "..." }
```

**Response:**
```json
{
  "success": true,
  "data": { "token": "eyJ...", "expiresAt": "2026-03-16T..." }
}
```

### Parámetros de Query para `GET /api/products`

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `Categoria` | string | — | `remeras` \| `buzos` \| `pantalones` |
| `Search` | string | — | Búsqueda en `Nombre` y `Descripcion` |
| `MinPrecio` | decimal | — | Precio mínimo |
| `MaxPrecio` | decimal | — | Precio máximo |
| `SoloNuevos` | bool | — | Solo `Nuevo = true` |
| `OrderBy` | string | `nombre` | `nombre` \| `precio` \| `nuevo` |
| `Desc` | bool | `false` | Orden descendente |
| `Page` | int | `1` | Página |
| `PageSize` | int | `12` | Ítems por página |

### Envelope de Respuesta

```json
{ "success": true, "data": { }, "message": "Opcional" }
```
```json
{ "success": false, "errors": ["Descripción del error"] }
```

### Manejo de Errores (ExceptionMiddleware)

| Excepción C# | HTTP | Uso |
|-------------|------|-----|
| `ArgumentException` | `400` | Validación de negocio |
| `KeyNotFoundException` | `404` | Entidad no encontrada |
| `UnauthorizedAccessException` | `401` | Sin permisos |
| Cualquier otra | `500` | Error inesperado (sin stack trace expuesto) |

---

## 🗄️ Base de Datos

### Supabase + PostgreSQL + Supavisor

- **Proveedor:** Supabase (PostgreSQL managed, región `aws-1-us-east-1`)
- **Conexión:** Supavisor Transaction Pooler — puerto `6543`, IPv4
- **ORM:** Entity Framework Core 9 + Npgsql 9.0.4
- **Auto-migración:** `db.Database.MigrateAsync()` al iniciar (con try-catch resiliente)

### Por qué `Pooling=false`

```
Supavisor (Transaction mode)
    └── gestiona su propio pool de conexiones a PostgreSQL
         ↑
         El pool interno de Npgsql compite con este pool
         → genera el error "Exited with status 139" en Linux
```

Solución: `Pooling=false` desactiva el pool de Npgsql; Supavisor gestiona todo.

### Formato del `DATABASE_URL`

```
User Id=postgres.[PROJECT_ID];Password=[PASSWORD];Server=aws-1-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Ssl Mode=Require;Trust Server Certificate=true;Pooling=false
```

### Diagrama Entidad-Relación

```
Categories ────────────────────────────────────
  Id (PK)  ·  Slug (UNIQUE)  ·  Nombre
  Descripcion  ·  Icono  ·  Ruta?
       │ 1
       │ N
Products ──────────────────────────────────────
  Id (text PK)  ·  Nombre?  ·  Titulo?
  Descripcion  ·  Precio (numeric 10,2)?
  PrecioAnterior (numeric 10,2)?
  Badge?  ·  Size?  ·  Nuevo (bool)
  Stock?  ·  TallasJson (text)?
  CaracteristicasJson (text)?  ·  Ruta?
  CategoriaId (FK → Categories, RESTRICT)
       │ 1                │ 1
       │ N                │ N
ProductImages          ProductColors
  Id · Url               Id · Hex · Nombre?
  EsPrincipal · Orden    ProductoId (FK CASCADE)
  ProductoId (FK CASCADE)

AdminUsers ─────────────────────────────────────
  Id (PK)  ·  Username (UNIQUE)  ·  PasswordHash

Orders ──────────────────────────────────────────
  Id (uuid PK)  ·  Nombre  ·  Email  ·  Telefono
  NotasAdicionales?  ·  FechaPedido (timestamptz UTC)
  Estado  ·  Talla  ·  Color  ·  Cantidad
  ProductoId (FK → Products, RESTRICT)
```

### Máquina de Estados de Pedidos

```
pendiente → confirmado → enviado → entregado
    └─────────────────────────────→ cancelado (desde cualquier estado)
```

**Estados válidos para `PATCH /api/orders/{id}/estado`:** `pendiente`, `confirmado`, `enviado`, `entregado`, `cancelado`

### Seeder Automático (primer arranque)

| Dato | Detalle |
|------|---------|
| Categorías | 3: Remeras (✧), Buzos (◆), Pantalones (☆) |
| Productos regulares | 12 (4 por categoría) con imágenes, colores y talles |
| Productos destacados | 6 (`featured-1` a `featured-6`) para el hero |
| Admin user | Credenciales desde `ADMIN_USERNAME` / `ADMIN_PASSWORD` (bcrypt hash) |

### Comandos de Migración

```bash
dotnet ef database update           # Aplicar migraciones pendientes
dotnet ef migrations add <Nombre>   # Nueva migración
dotnet ef migrations list           # Estado actual
```

---

## 🖼️ Gestión de Imágenes con Cloudinary

### Flujo completo de upload

```
Panel Admin (Vue)
      │  POST multipart/form-data
      ▼
POST /api/upload/imagen
      │  ICloudinaryService.UploadImageAsync()
      │  Transformation: Quality("auto") + FetchFormat("auto")
      ▼
Cloudinary CDN (cloud: db7oid66k)
      │  Carpeta: brana/productos
      │  Retorna SecureUrl HTTPS
      ▼
UploadResultDto { url, publicId, width, height, bytes }
      │
      │  url → usable directamente en <img src="...">
      │  url → guardar en ProductCreateDto.ImagenUrls[]
      ▼
POST /api/products → DB (Products + ProductImages)
```

### Configuración via `IConfiguration`

En Render, las variables con doble guion bajo son interpretadas como jerarquía:

| Variable Render | Sección IConfiguration |
|----------------|------------------------|
| `CLOUDINARY_CLOUD_NAME` | `Cloudinary:CloudName` |
| `CLOUDINARY_API_KEY` | `Cloudinary:ApiKey` |
| `CLOUDINARY_API_SECRET` | `Cloudinary:ApiSecret` |

### Restricciones de Upload

| Parámetro | Valor |
|-----------|-------|
| Tipos permitidos | JPEG, PNG, WebP, GIF |
| Tamaño máximo | 10 MB |
| Carpeta default | `brana/productos` |
| Transformaciones | `q_auto` + `f_auto` |
| Protocolo | HTTPS forzado (`Secure = true`) |

---

## 🛠️ Tecnologías Utilizadas

| Capa | Tecnología | Versión |
|------|------------|---------|
| Framework frontend | Vue.js | ^3.5.27 |
| Lenguaje frontend | TypeScript | ~5.9.3 |
| Build tool | Vite | ^7.3.1 |
| Estado global | Pinia | ^3.0.4 |
| Router | Vue Router | ^4.6.4 |
| UI Components | Element Plus | ^2.13.2 |
| SDK Cloudinary (frontend) | @cloudinary/vue + url-gen | ^1.13.4 / ^1.22.0 |
| Framework backend | ASP.NET Core | 9.0 |
| Lenguaje backend | C# | 13 (.NET 9) |
| ORM | Entity Framework Core | 9.0.0 |
| Driver PostgreSQL | Npgsql | 9.0.4 |
| SDK Cloudinary (backend) | CloudinaryDotNet | 1.27.0 |
| Autenticación | JwtBearer | 9.0.0 |
| Documentación API | Swagger / Swashbuckle | 6.9.0 |
| Contenedor | Docker | multi-stage build |
| Hosting backend | Render | — |
| Hosting frontend | Netlify | — |
| Base de datos | Supabase (PostgreSQL) | — |
| CDN imágenes | Cloudinary | — |

---

## 🔑 Variables de Entorno

### Backend — Render (Environment Variables)

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DATABASE_URL` | Connection string Supabase Pooler (Transaction mode, port 6543) | `User Id=postgres.xxx;Password=...;Server=aws-1-us-east-1.pooler.supabase.com;Port=6543;Database=postgres` |
| `CLOUDINARY_CLOUD_NAME` | Cloud name Cloudinary | `db7oid66k` |
| `CLOUDINARY_API_KEY` | API Key Cloudinary | *(secret — no exponer)* |
| `CLOUDINARY_API_SECRET` | API Secret Cloudinary | *(secret — no exponer)* |
| `JWT_SECRET` | Clave HMAC-SHA256 para tokens JWT (≥ 32 chars) | Generar: `openssl rand -base64 32` |
| `FRONTEND_URL` | URL del frontend para CORS dinámico | `https://transcendent-torrone-b7c5f8.netlify.app` |
| `ADMIN_USERNAME` | Usuario inicial del panel admin | `admin` |
| `ADMIN_PASSWORD` | Contraseña inicial del panel admin | *(secret — mínimo 12 chars)* |
| `ASPNETCORE_ENVIRONMENT` | Entorno .NET | `Production` |
| `PORT` | Puerto del servidor (Render lo inyecta automáticamente) | `8080` (automático) |

### Frontend — Netlify (Build Environment Variables)

| Variable | Descripción | Valor |
|----------|-------------|-------|
| `VITE_API_URL` | URL base del backend | `https://brana-backend.onrender.com/api` |
| `VITE_CLOUDINARY_CLOUD_NAME` | Cloud name para optimización de imágenes | `db7oid66k` |

### Desarrollo Local

**`backend/appsettings.json`** (referencia — sin secretos reales):
```jsonc
{
  "Urls": "http://localhost:8080",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=esoteric_store;Username=postgres;Password=tu_password"
  },
  "Cloudinary": { "CloudName": "", "ApiKey": "", "ApiSecret": "" }
}
```

**`Brana/.env.local`**:
```env
VITE_API_URL=http://localhost:8080/api
VITE_CLOUDINARY_CLOUD_NAME=db7oid66k
```

**`Brana/.env.production`**:
```env
VITE_API_URL=https://brana-backend.onrender.com/api
VITE_CLOUDINARY_CLOUD_NAME=db7oid66k
```

---

## 🔄 Flujo de Comunicación Frontend ↔ Backend

### Wrapper fetch central (`api.ts`)

```typescript
const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:8080/api';

async function apiFetch<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const res  = await fetch(`${BASE_URL}${endpoint}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  const json: ApiResponse<T> = await res.json();

  if (!res.ok || !json.success)
    throw new Error(json.errors?.[0] ?? `HTTP ${res.status}`);

  return json.data;
}
```

### Política CORS (Program.cs)

```csharp
// Orígenes hardcodeados (localhost dev + Netlify prod)
"http://localhost:5173" … "http://localhost:5177"
"http://localhost:3000" · "http://localhost:4173"
"https://transcendent-torrone-b7c5f8.netlify.app"

// Dinámico — agrega FRONTEND_URL si está configurada
if (!string.IsNullOrWhiteSpace(frontendUrl))
    corsOrigins.Add(frontendUrl);
```

### Resiliencia de inicio (Program.cs)

```csharp
// Si la DB no está disponible al iniciar, la app levanta igual
try {
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
} catch (Exception ex) {
    Console.Error.WriteLine($"ERROR DB: {ex.Message}");
    // La app continúa — evita crash con status 139
}
```

---

## 🚀 Instalación y Ejecución

### Backend (local)

```bash
cd backend

# Restaurar paquetes NuGet
dotnet restore

# Setear variables de entorno (PowerShell)
$env:DATABASE_URL="User Id=...;Password=...;Server=aws-1-us-east-1.pooler.supabase.com;Port=6543;Database=postgres"
$env:JWT_SECRET="mi-clave-secreta-de-al-menos-32-caracteres"

# Iniciar (auto-migra y siembra al arrancar)
dotnet run
# API: http://localhost:8080
# Swagger: http://localhost:8080/swagger
```

### Frontend (local)

```bash
cd Brana
npm install
npm run dev
# → http://localhost:5173
```

### Scripts NPM

| Script | Descripción |
|--------|-------------|
| `npm run dev` | Servidor de desarrollo con HMR |
| `npm run build` | Type-check + build de producción (en paralelo con `run-p`) |
| `npm run build-only` | Solo build Vite (sin type-check) |
| `npm run type-check` | Solo `vue-tsc --build` |
| `npm run preview` | Preview del build en local |

---

## ☁️ Despliegue en Producción

### Backend → Render (Docker)

1. Conectar repositorio en **Render → New Web Service → Docker**
2. Render detecta el `Dockerfile` en la raíz de `backend/`
3. Configurar todas las variables de entorno de la tabla anterior
4. Render inyecta `PORT=8080` automáticamente

**`Dockerfile` (multi-stage):**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]
```

### Frontend → Netlify

| Configuración | Valor |
|---------------|-------|
| Build command | `npm run build` |
| Publish directory | `dist` |
| `VITE_API_URL` | `https://brana-backend.onrender.com/api` |
| `VITE_CLOUDINARY_CLOUD_NAME` | `db7oid66k` |

---

## 🩺 Salud Técnica del Proyecto

Estado verificado al **2026-03-15**.

| Herramienta | Comando | Resultado |
|-------------|---------|-----------|
| TypeScript | `npm run type-check` | ✅ 0 errores |
| Build Vite | `npm run build` | ✅ Sin errores |
| .NET | `dotnet build` | ✅ 0 errores · 0 advertencias |

### Historial de cambios estructurales

| Fecha | Cambio | Detalle |
|-------|--------|---------|
| 2026-03 | Migración DB | SQLite → PostgreSQL (Npgsql 9.0.4 + Supabase Pooler) |
| 2026-03 | Migración hosting | Railway → Render (Docker, puerto 8080) |
| 2026-03 | Nuevos servicios | `ICloudinaryService`, `IOrderService`, `ICategoryService` |
| 2026-03 | Auth | JWT Bearer + `AdminUser` + `POST /api/auth/login` |
| 2026-03 | Upload | `UploadController` → Cloudinary CDN |
| 2026-03 | Resiliencia | Try-catch en MigrateAsync; `EnsureParam` Supabase Pooler |
| 2026-03 | Limpieza | 46 archivos stub eliminados; type-check en 0 errores |
| 2026-03-16 | Panel admin | `AdminLoginView` y `AdminProductosView` funcionales. Fix: `tipo`→`categoriaId` |
| 2026-03-16 | Seguridad | Repositorios recreados limpios. Historial con credenciales purgado. `.gitignore` endurecido |

---

## ⚠️ Limitaciones y Bugs Conocidos

| # | Severidad | Descripción | Archivo |
|---|-----------|-------------|---------|
| 1 | ✅ Resuelto | Imports rotos en router | `src/router/index.ts` |
| 2 | ✅ Resuelto | Rutas `/admin/*` sin registrar | `src/router/admin.routes.ts` |
| 3 | ✅ Resuelto | Controllers sin capa de servicio | `Services/` |
| 4 | ✅ Resuelto | SQLite en producción (pérdida de datos) | Migrado a Supabase |
| 5 | ✅ Resuelto | Error status 139 en Render (Supabase IPv6 + pool conflict) | `Program.cs` |
| 6 | 🔴 Crítico | Imágenes del seed usan rutas locales (`/images/remera-1.jpg`) — rotas en producción | `AppDbContext.cs` |
| 7 | 🔴 Crítico | `GET /api/orders` público — expone emails y teléfonos de clientes | `OrdersController.cs` |
| 8 | 🟡 Media | Sin navigation guards — `/admin/*` accesible sin JWT desde el browser | `router/` |
| 9 | 🟡 Media | `AuthController` accede a `AppDbContext` directamente (fuera del patrón de servicios) | `AuthController.cs` |
| 10 | ✅ Resuelto | Formulario de login usaba campo `email` en vez de `username` | `AdminLoginView.vue` |
| 11 | ✅ Resuelto | Formulario de productos enviaba `tipo` (string) en vez de `categoriaId` (int) → FK violation | `AdminProductosView.vue` |

---

## 🗺️ Hoja de Ruta (Roadmap)

| Funcionalidad | Prioridad | Estado |
|---------------|-----------|--------|
| Imports router corregidos | 🔴 | ✅ Resuelto |
| Servicios `IOrderService` / `ICategoryService` | 🔴 | ✅ Resuelto |
| Migración PostgreSQL (Supabase) | 🔴 | ✅ Resuelto |
| JWT Authentication + `AdminUser` | 🔴 | ✅ Resuelto |
| Upload de imágenes a Cloudinary | 🔴 | ✅ Resuelto |
| Resiliencia Supabase Pooler | 🔴 | ✅ Resuelto |
| **Login admin + crear productos** | 🔴 | ✅ Resuelto |
| **Implementar vistas admin restantes** (Pedidos, Dashboard, Usuarios) | 🔴 | ⏭️ Siguiente paso |
| **Proteger `GET /api/orders` con `[Authorize]`** | 🔴 | ⏭️ Siguiente paso |
| **Navigation Guards (guards.ts)** | 🔴 | ⏭️ Siguiente paso |
| **Subir imágenes reales a Cloudinary y actualizar seed** | 🔴 | ⏭️ Siguiente paso |
| Integración MercadoPago Checkout Pro | 🟡 | 📋 Planificado |
| Tests unitarios y de integración | 🟡 | 📋 Planificado |
| Columnas JSON → tablas normalizadas | 🟡 | 📋 Planificado |
| FluentValidation para DTOs | 🟢 | 📋 Planificado |
| Páginas secundarias (FAQ, Términos, etc.) | 🟢 | 📋 Planificado |

---

## ✅ Evaluación de Calidad del Código

### Lo que está bien implementado

| Área | Detalle |
|------|---------|
| **Coherencia de contratos** | DTOs C# ↔ Types TypeScript verificados campo a campo. 0 discrepancias. |
| **Envelope genérico** | `ApiResponse<T>` uniforme en todos los endpoints |
| **Paginación completa** | `PagedResponse<T>` con `hasNext`, `hasPrev`, `totalPages` |
| **Capa de servicios** | Los 4 controllers delegan completamente a sus interfaces |
| **JWT correcto** | HMAC-SHA256, `ClockSkew=Zero`, sin validación de issuer/audience |
| **Cloudinary seguro** | `Secure=true`, `q_auto`/`f_auto`, carpetas organizadas |
| **CORS explícito** | Sin wildcard `*`; lista específica + `FRONTEND_URL` dinámico |
| **Resiliencia DB** | Try-catch en MigrateAsync; `EnsureParam` para parámetros Supabase |
| **Seed idempotente** | `if (AnyAsync()) return;` — sobrevive reinicios sin duplicar |
| **Swagger con Bearer** | UI interactiva con JWT en `/swagger` |
| **Docker multi-stage** | Imagen final usa `aspnet:9.0` (sin SDK), mínima superficie de ataque |
| **Sin secretos hardcodeados** | Todo vía `IConfiguration` y variables de entorno |

### Lo que necesita mejora

| Área | Problema |
|------|----------|
| ~~**Imports rotos en router**~~ | ✅ Resuelto |
| ~~**Rutas admin sin registrar**~~ | ✅ Resuelto |
| ~~**Controllers sin capa de servicios**~~ | ✅ Resuelto |
| ~~**SQLite en producción**~~ | ✅ Resuelto — migrado a Supabase |
| **`GET /api/orders` público** | Expone datos personales de clientes |
| **Imágenes del seed rotas** | URLs locales inexistentes en producción |
| **Sin navigation guards** | `/admin/*` accesible sin autenticación |
| **Sin tests** | Cero cobertura en ambos proyectos |
| **Columnas JSON** | `TallasJson`, `CaracteristicasJson` como texto plano; no consultables |

---

## 💡 Recomendaciones Técnicas

### 1 — Proteger `GET /api/orders` *(30 minutos)*
```csharp
[HttpGet]
[Authorize]  // ← agregar
public async Task<IActionResult> GetAll(...) { ... }
```

### 2 — Subir imágenes reales a Cloudinary *(1-2 días)*
Reemplazar las URLs `/images/remera-1.jpg` del seeder por URLs absolutas de Cloudinary. Sin esto, todas las imágenes de productos están rotas en producción.

### 3 — Implementar vistas admin + navigation guards *(3-5 días)*
- `AdminLoginView.vue`: POST a `/api/auth/login`, guardar JWT en localStorage
- `guards.ts`: `router.beforeEach()` que verifica el token antes de `/admin/*`
- `AdminProductosView.vue`: CRUD completo con Cloudinary Upload Widget

### 4 — Integrar MercadoPago Checkout Pro *(3-5 días)*
`POST /api/payments/preference` → MercadoPago API → `preferenceId` → frontend redirige al checkout de MP.

### 5 — Agregar Tests *(2-3 días)*
- **Backend:** `xUnit` + `WebApplicationFactory<Program>` + TestContainers
- **Frontend:** `Vitest` + `@vue/test-utils` + `msw`

---

## 🤝 Contribuir

### Convención de Ramas

```
main        → producción (protegida)
dev         → integración
feature/... → nueva funcionalidad
fix/...     → corrección de bug
chore/...   → mantenimiento
```

### Checklist antes de PR

```bash
# Frontend
npm run type-check  # 0 errores TypeScript
npm run build       # build de producción sin errores

# Backend
dotnet build        # 0 errores, 0 advertencias
```

- [ ] Type-check sin errores
- [ ] Build sin errores
- [ ] Nuevos endpoints o variables de entorno documentados en este README
- [ ] Sin archivos `.env`, `appsettings.Development.json` ni `esoteric_store.db` en el commit

---

## 📄 Licencia

Sin licencia definida. Repositorio privado.

---

<details>
<summary>📊 Historial de versiones del README</summary>

| Versión | Fecha | Cambios principales |
|---------|-------|---------------------|
| v4.0 | 2026-03-16 | Panel admin funcional (Login + Productos). Fix categoriaId. Repos recreados limpios. Host corregido aws-1. Vars Cloudinary actualizadas. |
| v3.0 | 2026-03-15 | Arquitectura Render + Supabase + Cloudinary. Variables de entorno detalladas. Auditoría coherencia Frontend↔Backend (100%). Endpoints upload e auth. Resiliencia Supabase Pooler documentada. |
| v2.0 | 2026-03 | ICategoryService/IOrderService. Limpieza 46 archivos. Admin routes. CloudinaryService. JWT. |
| v1.0 | 2026-02 | README inicial generado desde codebase. |

</details>
