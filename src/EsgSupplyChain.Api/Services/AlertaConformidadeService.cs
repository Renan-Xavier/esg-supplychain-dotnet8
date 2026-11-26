using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Models;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EsgSupplyChain.Api.Services;

public interface IAlertaConformidadeService
{
    Task<ResultadoPaginadoViewModel<AlertaConformidadeViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? severidade,
        bool? resolvido);

    Task<PainelCadeiaSuprimentosViewModel> ObterPainelCadeiaAsync();
    Task<AlertaConformidadeViewModel> ResolverAlertaAsync(long alertaId, ResolverAlertaViewModel dados);
    Task SincronizarAlertasFornecedorAsync(long fornecedorId);
}

public class AlertaConformidadeService : IAlertaConformidadeService
{
    private readonly EsgDbContext _context;

    public AlertaConformidadeService(EsgDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoPaginadoViewModel<AlertaConformidadeViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? severidade,
        bool? resolvido)
    {
        ValidarPaginacao(pagina, tamanhoPagina);

        var query = _context.AlertasConformidade
            .AsNoTracking()
            .Include(a => a.Fornecedor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(severidade))
        {
            var sevNormalizada = severidade.Trim().ToUpperInvariant();
            query = query.Where(a => a.Severidade == sevNormalizada);
        }

        if (resolvido.HasValue)
        {
            query = query.Where(a => a.Resolvido == resolvido.Value);
        }

        var totalItens = await query.CountAsync();
        var registros = await query
            .OrderBy(a => a.Resolvido)
            .ThenByDescending(a => a.DataGeracao)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var itens = registros.Select(AlertaConformidadeViewModel.DeEntidade).ToList();
        return ResultadoPaginadoViewModel<AlertaConformidadeViewModel>.Criar(itens, totalItens, pagina, tamanhoPagina);
    }

    public async Task<PainelCadeiaSuprimentosViewModel> ObterPainelCadeiaAsync()
    {
        var fornecedores = await _context.Fornecedores
            .AsNoTracking()
            .ToListAsync();

        var volumeTotal = await _context.Emissoes
            .AsNoTracking()
            .Where(e => e.Ativo && e.StatusAuditoria != "REJEITADO" && e.StatusAuditoria != "INVALIDADO")
            .SumAsync(e => (decimal?)e.QuantidadeCarbono) ?? 0m;

        var alertasAbertos = await _context.AlertasConformidade
            .AsNoTracking()
            .Where(a => !a.Resolvido)
            .ToListAsync();

        var porTier = fornecedores
            .GroupBy(f => f.NivelCadeia)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PainelCadeiaSuprimentosViewModel
        {
            TotalFornecedores = fornecedores.Count,
            FornecedoresSemCertificacaoDh = fornecedores.Count(f => !f.CertificadoDireitosHumanos),
            VolumeTotalCarbonoEscopo3 = Math.Round(volumeTotal, 2),
            AlertasCriticosAbertos = alertasAbertos.Count(a => a.Severidade == "CRITICA"),
            AlertasAltaSeveridadeAbertos = alertasAbertos.Count(a => a.Severidade == "ALTA"),
            FornecedoresPorTier = porTier
        };
    }

    public async Task<AlertaConformidadeViewModel> ResolverAlertaAsync(long alertaId, ResolverAlertaViewModel dados)
    {
        var plano = dados.PlanoMitigacao?.Trim() ?? string.Empty;
        if (plano.Length < 10)
        {
            throw new RegraDeNegocioException("O plano de mitigação deve conter pelo menos 10 caracteres úteis.");
        }

        var alerta = await _context.AlertasConformidade
            .Include(a => a.Fornecedor)
            .FirstOrDefaultAsync(a => a.Id == alertaId)
            ?? throw new RecursoNaoEncontradoException($"Alerta de conformidade não encontrado com o ID: {alertaId}");

        if (alerta.Resolvido)
        {
            throw new RegraDeNegocioException("Este alerta de conformidade já foi encerrado anteriormente.");
        }

        alerta.Resolvido = true;
        alerta.PlanoMitigacao = plano;
        alerta.DataResolucao = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return AlertaConformidadeViewModel.DeEntidade(alerta);
    }

    public async Task SincronizarAlertasFornecedorAsync(long fornecedorId)
    {
        var fornecedor = await _context.Fornecedores
            .FirstOrDefaultAsync(f => f.Id == fornecedorId);

        if (fornecedor is null)
        {
            return;
        }

        var totalCarbono = await _context.Emissoes
            .Where(e => e.FornecedorId == fornecedorId
                        && e.Ativo
                        && e.StatusAuditoria != "REJEITADO"
                        && e.StatusAuditoria != "INVALIDADO")
            .SumAsync(e => (decimal?)e.QuantidadeCarbono) ?? 0m;

        var alertasEmAberto = await _context.AlertasConformidade
            .Where(a => a.FornecedorId == fornecedorId && !a.Resolvido)
            .ToListAsync();

        // Regra de certificação de direitos humanos
        var alertaDh = alertasEmAberto.FirstOrDefault(a => a.TipoAlerta == "SEM_CERTIFICACAO_DH");
        if (!fornecedor.CertificadoDireitosHumanos)
        {
            RegistrarOuAtualizarAlerta(
                alertaDh,
                fornecedorId,
                "SEM_CERTIFICACAO_DH",
                "CRITICA",
                $"Fornecedor {fornecedor.NomeEmpresa} opera sem certificação ativa de direitos humanos e trabalho justo.");
        }
        else if (alertaDh is not null)
        {
            alertaDh.Resolvido = true;
            alertaDh.PlanoMitigacao = "Certificação de direitos humanos regularizada no cadastro do fornecedor.";
            alertaDh.DataResolucao = DateTime.UtcNow;
        }

        // Regras de limite anual de carbono (80% preventivo e >100% violação crítica)
        var percentual = fornecedor.MetaAnualCarbono > 0
            ? Math.Round((totalCarbono / fornecedor.MetaAnualCarbono) * 100m, 1)
            : 0m;

        var alertaExcedido = alertasEmAberto.FirstOrDefault(a => a.TipoAlerta == "META_CARBONO_EXCEDIDA");
        var alertaPreventivo = alertasEmAberto.FirstOrDefault(a => a.TipoAlerta == "LIMITE_PREVENTIVO_80");

        if (totalCarbono > fornecedor.MetaAnualCarbono)
        {
            RegistrarOuAtualizarAlerta(
                alertaExcedido,
                fornecedorId,
                "META_CARBONO_EXCEDIDA",
                "CRITICA",
                $"Meta anual de carbono excedida ({percentual:F1}%): acumulado de {totalCarbono:F2} tCO2e para teto de {fornecedor.MetaAnualCarbono:F2} tCO2e.");

            if (alertaPreventivo is not null)
            {
                alertaPreventivo.Resolvido = true;
                alertaPreventivo.PlanoMitigacao = "Substituído automaticamente por alerta crítico de meta excedida.";
                alertaPreventivo.DataResolucao = DateTime.UtcNow;
            }
        }
        else if (percentual >= 80m)
        {
            RegistrarOuAtualizarAlerta(
                alertaPreventivo,
                fornecedorId,
                "LIMITE_PREVENTIVO_80",
                "ALTA",
                $"Fornecedor atingiu {percentual:F1}% da meta anual de emissões ({totalCarbono:F2} de {fornecedor.MetaAnualCarbono:F2} tCO2e).");
        }

        await _context.SaveChangesAsync();
    }

    private void RegistrarOuAtualizarAlerta(
        AlertaConformidade? existente,
        long fornecedorId,
        string tipo,
        string severidade,
        string mensagem)
    {
        if (existente is not null)
        {
            existente.Mensagem = mensagem;
            existente.Severidade = severidade;
            return;
        }

        _context.AlertasConformidade.Add(new AlertaConformidade
        {
            FornecedorId = fornecedorId,
            TipoAlerta = tipo,
            Severidade = severidade,
            Mensagem = mensagem,
            Resolvido = false,
            DataGeracao = DateTime.UtcNow
        });
    }

    private static void ValidarPaginacao(int pagina, int tamanhoPagina)
    {
        if (pagina < 1 || tamanhoPagina < 1 || tamanhoPagina > 50)
        {
            throw new ArgumentException("Os parâmetros de paginação são inválidos: informe página >= 1 e tamanhoPagina entre 1 e 50.");
        }
    }
}
