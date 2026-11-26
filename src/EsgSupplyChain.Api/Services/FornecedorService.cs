using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Models;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EsgSupplyChain.Api.Services;

public interface IFornecedorService
{
    Task<ResultadoPaginadoViewModel<FornecedorRespostaViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nivelCadeia,
        string? pais,
        bool? apenasSemCertificacao);

    Task<FornecedorRespostaViewModel> BuscarPorIdAsync(long id);
    Task<FornecedorRespostaViewModel> CadastrarAsync(FornecedorRequisicaoViewModel dados);
    Task<FornecedorRespostaViewModel> AtualizarAsync(long id, FornecedorRequisicaoViewModel dados);
    Task<ConformidadeFornecedorViewModel> AvaliarConformidadeAsync(long id);
}

public class FornecedorService : IFornecedorService
{
    private static readonly HashSet<string> TiersPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "TIER_1", "TIER_2", "TIER_3"
    };

    private readonly EsgDbContext _context;
    private readonly IAlertaConformidadeService _alertaService;

    public FornecedorService(EsgDbContext context, IAlertaConformidadeService alertaService)
    {
        _context = context;
        _alertaService = alertaService;
    }

    public async Task<ResultadoPaginadoViewModel<FornecedorRespostaViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        string? nivelCadeia,
        string? pais,
        bool? apenasSemCertificacao)
    {
        if (pagina < 1 || tamanhoPagina < 1 || tamanhoPagina > 50)
        {
            throw new ArgumentException("Os parâmetros de paginação são inválidos: informe página >= 1 e tamanhoPagina entre 1 e 50.");
        }

        var query = _context.Fornecedores.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nivelCadeia))
        {
            var tier = nivelCadeia.Trim().ToUpperInvariant();
            query = query.Where(f => f.NivelCadeia == tier);
        }

        if (!string.IsNullOrWhiteSpace(pais))
        {
            var paisFiltro = pais.Trim().ToLower();
            query = query.Where(f => f.Pais.ToLower() == paisFiltro);
        }

        if (apenasSemCertificacao == true)
        {
            query = query.Where(f => !f.CertificadoDireitosHumanos);
        }

        var totalItens = await query.CountAsync();
        var fornecedores = await query
            .OrderBy(f => f.NomeEmpresa)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var itens = fornecedores.Select(FornecedorRespostaViewModel.DeEntidade).ToList();
        return ResultadoPaginadoViewModel<FornecedorRespostaViewModel>.Criar(itens, totalItens, pagina, tamanhoPagina);
    }

    public async Task<FornecedorRespostaViewModel> BuscarPorIdAsync(long id)
    {
        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Fornecedor não encontrado com o ID: {id}");

        return FornecedorRespostaViewModel.DeEntidade(fornecedor);
    }

    public async Task<FornecedorRespostaViewModel> CadastrarAsync(FornecedorRequisicaoViewModel dados)
    {
        var cnpjLimpo = NormalizarValidarCnpj(dados.Cnpj);
        var nivelNormalizado = NormalizarNivelCadeia(dados.NivelCadeia);

        if (dados.MetaAnualCarbono <= 0)
        {
            throw new RegraDeNegocioException("A meta anual de emissões do fornecedor deve ser maior que zero.");
        }

        var jaExiste = await _context.Fornecedores.AnyAsync(f => f.Cnpj == cnpjLimpo);
        if (jaExiste)
        {
            throw new RegraDeNegocioException("Já existe um fornecedor cadastrado com este CNPJ.");
        }

        var fornecedor = new Fornecedor
        {
            NomeEmpresa = dados.NomeEmpresa.Trim(),
            Cnpj = cnpjLimpo,
            Pais = dados.Pais.Trim(),
            NivelCadeia = nivelNormalizado,
            MetaAnualCarbono = Math.Round(dados.MetaAnualCarbono, 2),
            CertificadoDireitosHumanos = dados.CertificadoDireitosHumanos,
            DataCadastro = DateTime.UtcNow
        };

        _context.Fornecedores.Add(fornecedor);
        await _context.SaveChangesAsync();

        await _alertaService.SincronizarAlertasFornecedorAsync(fornecedor.Id);

        return FornecedorRespostaViewModel.DeEntidade(fornecedor);
    }

    public async Task<FornecedorRespostaViewModel> AtualizarAsync(long id, FornecedorRequisicaoViewModel dados)
    {
        var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Fornecedor não encontrado com o ID: {id}");

        var cnpjLimpo = NormalizarValidarCnpj(dados.Cnpj);
        var nivelNormalizado = NormalizarNivelCadeia(dados.NivelCadeia);

        if (dados.MetaAnualCarbono <= 0)
        {
            throw new RegraDeNegocioException("A meta anual de emissões do fornecedor deve ser maior que zero.");
        }

        var cnpjConflitante = await _context.Fornecedores
            .AnyAsync(f => f.Cnpj == cnpjLimpo && f.Id != id);

        if (cnpjConflitante)
        {
            throw new RegraDeNegocioException("Já existe outro fornecedor cadastrado com este CNPJ.");
        }

        fornecedor.NomeEmpresa = dados.NomeEmpresa.Trim();
        fornecedor.Cnpj = cnpjLimpo;
        fornecedor.Pais = dados.Pais.Trim();
        fornecedor.NivelCadeia = nivelNormalizado;
        fornecedor.MetaAnualCarbono = Math.Round(dados.MetaAnualCarbono, 2);
        fornecedor.CertificadoDireitosHumanos = dados.CertificadoDireitosHumanos;

        await _context.SaveChangesAsync();
        await _alertaService.SincronizarAlertasFornecedorAsync(fornecedor.Id);

        return FornecedorRespostaViewModel.DeEntidade(fornecedor);
    }

    // Avalia teto anual de carbono, auditoria e direitos humanos para classificar o fornecedor
    public async Task<ConformidadeFornecedorViewModel> AvaliarConformidadeAsync(long id)
    {
        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Fornecedor não encontrado com o ID: {id}");

        var emissoesValidas = await _context.Emissoes
            .AsNoTracking()
            .Where(e => e.FornecedorId == id
                        && e.Ativo
                        && e.StatusAuditoria != "REJEITADO"
                        && e.StatusAuditoria != "INVALIDADO")
            .ToListAsync();

        var alertasAbertos = await _context.AlertasConformidade
            .AsNoTracking()
            .CountAsync(a => a.FornecedorId == id && !a.Resolvido);

        var totalCarbono = emissoesValidas.Sum(e => e.QuantidadeCarbono);
        var auditados = emissoesValidas.Count(e => e.StatusAuditoria == "AUDITADO");
        var percentualMeta = fornecedor.MetaAnualCarbono > 0
            ? Math.Round((totalCarbono / fornecedor.MetaAnualCarbono) * 100m, 2)
            : 0m;

        string status;
        if (!fornecedor.CertificadoDireitosHumanos)
        {
            status = "BLOQUEADO_DIREITOS_HUMANOS";
        }
        else if (totalCarbono > fornecedor.MetaAnualCarbono)
        {
            status = "META_CARBONO_EXCEDIDA";
        }
        else if (percentualMeta >= 80m)
        {
            status = "ALERTA_LIMITE_CARBONO";
        }
        else if (emissoesValidas.Count == 0)
        {
            status = "PENDENTE_REPORTE_ESCOPO3";
        }
        else if (auditados == 0)
        {
            status = "EM_ANALISE_AUDITORIA";
        }
        else
        {
            status = "CONFORME_AUDITADO";
        }

        return new ConformidadeFornecedorViewModel
        {
            FornecedorId = fornecedor.Id,
            NomeEmpresa = fornecedor.NomeEmpresa,
            NivelCadeia = fornecedor.NivelCadeia,
            CertificadoDireitosHumanos = fornecedor.CertificadoDireitosHumanos,
            MetaAnualCarbono = fornecedor.MetaAnualCarbono,
            TotalCarbonoContabilizado = Math.Round(totalCarbono, 2),
            PercentualMetaUtilizado = percentualMeta,
            QuantidadeRelatoriosValidos = emissoesValidas.Count,
            QuantidadeRelatoriosAuditados = auditados,
            AlertasAbertos = alertasAbertos,
            StatusConformidade = status
        };
    }

    private static string NormalizarValidarCnpj(string cnpjBruto)
    {
        var apenasDigitos = new string((cnpjBruto ?? string.Empty).Where(char.IsDigit).ToArray());
        if (apenasDigitos.Length != 14)
        {
            throw new RegraDeNegocioException("O CNPJ informado deve conter exatamente 14 dígitos numéricos.");
        }

        return apenasDigitos;
    }

    private static string NormalizarNivelCadeia(string nivel)
    {
        var normalizado = (nivel ?? string.Empty).Trim().ToUpperInvariant();
        if (!TiersPermitidos.Contains(normalizado))
        {
            throw new RegraDeNegocioException("O nível da cadeia deve ser TIER_1, TIER_2 ou TIER_3.");
        }

        return normalizado;
    }
}
