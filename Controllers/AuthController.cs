using EsotericStore.API.Data;
using EsotericStore.API.Helpers;
using EsotericStore.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context       = context;
        _configuration = configuration;
    }

    /// <summary>Login de administrador. Devuelve un token JWT válido por 24 horas.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var admin = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (admin is null || !PasswordHelper.VerifyPassword(dto.Password, admin.PasswordHash))
            return Unauthorized(ApiResponse<TokenResponseDto>.Fail("Credenciales inválidas"));

        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT_SECRET no está configurado.");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(24);

        var token = new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.Name, admin.Username) },
            expires: expiry,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(ApiResponse<TokenResponseDto>.Ok(new TokenResponseDto(tokenString, expiry)));
    }
}
