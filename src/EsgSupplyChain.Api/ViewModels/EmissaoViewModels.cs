using System.ComponentModel.DataAnnotations;
using EsgSupplyChain.Api.Models;

namespace EsgSupplyChain.Api.ViewModels;

public class EmissaoRequisicaoViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Informe um identificador de fornecedor válido.")]
    public long FornecedorId { get; set; }

    [Required(ErrorMessage = "A categoria de Escopo 3 é obrigatória.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "A categoria deve ter entre 3 e 100 caracteres.")]
    public string CategoriaEscopo3 { get; set; } = string.Empty;

    [Range(0.01, 999999999, ErrorMessage = "A quantidade de carbono emitida deve ser maior que zero.")]
    public decimal QuantidadeCarbono { get; set; }

    [Required(ErrorMessage = "O tipo de dado é obrigatório (PRIMARIO_MEDIDO ou ESTIMADO_MEDIA).")]
    public string TipoDado { get; set; } = "PRIMARIO_MEDIDO";

    [Required(ErrorMessage = "A data do relatório é obrigatória.")]
    public DateOnly DataRelatorio { get; set; }
}

public class EmissaoAuditoriaViewModel
{
    [Required(ErrorMessage = "O novo status de auditoria é obrigatório (AUDITADO ou REJEITADO).")]
    public string StatusAuditoria { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "A observação de auditoria deve ter no máximo 300 caracteres.")]
    public string? ObservacaoAuditoria { get; set; }
}

public class EmissaoRespostaViewModel
{
    public long Id { get; init; }
    public long FornecedorId { get; init; }
    public string NomeFornecedor { get; init; } = string.Empty;
    public string CategoriaEscopo3 { get; init; } = string.Empty;
    public decimal QuantidadeCarbono { get; init; }
    public string TipoDado { get; init; } = string.Empty;
    public string StatusAuditoria { get; init; } = string.Empty;
    public string? ObservacaoAuditoria { get; init; }
    public DateOnly DataRelatorio { get; init; }
    public bool Ativo { get; init; }

    public static EmissaoRespostaViewModel DeEntidade(EmissaoEscopo3 entidade, string? nomeFornecedor = null) => new()
    {
        Id = entidade.Id,
        FornecedorId = entidade.FornecedorId,
        NomeFornecedor = nomeFornecedor ?? entidade.Fornecedor?.NomeEmpresa ?? string.Empty,
        CategoriaEscopo3 = entidade.CategoriaEscopo3,
        QuantidadeCarbono = entidade.QuantidadeCarbono,
        TipoDado = entidade.TipoDado,
        StatusAuditoria = entidade.StatusAuditoria,
        ObservacaoAuditoria = entidade.ObservacaoAuditoria,
        DataRelatorio = entidade.DataRelatorio,
        Ativo = entidade.Ativo
    };
}
