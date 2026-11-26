using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=EsgSupplyChainDb;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<EsgDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAlertaConformidadeService, AlertaConformidadeService>();
builder.Services.AddScoped<IFornecedorService, FornecedorService>();
builder.Services.AddScoped<IEmissaoService, EmissaoService>();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientesCorporativos", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("ClientesCorporativos");
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
