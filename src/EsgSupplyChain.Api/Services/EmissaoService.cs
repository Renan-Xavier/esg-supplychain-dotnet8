using EsgSupplyChain.Api.Data;
using EsgSupplyChain.Api.Exceptions;
using EsgSupplyChain.Api.Models;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EsgSupplyChain.Api.Services;

public interface IEmissaoService
{
    Task<ResultadoPaginadoViewModel<EmissaoRespostaViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        long? fornecedorId,
        string? statusAuditoria);

    Task<EmissaoRespostaViewModel> BuscarPorIdAsync(long id);
    Task<EmissaoRespostaViewModel> RegistrarAsync(EmissaoRequisicaoViewModel dados);
    Task<EmissaoRespostaViewModel> AuditarAsync(long id, EmissaoAuditoriaViewModel dados);
    Task InvalidarAsync(long id);
}

public class EmissaoService : IEmissaoService
{
    private static readonly HashSet<string> TiposDadoPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "PRIMARIO_MEDIDO", "ESTIMADO_MEDIA"
    };

    private static readonly HashSet<string> StatusParecerPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUDITADO", "REJEITADO", "PENDENTE"
    };

    private readonly EsgDbContext _context;
    private readonly IAlertaConformidadeService _alertaService;

    public EmissaoService(EsgDbContext context, IAlertaConformidadeService alertaService)
    {
        _context = context;
        _alertaService = alertaService;
    }

    public async Task<ResultadoPaginadoViewModel<EmissaoRespostaViewModel>> ListarPaginadoAsync(
        int pagina,
        int tamanhoPagina,
        long? fornecedorId,
        string? statusAuditoria)
    {
        if (pagina < 1 || tamanhoPagina < 1 || tamanhoPagina > 50)
        {
            throw new ArgumentException("Os parâmetros de paginação são inválidos: informe página >= 1 e tamanhoPagina entre 1 e 50.");
        }

        var query = _context.Emissoes
            .AsNoTracking()
            .Include(e => e.Fornecedor)
            .Where(e => e.Ativo)
            .AsQueryable();

        if (fornecedorId.HasValue)
        {
            query = query.Where(e => e.FornecedorId == fornecedorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusAuditoria))
        {
            var status = statusAuditoria.Trim().ToUpperInvariant();
            query = query.Where(e => e.StatusAuditoria == status);
        }

        var totalItens = await query.CountAsync();
        var registros = await query
            .OrderByDescending(e => e.DataRelatorio)
            .ThenByDescending(e => e.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var itens = registros.Select(e => EmissaoRespostaViewModel.DeEntidade(e)).ToList();
        return ResultadoPaginadoViewModel<EmissaoRespostaViewModel>.Criar(itens, totalItens, pagina, tamanhoPagina);
    }

    public async Task<EmissaoRespostaViewModel> BuscarPorIdAsync(long id)
    {
        var emissao = await _context.Emissoes
            .AsNoTracking()
            .Include(e => e.Fornecedor)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Relatório de emissão não encontrado com o ID: {id}");

        return EmissaoRespostaViewModel.DeEntidade(emissao);
    }

    public async Task<EmissaoRespostaViewModel> RegistrarAsync(EmissaoRequisicaoViewModel dados)
    {
        var fornecedor = await _context.Fornecedores
            .FirstOrDefaultAsync(f => f.Id == dados.FornecedorId)
            ?? throw new RecursoNaoEncontradoException($"Fornecedor não encontrado com o ID: {dados.FornecedorId}");

        if (dados.QuantidadeCarbono <= 0)
        {
            throw new RegraDeNegocioException("A quantidade de carbono reportada deve ser estritamente maior que zero.");
        }

        var tipoNormalizado = (dados.TipoDado ?? string.Empty).Trim().ToUpperInvariant();
        if (!TiposDadoPermitidos.Contains(tipoNormalizado))
        {
            throw new RegraDeNegocioException("O tipo de dado deve ser PRIMARIO_MEDIDO ou ESTIMADO_MEDIA.");
        }

        var novaEmissao = new EmissaoEscopo3
        {
            FornecedorId = fornecedor.Id,
            CategoriaEscopo3 = dados.CategoriaEscopo3.Trim(),
            QuantidadeCarbono = Math.Round(dados.QuantidadeCarbono, 2),
            TipoDado = tipoNormalizado,
            StatusAuditoria = "PENDENTE",
            DataRelatorio = dados.DataRelatorio,
            Ativo = true
        };

        _context.Emissoes.Add(novaEmissao);
        await _context.SaveChangesAsync();

        await _alertaService.SincronizarAlertasFornecedorAsync(fornecedor.Id);

        return EmissaoRespostaViewModel.DeEntidade(novaEmissao, fornecedor.NomeEmpresa);
    }

    public async Task<EmissaoRespostaViewModel> AuditarAsync(long id, EmissaoAuditoriaViewModel dados)
    {
        var emissao = await _context.Emissoes
            .Include(e => e.Fornecedor)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Relatório de emissão não encontrado com o ID: {id}");

        if (!emissao.Ativo)
        {
            throw new RegraDeNegocioException("Não é possível auditar um relatório de emissão que já foi invalidado.");
        }

        var statusNormalizado = (dados.StatusAuditoria ?? string.Empty).Trim().ToUpperInvariant();
        if (!StatusParecerPermitidos.Contains(statusNormalizado))
        {
            throw new RegraDeNegocioException("O parecer de auditoria deve ser AUDITADO, REJEITADO ou PENDENTE.");
        }

        emissao.StatusAuditoria = statusNormalizado;
        emissao.ObservacaoAuditoria = string.IsNullOrWhiteSpace(dados.ObservacaoAuditoria)
            ? null
            : dados.ObservacaoAuditoria.Trim();

        await _context.SaveChangesAsync();
        await _alertaService.SincronizarAlertasFornecedorAsync(emissao.FornecedorId);

        return EmissaoRespostaViewModel.DeEntidade(emissao);
    }

    // Inativação lógica para preservar trilha histórica de auditoria
    public async Task InvalidarAsync(long id)
    {
        var emissao = await _context.Emissoes
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new RecursoNaoEncontradoException($"Relatório de emissão não encontrado com o ID: {id}");

        emissao.Ativo = false;
        emissao.StatusAuditoria = "INVALIDADO";

        await _context.SaveChangesAsync();
        await _alertaService.SincronizarAlertasFornecedorAsync(emissao.FornecedorId);
    }
}
