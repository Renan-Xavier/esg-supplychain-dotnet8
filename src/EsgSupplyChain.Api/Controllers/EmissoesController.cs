using EsgSupplyChain.Api.Services;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgSupplyChain.Api.Controllers;

[ApiController]
[Route("api/emissoes")]
public class EmissoesController : ControllerBase
{
    private readonly IEmissaoService _emissaoService;

    public EmissoesController(IEmissaoService emissaoService)
    {
        _emissaoService = emissaoService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResultadoPaginadoViewModel<EmissaoRespostaViewModel>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] long? fornecedorId = null,
        [FromQuery] string? statusAuditoria = null)
    {
        var resultado = await _emissaoService.ListarPaginadoAsync(pagina, tamanhoPagina, fornecedorId, statusAuditoria);
        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<EmissaoRespostaViewModel>> BuscarPorId(long id)
    {
        var emissao = await _emissaoService.BuscarPorIdAsync(id);
        return Ok(emissao);
    }

    [HttpPost]
    public async Task<ActionResult<EmissaoRespostaViewModel>> Registrar([FromBody] EmissaoRequisicaoViewModel dados)
    {
        var criada = await _emissaoService.RegistrarAsync(dados);
        return CreatedAtAction(nameof(BuscarPorId), new { id = criada.Id }, criada);
    }

    [HttpPatch("{id:long}/auditoria")]
    public async Task<ActionResult<EmissaoRespostaViewModel>> Auditar(long id, [FromBody] EmissaoAuditoriaViewModel dados)
    {
        var atualizada = await _emissaoService.AuditarAsync(id, dados);
        return Ok(atualizada);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Invalidar(long id)
    {
        await _emissaoService.InvalidarAsync(id);
        return NoContent();
    }
}

