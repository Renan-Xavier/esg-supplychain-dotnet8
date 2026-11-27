using EsgSupplyChain.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EsgSupplyChain.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _nomeBancoMemoria = $"EsgTestsDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descritorAtual = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EsgDbContext>));

            if (descritorAtual is not null)
            {
                services.Remove(descritorAtual);
            }

            services.AddDbContext<EsgDbContext>(options =>
                options.UseInMemoryDatabase(_nomeBancoMemoria));
        });
    }
}
