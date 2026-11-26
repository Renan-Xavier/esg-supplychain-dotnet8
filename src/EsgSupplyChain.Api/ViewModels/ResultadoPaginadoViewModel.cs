namespace EsgSupplyChain.Api.ViewModels;

public class ResultadoPaginadoViewModel<T>
{
    public int PaginaAtual { get; init; }
    public int TamanhoPagina { get; init; }
    public int TotalItens { get; init; }
    public int TotalPaginas { get; init; }
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();

    public static ResultadoPaginadoViewModel<T> Criar(IReadOnlyList<T> itens, int totalItens, int pagina, int tamanhoPagina)
    {
        var totalPaginas = totalItens == 0 ? 0 : (int)Math.Ceiling(totalItens / (double)tamanhoPagina);
        return new ResultadoPaginadoViewModel<T>
        {
            PaginaAtual = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = totalItens,
            TotalPaginas = totalPaginas,
            Itens = itens
        };
    }
}
