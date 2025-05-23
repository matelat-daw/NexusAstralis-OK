using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NexusAstralis.Data;
using NexusAstralis.Email;
using NexusAstralis.Interface;
using NexusAstralis.Models.Email;
using NexusAstralis.Models.User;
using NexusAstralis.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration; // Configuración de la Aplicación.

//builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp")); // Configuración del Servidor de Correo.

builder.Services.Configure<SmtpSettings>(option =>
{
    builder.Configuration.GetSection("Smtp").Bind(option); // Configuración del Servidor de Correo.
    option.Password = Environment.GetEnvironmentVariable("Gmail-Nexus"); // Contraseña del Servidor de Correo.
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserTokenService>();
builder.Services.AddTransient<IEmailSender, EmailSender>(); // Servicio de Correo.

//var conn = builder.Configuration.GetConnectionString("conn"); // Conexión con la Base de Datos de Usuarios Sin Password.
//var conn2 = builder.Configuration.GetConnectionString("conn2"); // Conexión con la Base de Datos de Datos Sin Password.
//var pass = Environment.GetEnvironmentVariable("SQL-SERVER"); // Contraseña de la Base de Datos.
//var fullConn = $"{conn};Password={pass}";
//var fullConn2 = $"{conn2};Password={pass}";

var pass = Environment.GetEnvironmentVariable("SQL-SERVER");
var fullConn = $"{builder.Configuration.GetConnectionString("conn")};Password={pass}";
var fullConn2 = $"{builder.Configuration.GetConnectionString("conn2")};Password={pass}";

builder.Services.AddDbContext<UserContext>(options => options.UseSqlServer(fullConn)); // Conexión con la Base de Datos de Usuarios.
builder.Services.AddDbContext<NexusStarsContext>(options => options.UseSqlServer(fullConn2)); // Conexión con la Base de Datos de Datos.

builder.Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Para Evitar que los JSON Hagan un Bucle Infinito.}
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddIdentity<NexusUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<UserContext>(); // AddIdentity Agrega todos los servicios, Roles, SingInManager, Etc. para la Autenticación de Usuarios.

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    }) // Método para la Autenticación por Token.
    .AddGoogle(options =>
    {
        options.ClientId = Environment.GetEnvironmentVariable("Google-Client-Id")!;
        options.ClientSecret = Environment.GetEnvironmentVariable("Google-Client-Secret")!;
    })
    .AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = Environment.GetEnvironmentVariable("Microsoft-Client-Id")!;
        microsoftOptions.ClientSecret = Environment.GetEnvironmentVariable("Microsoft-Client-Secret")!;
    }); // Métodos para la Autenticación por Google y Microsoft.

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Pega el Token del Usuario Logueado",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
}); // Este Método Habilita Swagger para Hacer la Pruebas de la API Autenticando Usuarios con el Token.

var AllowCors = "AllowCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowCors, policy =>
    {
        policy.WithOrigins(
            "https://nexus-astralis-2.vercel.app",
            "https://login-google-rho.vercel.app",
            "https://external-login-lemon.vercel.app",
            "http://localhost:4200") // Permitir los origenes de pruebas.
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Logging.ClearProviders(); // Limpia los proveedores de registro predeterminados.
builder.Logging.AddConsole(); // Agrega el registro en la consola.
builder.Logging.AddDebug(); // Agrega el registro en la ventana de depuración.


var app = builder.Build();

app.UseCors(AllowCors); // Habilita CORS para la API.

app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("Cross-Origin-Opener-Policy");
    context.Response.Headers.Remove("Cross-Origin-Embedder-Policy");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();