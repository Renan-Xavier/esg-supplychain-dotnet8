using System.Text;
using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=EsgSupplyChainDb;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<EsgDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAlertaConformidadeService, AlertaConformidadeService>();
builder.Services.AddScoped<IFornecedorService, FornecedorService>();
builder.Services.AddScoped<IEmissaoService, EmissaoService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddExceptionHandler<TratadorGlobalExcecoes>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos na requisição",
                Detail = "Verifique os campos enviados e tente novamente.",
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });

var chaveJwt = builder.Configuration["Jwt:ChaveSecreta"]
    ?? "ChaveCorporativaEsgSupplyChainFase4Fiap2026Segura!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "EsgSupplyChain.Api";
var audience = builder.Configuration["Jwt:Audience"] ?? "EsgSupplyChain.Clientes";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientesCorporativos", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ESG Supply Chain API (.NET 8)",
        Version = "v1",
        Description = "Webservice RESTful para rastreamento de emissões de Escopo 3 e auditoria de conformidade ESG na cadeia de suprimentos."
    });

    var esquemaJwt = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT obtido em POST /api/auth/token."
    };

    options.AddSecurityDefinition("Bearer", esquemaJwt);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("ClientesCorporativos");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EsgDbContext>();
    await db.Database.EnsureCreatedAsync();
    await CargaInicialDados.InicializarAsync(db);
}

app.Run();

public partial class Program { }
