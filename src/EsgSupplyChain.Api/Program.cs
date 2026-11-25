using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=EsgSupplyChainDb;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<EsgDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddExceptionHandler<TratadorGlobalExcecoes>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/status", () => Results.Ok(new
{
    Servico = "ESG Supply Chain API",
    Versao = "0.2.0",
    Persistencia = "EF Core 8 + Migrations configuradas"
}));

app.Run();
