using EsgSupplyChain.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EsgSupplyChain.Api.Data;

public static class CargaInicialDados
{
    public static async Task InicializarAsync(EsgDbContext context)
    {
        if (await context.Fornecedores.AnyAsync())
        {
            return;
        }

        var transLog = new Fornecedor
        {
            NomeEmpresa = "TransLog Sul Transportes S.A.",
            Cnpj = "12345678000190",
            Pais = "Brasil",
            NivelCadeia = "TIER_1",
            MetaAnualCarbono = 500.00m,
            CertificadoDireitosHumanos = true,
            DataCadastro = DateTime.UtcNow.AddDays(-45)
        };

        var metalurgica = new Fornecedor
        {
            NomeEmpresa = "Metalúrgica Vale Verde Ltda.",
            Cnpj = "98765432000155",
            Pais = "Brasil",
            NivelCadeia = "TIER_2",
            MetaAnualCarbono = 300.00m,
            CertificadoDireitosHumanos = true,
            DataCadastro = DateTime.UtcNow.AddDays(-30)
        };

        var textilGlobal = new Fornecedor
        {
            NomeEmpresa = "Insumos Têxteis Andinos Corp.",
            Cnpj = "45123987000112",
            Pais = "Peru",
            NivelCadeia = "TIER_3",
            MetaAnualCarbono = 250.00m,
            CertificadoDireitosHumanos = false,
            DataCadastro = DateTime.UtcNow.AddDays(-15)
        };

        context.Fornecedores.AddRange(transLog, metalurgica, textilGlobal);
        await context.SaveChangesAsync();

        context.Emissoes.AddRange(
            new EmissaoEscopo3
            {
                FornecedorId = transLog.Id,
                CategoriaEscopo3 = "Transporte e distribuição upstream",
                QuantidadeCarbono = 210.50m,
                TipoDado = "PRIMARIO_MEDIDO",
                StatusAuditoria = "AUDITADO",
                ObservacaoAuditoria = "Frota monitorada via telemetria homologada.",
                DataRelatorio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                Ativo = true
            },
            new EmissaoEscopo3
            {
                FornecedorId = metalurgica.Id,
                CategoriaEscopo3 = "Bens e serviços adquiridos",
                QuantidadeCarbono = 265.00m,
                TipoDado = "ESTIMADO_MEDIA",
                StatusAuditoria = "PENDENTE",
                DataRelatorio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                Ativo = true
            }
        );

        context.AlertasConformidade.AddRange(
            new AlertaConformidade
            {
                FornecedorId = metalurgica.Id,
                TipoAlerta = "LIMITE_PREVENTIVO_80",
                Severidade = "ALTA",
                Mensagem = "Fornecedor atingiu 88,3% da meta anual de emissões (265,00 de 300,00 tCO2e).",
                Resolvido = false,
                DataGeracao = DateTime.UtcNow.AddDays(-10)
            },
            new AlertaConformidade
            {
                FornecedorId = textilGlobal.Id,
                TipoAlerta = "SEM_CERTIFICACAO_DH",
                Severidade = "CRITICA",
                Mensagem = "Fornecedor sem certificação ativa de direitos humanos e trabalho justo.",
                Resolvido = false,
                DataGeracao = DateTime.UtcNow.AddDays(-15)
            }
        );

        await context.SaveChangesAsync();
    }
}
