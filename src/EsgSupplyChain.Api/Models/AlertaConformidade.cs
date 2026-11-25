namespace EsgSupplyChain.Api.Models;

public class AlertaConformidade
{
    public long Id { get; set; }
    public long FornecedorId { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Severidade { get; set; } = "ALTA";
    public string Mensagem { get; set; } = string.Empty;
    public bool Resolvido { get; set; }
    public string? PlanoMitigacao { get; set; }
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;
    public DateTime? DataResolucao { get; set; }

    public Fornecedor? Fornecedor { get; set; }
}
