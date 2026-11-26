using System.ComponentModel.DataAnnotations;
using EsgSupplyChain.Api.Models;

namespace EsgSupplyChain.Api.ViewModels;

public class FornecedorRequisicaoViewModel
{
    [Required(ErrorMessage = "O nome da empresa é obrigatório.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "O nome da empresa deve ter entre 3 e 150 caracteres.")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ do fornecedor é obrigatório.")]
    [StringLength(18, MinimumLength = 14, ErrorMessage = "Informe um CNPJ válido com 14 dígitos.")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O país de operação é obrigatório.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "O país deve ter entre 2 e 80 caracteres.")]
    public string Pais { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nível na cadeia de suprimentos é obrigatório (TIER_1, TIER_2 ou TIER_3).")]
    public string NivelCadeia { get; set; } = "TIER_1";

    [Range(0.01, 999999999, ErrorMessage = "A meta anual de carbono deve ser maior que zero.")]
    public decimal MetaAnualCarbono { get; set; }

    public bool CertificadoDireitosHumanos { get; set; } = true;
}

public class FornecedorRespostaViewModel
{
    public long Id { get; init; }
    public string NomeEmpresa { get; init; } = string.Empty;
    public string Cnpj { get; init; } = string.Empty;
    public string Pais { get; init; } = string.Empty;
    public string NivelCadeia { get; init; } = string.Empty;
    public decimal MetaAnualCarbono { get; init; }
    public bool CertificadoDireitosHumanos { get; init; }
    public DateTime DataCadastro { get; init; }

    public static FornecedorRespostaViewModel DeEntidade(Fornecedor entidade) => new()
    {
        Id = entidade.Id,
        NomeEmpresa = entidade.NomeEmpresa,
        Cnpj = entidade.Cnpj,
        Pais = entidade.Pais,
        NivelCadeia = entidade.NivelCadeia,
        MetaAnualCarbono = entidade.MetaAnualCarbono,
        CertificadoDireitosHumanos = entidade.CertificadoDireitosHumanos,
        DataCadastro = entidade.DataCadastro
    };
}

public class ConformidadeFornecedorViewModel
{
    public long FornecedorId { get; init; }
    public string NomeEmpresa { get; init; } = string.Empty;
    public string NivelCadeia { get; init; } = string.Empty;
    public bool CertificadoDireitosHumanos { get; init; }
    public decimal MetaAnualCarbono { get; init; }
    public decimal TotalCarbonoContabilizado { get; init; }
    public decimal PercentualMetaUtilizado { get; init; }
    public int QuantidadeRelatoriosValidos { get; init; }
    public int QuantidadeRelatoriosAuditados { get; init; }
    public int AlertasAbertos { get; init; }
    public string StatusConformidade { get; init; } = string.Empty;
}
