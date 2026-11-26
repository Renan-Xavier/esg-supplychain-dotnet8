using EsgSupplyChain.Api.Services;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgSupplyChain.Api.Controllers;

[ApiController]
[Route("api/fornecedores")]
public class FornecedoresController : ControllerBase
{
    private readonly IFornecedorService _fornecedorService;

    public FornecedoresController(IFornecedorService fornecedorService)
    {
        _fornecedorService = fornecedorService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResultadoPaginadoViewModel<FornecedorRespostaViewModel>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] string? nivelCadeia = null,
        [FromQuery] string? pais = null,
        [FromQuery] bool? apenasSemCertificacao = null)
    {
        var resultado = await _fornecedorService.ListarPaginadoAsync(
            pagina,
            tamanhoPagina,
            nivelCadeia,
            pais,
            apenasSemCertificacao);

        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<FornecedorRespostaViewModel>> BuscarPorId(long id)
    {
        var fornecedor = await _fornecedorService.BuscarPorIdAsync(id);
        return Ok(fornecedor);
    }

    [HttpPost]
    public async Task<ActionResult<FornecedorRespostaViewModel>> Cadastrar([FromBody] FornecedorRequisicaoViewModel dados)
    {
        var criado = await _fornecedorService.CadastrarAsync(dados);
        return CreatedAtAction(nameof(BuscarPorId), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<FornecedorRespostaViewModel>> Atualizar(long id, [FromBody] FornecedorRequisicaoViewModel dados)
    {
        var atualizado = await _fornecedorService.AtualizarAsync(id, dados);
        return Ok(atualizado);
    }

    [HttpGet("{id:long}/conformidade")]
    [AllowAnonymous]
    public async Task<ActionResult<ConformidadeFornecedorViewModel>> ConsultarConformidade(long id)
    {
        var diagnostico = await _fornecedorService.AvaliarConformidadeAsync(id);
        return Ok(diagnostico);
    }
}

