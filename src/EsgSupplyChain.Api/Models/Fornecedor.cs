namespace EsgSupplyChain.Api.Models;

public class Fornecedor
{
    public long Id { get; set; }
    public string NomeEmpresa { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string NivelCadeia { get; set; } = "TIER_1";
    public decimal MetaAnualCarbono { get; set; }
    public bool CertificadoDireitosHumanos { get; set; } = true;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public ICollection<EmissaoEscopo3> Emissoes { get; set; } = new List<EmissaoEscopo3>();
    public ICollection<AlertaConformidade> Alertas { get; set; } = new List<AlertaConformidade>();
}
