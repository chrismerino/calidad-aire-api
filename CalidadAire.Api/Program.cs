using System.Text;
using CalidadAire.Api.Data;
using CalidadAire.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

if (args.Contains("--generar-hash"))
{
    Console.Write("Contraseña (mínimo 12 caracteres): ");
    var entrada = new StringBuilder();
    if (Console.IsInputRedirected)
        entrada.Append(Console.ReadLine());
    else
    {
        ConsoleKeyInfo tecla;
        while ((tecla = Console.ReadKey(true)).Key != ConsoleKey.Enter)
        {
            if (tecla.Key == ConsoleKey.Backspace && entrada.Length > 0)
                entrada.Length--;
            else if (!char.IsControl(tecla.KeyChar))
                entrada.Append(tecla.KeyChar);
        }
    }
    if (entrada.Length is < 12 or > 200)
        throw new InvalidOperationException("La contraseña debe tener entre 12 y 200 caracteres.");
    Console.WriteLine("\n" + new PasswordHasher<object>().HashPassword(new object(), entrada.ToString()));
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
var clave = builder.Configuration["Jwt:Key"];
var conexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(clave) || Encoding.UTF8.GetByteCount(clave) < 32)
    throw new InvalidOperationException("Configura Jwt:Key con al menos 32 bytes.");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Configura ConnectionStrings:DefaultConnection.");
if (string.IsNullOrWhiteSpace(builder.Configuration["Acceso:Usuario"]) ||
    string.IsNullOrWhiteSpace(builder.Configuration["Acceso:ContrasenaHash"]))
    throw new InvalidOperationException("Configura el usuario y el hash de acceso.");

builder.Services.AddControllers();
builder.Services.AddDbContext<AireDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")!));
builder.Services.AddScoped<AlertaService>();

builder.Services.AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>();
builder.Services.AddExceptionHandler<ErrorHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API de calidad del aire", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Ingresa el token obtenido en /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
