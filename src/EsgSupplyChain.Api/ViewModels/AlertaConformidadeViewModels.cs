using System.ComponentModel.DataAnnotations;
using EsgSupplyChain.Api.Models;

namespace EsgSupplyChain.Api.ViewModels;

public class ResolverAlertaViewModel
{
    [Required(ErrorMessage = "O plano de mitigação é obrigatório para encerrar o alerta.")]
    [StringLength(400, MinimumLength = 10, ErrorMessage = "Descreva o plano de mitigação com no mínimo 10 e no máximo 400 caracteres.")]
    public string PlanoMitigacao { get; set; } = string.Empty;
}

public class AlertaConformidadeViewModel
{
    public long Id { get; init; }
    public long FornecedorId { get; init; }
    public string NomeFornecedor { get; init; } = string.Empty;
    public string TipoAlerta { get; init; } = string.Empty;
    public string Severidade { get; init; } = string.Empty;
    public string Mensagem { get; init; } = string.Empty;
    public bool Resolvido { get; init; }
    public string? PlanoMitigacao { get; init; }
    public DateTime DataGeracao { get; init; }
    public DateTime? DataResolucao { get; init; }

    public static AlertaConformidadeViewModel DeEntidade(AlertaConformidade entidade) => new()
    {
        Id = entidade.Id,
        FornecedorId = entidade.FornecedorId,
        NomeFornecedor = entidade.Fornecedor?.NomeEmpresa ?? string.Empty,
        TipoAlerta = entidade.TipoAlerta,
        Severidade = entidade.Severidade,
        Mensagem = entidade.Mensagem,
        Resolvido = entidade.Resolvido,
        PlanoMitigacao = entidade.PlanoMitigacao,
        DataGeracao = entidade.DataGeracao,
        DataResolucao = entidade.DataResolucao
    };
}

public class PainelCadeiaSuprimentosViewModel
{
    public int TotalFornecedores { get; init; }
    public int FornecedoresSemCertificacaoDh { get; init; }
    public decimal VolumeTotalCarbonoEscopo3 { get; init; }
    public int AlertasCriticosAbertos { get; init; }
    public int AlertasAltaSeveridadeAbertos { get; init; }
    public IReadOnlyDictionary<string, int> FornecedoresPorTier { get; init; } = new Dictionary<string, int>();
}
