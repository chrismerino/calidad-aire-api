using CalidadAire.Api.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CalidadAire.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config, IPasswordHasher<object> hasher) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login(LoginDto datos)
    {
        var resultado = hasher.VerifyHashedPassword(new object(), config["Acceso:ContrasenaHash"]!, datos.Contrasena);
        if (datos.Usuario != config["Acceso:Usuario"] || resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        var expiracion = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:DuracionMinutos", 60));
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: [new Claim(ClaimTypes.Name, datos.Usuario)], expires: expiracion,
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expiraEn = expiracion });
    }
}
