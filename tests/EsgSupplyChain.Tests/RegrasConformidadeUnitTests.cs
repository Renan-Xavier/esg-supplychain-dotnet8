using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Services;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EsgSupplyChain.Tests;

public class RegrasConformidadeUnitTests
{
    private static EsgDbContext CriarContextoIsolado()
    {
        var options = new DbContextOptionsBuilder<EsgDbContext>()
            .UseInMemoryDatabase($"UnitEsgDb_{Guid.NewGuid()}")
            .Options;

        return new EsgDbContext(options);
    }

    [Fact]
    public async Task CadastrarFornecedor_ComCnpjDuplicado_LancaRegraDeNegocioException()
    {
        await using var context = CriarContextoIsolado();
        var alertaService = new AlertaConformidadeService(context);
        var fornecedorService = new FornecedorService(context, alertaService);

        var requisicao = new FornecedorRequisicaoViewModel
        {
            NomeEmpresa = "Química Limpa S.A.",
            Cnpj = "11.222.333/0001-44",
            Pais = "Brasil",
            NivelCadeia = "TIER_1",
            MetaAnualCarbono = 400m,
            CertificadoDireitosHumanos = true
        };

        await fornecedorService.CadastrarAsync(requisicao);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => fornecedorService.CadastrarAsync(requisicao));
    }

    [Fact]
    public async Task UltrapassarMetaAnualDeCarbono_GeraAlertaCriticoSemDuplicar()
    {
        await using var context = CriarContextoIsolado();
        var alertaService = new AlertaConformidadeService(context);
        var fornecedorService = new FornecedorService(context, alertaService);
        var emissaoService = new EmissaoService(context, alertaService);

        var fornecedor = await fornecedorService.CadastrarAsync(new FornecedorRequisicaoViewModel
        {
            NomeEmpresa = "Siderúrgica Horizonte Ltda.",
            Cnpj = "55666777000199",
            Pais = "Brasil",
            NivelCadeia = "TIER_1",
            MetaAnualCarbono = 100m,
            CertificadoDireitosHumanos = true
        });

        await emissaoService.RegistrarAsync(new EmissaoRequisicaoViewModel
        {
            FornecedorId = fornecedor.Id,
            CategoriaEscopo3 = "Transporte rodoviário pesado",
            QuantidadeCarbono = 120m,
            TipoDado = "PRIMARIO_MEDIDO",
            DataRelatorio = new DateOnly(2026, 10, 15)
        });

        // Segunda emissão acima da meta deve atualizar o alerta aberto sem duplicar registro
        await emissaoService.RegistrarAsync(new EmissaoRequisicaoViewModel
        {
            FornecedorId = fornecedor.Id,
            CategoriaEscopo3 = "Combustíveis e energia",
            QuantidadeCarbono = 30m,
            TipoDado = "PRIMARIO_MEDIDO",
            DataRelatorio = new DateOnly(2026, 11, 10)
        });

        var diagnostico = await fornecedorService.AvaliarConformidadeAsync(fornecedor.Id);
        Assert.Equal("META_CARBONO_EXCEDIDA", diagnostico.StatusConformidade);
        Assert.Equal(150m, diagnostico.TotalCarbonoContabilizado);
        Assert.Equal(1, diagnostico.AlertasAbertos);
    }

    [Fact]
    public async Task InvalidarEmissao_MantemRegistroHistoricoMasRetiraDoCalculo()
    {
        await using var context = CriarContextoIsolado();
        var alertaService = new AlertaConformidadeService(context);
        var fornecedorService = new FornecedorService(context, alertaService);
        var emissaoService = new EmissaoService(context, alertaService);

        var fornecedor = await fornecedorService.CadastrarAsync(new FornecedorRequisicaoViewModel
        {
            NomeEmpresa = "AgroFlorestal Raiz S.A.",
            Cnpj = "22333444000177",
            Pais = "Brasil",
            NivelCadeia = "TIER_2",
            MetaAnualCarbono = 200m,
            CertificadoDireitosHumanos = true
        });

        var emissao = await emissaoService.RegistrarAsync(new EmissaoRequisicaoViewModel
        {
            FornecedorId = fornecedor.Id,
            CategoriaEscopo3 = "Logística reversa",
            QuantidadeCarbono = 90m,
            TipoDado = "ESTIMADO_MEDIA",
            DataRelatorio = new DateOnly(2026, 11, 01)
        });

        await emissaoService.InvalidarAsync(emissao.Id);

        var registroNoBanco = await context.Emissoes.FirstAsync(e => e.Id == emissao.Id);
        Assert.False(registroNoBanco.Ativo);
        Assert.Equal("INVALIDADO", registroNoBanco.StatusAuditoria);

        var diagnostico = await fornecedorService.AvaliarConformidadeAsync(fornecedor.Id);
        Assert.Equal(0m, diagnostico.TotalCarbonoContabilizado);
        Assert.Equal("PENDENTE_REPORTE_ESCOPO3", diagnostico.StatusConformidade);
    }
}
