using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ESG Supply Chain API (.NET 8)",
        Version = "v1",
        Description = "Estrutura inicial do Webservice RESTful para transparência e conformidade ESG na cadeia de suprimentos."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/status", () => Results.Ok(new
{
    Projeto = "ESG Supply Chain API (.NET 8)",
    TemaEsg = "Tema 6 - Transparência na cadeia de suprimentos",
    Status = "Especificação SDD concluída; implementação das camadas MVVM em andamento."
}));

app.Run();
