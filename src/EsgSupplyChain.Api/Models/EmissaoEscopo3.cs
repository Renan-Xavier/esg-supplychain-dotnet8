namespace EsgSupplyChain.Api.Models;

public class EmissaoEscopo3
{
    public long Id { get; set; }
    public long FornecedorId { get; set; }
    public string CategoriaEscopo3 { get; set; } = string.Empty;
    public decimal QuantidadeCarbono { get; set; }
    public string TipoDado { get; set; } = "PRIMARIO_MEDIDO";
    public string StatusAuditoria { get; set; } = "PENDENTE";
    public string? ObservacaoAuditoria { get; set; }
    public DateOnly DataRelatorio { get; set; }
    public bool Ativo { get; set; } = true;

    public Fornecedor? Fornecedor { get; set; }
}
